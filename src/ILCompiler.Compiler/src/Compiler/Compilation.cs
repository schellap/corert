// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.IO;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Internal.IL;

using Internal.JitInterface;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.IL.Stubs.StartupCode;

namespace ILCompiler
{
    public class CompilationOptions
    {
        public IReadOnlyDictionary<string, string> InputFilePaths;
        public IReadOnlyDictionary<string, string> ReferenceFilePaths;

        public string OutputFilePath;

        public string SystemModuleName;

        public TargetOS TargetOS;
        public TargetArchitecture TargetArchitecture;

        public bool MultiFile;

        public bool IsCppCodeGen;
        public bool NoLineNumbers;
        public string DgmlLog;
        public bool FullLog;
        public bool Verbose;
    }
    
    public partial class Compilation : ICompilationRootProvider
    {
        private readonly CompilerTypeSystemContext _typeSystemContext;
        private readonly CompilationOptions _options;
        private readonly TypeInitialization _typeInitManager;

        private NodeFactory _nodeFactory;
        private DependencyAnalyzerBase<NodeFactory> _dependencyGraph;

        private NameMangler _nameMangler = null;

        private ILCompiler.CppCodeGen.CppWriter _cppWriter = null;
        private CompilationModuleGroup _compilationModuleGroup;

        public Compilation(CompilationOptions options)
        {
            _options = options;

            _typeSystemContext = new CompilerTypeSystemContext(new TargetDetails(options.TargetArchitecture, options.TargetOS), options.MultiFile);
            _typeSystemContext.InputFilePaths = options.InputFilePaths;
            _typeSystemContext.ReferenceFilePaths = options.ReferenceFilePaths;

            _typeSystemContext.SetSystemModule(_typeSystemContext.GetModuleForSimpleName(options.SystemModuleName));

            _nameMangler = new NameMangler(this);

            _typeInitManager = new TypeInitialization();

            if (options.MultiFile)
            {
                _compilationModuleGroup = new MultiFileCompilationModuleGroup(_typeSystemContext, this);
            }
            else
            {
                _compilationModuleGroup = new SingleFileCompilationModuleGroup(_typeSystemContext, this);
            }
        }

        public CompilerTypeSystemContext TypeSystemContext
        {
            get
            {
                return _typeSystemContext;
            }
        }

        public NameMangler NameMangler
        {
            get
            {
                return _nameMangler;
            }
        }

        public NodeFactory NodeFactory
        {
            get
            {
                return _nodeFactory;
            }
        }

        public TextWriter Log
        {
            get;
            set;
        }

        public MethodDesc StartupCodeMain
        {
            get;
            set;
        }

        internal bool IsCppCodeGen
        {
            get
            {
                return _options.IsCppCodeGen;
            }
        }

        internal CompilationOptions Options
        {
            get
            {
                return _options;
            }
        }

        private ILProvider _methodILCache = new ILProvider();

        public MethodIL GetMethodIL(MethodDesc method)
        {
            // Flush the cache when it grows too big
            if (_methodILCache.Count > 1000)
                _methodILCache= new ILProvider();

            return _methodILCache.GetMethodIL(method);
        }

        private CorInfoImpl _corInfo;

        public void CompileSingleFile()
        {
            NodeFactory.NameMangler = NameMangler;

            _nodeFactory = new NodeFactory(_typeSystemContext, _typeInitManager, _compilationModuleGroup, _options.IsCppCodeGen);

            // Choose which dependency graph implementation to use based on the amount of logging requested.
            if (_options.DgmlLog == null)
            {
                // No log uses the NoLogStrategy
                _dependencyGraph = new DependencyAnalyzer<NoLogStrategy<NodeFactory>, NodeFactory>(_nodeFactory, null);
            }
            else
            {
                if (_options.FullLog)
                {
                    // Full log uses the full log strategy
                    _dependencyGraph = new DependencyAnalyzer<FullGraphLogStrategy<NodeFactory>, NodeFactory>(_nodeFactory, null);
                }
                else
                {
                    // Otherwise, use the first mark strategy
                    _dependencyGraph = new DependencyAnalyzer<FirstMarkLogStrategy<NodeFactory>, NodeFactory>(_nodeFactory, null);
                }
            }

            _nodeFactory.AttachToDependencyGraph(_dependencyGraph);

            _compilationModuleGroup.AddWellKnownTypes();
            _compilationModuleGroup.AddCompilationRoots();

            if (_options.IsCppCodeGen)
            {
                _cppWriter = new CppCodeGen.CppWriter(this);

                _dependencyGraph.ComputeDependencyRoutine += CppCodeGenComputeDependencyNodeDependencies;

                var nodes = _dependencyGraph.MarkedNodeList;

                _cppWriter.OutputCode(nodes);
            }
            else
            {
                _corInfo = new CorInfoImpl(this);

                _dependencyGraph.ComputeDependencyRoutine += ComputeDependencyNodeDependencies;

                var nodes = _dependencyGraph.MarkedNodeList;

                ObjectWriter.EmitObject(_options.OutputFilePath, nodes, _nodeFactory);
            }

            if (_options.DgmlLog != null)
            {
                using (FileStream dgmlOutput = new FileStream(_options.DgmlLog, FileMode.Create))
                {
                    DgmlWriter.WriteDependencyGraphToStream(dgmlOutput, _dependencyGraph);
                    dgmlOutput.Flush();
                }
            }
        }
        
        #region ICompilationRootProvider implementation

        public void AddMethodCompilationRoot(MethodDesc method, string reason, string exportName = null)
        {
            var methodEntryPoint = _nodeFactory.MethodEntrypoint(method);

            _dependencyGraph.AddRoot(methodEntryPoint, reason);

            if (exportName != null)
                _nodeFactory.NodeAliases.Add(methodEntryPoint, exportName);
        }

        public void AddTypeCompilationRoot(TypeDesc type, string reason)
        {
            _dependencyGraph.AddRoot(_nodeFactory.ConstructedTypeSymbol(type), reason);
        }

        public void AddMainMethodCompilationRoot(EcmaModule module)
        {
            if (StartupCodeMain != null)
                throw new Exception("Multiple entrypoint modules");

            int entryPointToken = module.PEReader.PEHeaders.CorHeader.EntryPointTokenOrRelativeVirtualAddress;
            MethodDesc mainMethod = module.GetMethod(MetadataTokens.EntityHandle(entryPointToken));

            var owningType = module.GetGlobalModuleType();
            StartupCodeMain = new StartupCodeMainMethod(owningType, mainMethod);

            AddMethodCompilationRoot(StartupCodeMain, "Startup Code Main Method", "__managed__Main");
        }

        #endregion

        private void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj)
        {
            foreach (MethodCodeNode methodCodeNodeNeedingCode in obj)
            {
                MethodDesc method = methodCodeNodeNeedingCode.Method;
                string methodName = method.ToString();
                Log.WriteLine("Compiling " + methodName);

                var methodIL = GetMethodIL(method);
                if (methodIL == null)
                    return;

                try
                {
                    _corInfo.CompileMethod(methodCodeNodeNeedingCode);
                }
                catch (Exception e)
                {
                    Log.WriteLine("*** " + method + ": " + e.Message);

                    // Call the __not_yet_implemented method
                    DependencyAnalysis.X64.X64Emitter emit = new DependencyAnalysis.X64.X64Emitter(_nodeFactory);
                    emit.Builder.RequireAlignment(_nodeFactory.Target.MinimumFunctionAlignment);
                    emit.Builder.DefinedSymbols.Add(methodCodeNodeNeedingCode);

                    emit.EmitLEAQ(emit.TargetRegister.Arg0, _nodeFactory.StringIndirection(method.ToString()));
                    DependencyAnalysis.X64.AddrMode loadFromArg0 =
                        new DependencyAnalysis.X64.AddrMode(emit.TargetRegister.Arg0, null, 0, 0, DependencyAnalysis.X64.AddrModeSize.Int64);
                    emit.EmitMOV(emit.TargetRegister.Arg0, ref loadFromArg0);
                    emit.EmitMOV(emit.TargetRegister.Arg0, ref loadFromArg0);

                    emit.EmitLEAQ(emit.TargetRegister.Arg1, _nodeFactory.StringIndirection(e.Message));
                    DependencyAnalysis.X64.AddrMode loadFromArg1 =
                        new DependencyAnalysis.X64.AddrMode(emit.TargetRegister.Arg1, null, 0, 0, DependencyAnalysis.X64.AddrModeSize.Int64);
                    emit.EmitMOV(emit.TargetRegister.Arg1, ref loadFromArg1);
                    emit.EmitMOV(emit.TargetRegister.Arg1, ref loadFromArg1);

                    emit.EmitJMP(_nodeFactory.ExternSymbol("__not_yet_implemented"));
                    methodCodeNodeNeedingCode.SetCode(emit.Builder.ToObjectData());
                    continue;
                }
            }
        }

        private void CppCodeGenComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> obj)
        {
            foreach (CppMethodCodeNode methodCodeNodeNeedingCode in obj)
            {
                _cppWriter.CompileMethod(methodCodeNodeNeedingCode);
            }
        }

        private Dictionary<MethodDesc, DelegateInfo> _delegateInfos = new Dictionary<MethodDesc, DelegateInfo>();
        public DelegateInfo GetDelegateCtor(MethodDesc target)
        {
            DelegateInfo info;

            if (!_delegateInfos.TryGetValue(target, out info))
            {
                _delegateInfos.Add(target, info = new DelegateInfo(this, target));
            }

            return info;
        }

        /// <summary>
        /// Gets an object representing the static data for RVA mapped fields from the PE image.
        /// </summary>
        public object GetFieldRvaData(FieldDesc field)
        {
            return _nodeFactory.ReadOnlyDataBlob(NameMangler.GetMangledFieldName(field),
                ((EcmaField)field).GetFieldRvaData(), _typeSystemContext.Target.PointerSize);
        }

        public bool HasLazyStaticConstructor(TypeDesc type)
        {
            return _typeInitManager.HasLazyStaticConstructor(type);
        }
    }
}
