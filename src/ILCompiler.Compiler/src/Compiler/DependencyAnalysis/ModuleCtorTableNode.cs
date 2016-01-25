using System;
using System.Collections.Generic;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    internal class ModuleCtorTableNode : EmbeddedObjectNode, ISymbolNode
    {
        private ModuleDesc _module;
        private MethodDesc _method;

        public ModuleCtorTableNode(ModuleDesc module)
        {
            _module = module;
            _method = _module.GetGlobalModuleType().GetStaticConstructor();
        }

        public string MangledName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        int ISymbolNode.Offset
        {
            get
            {
                return base.Offset;
            }
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return true;
            }
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            dataBuilder.RequirePointerAlignment();

            ISymbolNode methodCodeNode = factory.MethodEntrypoint(_method);

            dataBuilder.EmitPointerReloc(methodCodeNode);
        }

        public override string GetName()
        {
            return ((ISymbolNode)this).MangledName;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            EcmaModule module = (EcmaModule)_module;
            foreach (EcmaModule dep in module.GetAssemblyReferences())
            {
                yield return new DependencyListEntry(context.ModuleCtorIndirection(module), "Module cctor assembly ref");
            };
            yield return new DependencyListEntry(context.MethodEntrypoint(_method), "Module cctor");
        }

        protected override void OnMarked(NodeFactory context)
        {
            context.ModuleCtorTable.AddEmbeddedObject(this);
        }

    }
}