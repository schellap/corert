﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    internal class MethodCodeNode : ObjectNode, INodeWithFrameInfo, INodeWithDebugInfo, ISymbolNode
    {
        private MethodDesc _method;
        private ObjectData _methodCode;
        private FrameInfo[] _frameInfos;
        private DebugLocInfo[] _debugLocInfos;

        public MethodCodeNode(MethodDesc method)
        {
            Debug.Assert(!method.IsAbstract);
            _method = method;
        }

        public void SetCode(ObjectData data)
        {
            Debug.Assert(_methodCode == null);
            _methodCode = data;
        }

        public MethodDesc Method
        {
            get
            {
                return _method;
            }
        }
        public override string GetName()
        {
            return ((ISymbolNode)this).MangledName;
        }

        public override string Section
        {
            get
            {
                return "text";
            }
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return _methodCode != null;
            }
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory factory)
        {
            DependencyList dependencyList = new DependencyNodeCore<NodeFactory>.DependencyList();
            if (_method.OwningType is MetadataType)
            {
                var module = ((MetadataType)_method.OwningType).Module;
                MethodDesc cctor = module.GetGlobalModuleType().GetStaticConstructor();
                if (cctor != null)
                {
                    dependencyList.Add(factory.ModuleCtorIndirection(module), "Module initializer");
                }
            }
            return dependencyList;
        }


        string ISymbolNode.MangledName
        {
            get
            {
                return NodeFactory.NameMangler.GetMangledMethodName(_method);
            }
        }

        int ISymbolNode.Offset
        {
            get
            {
                return 0;
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            return _methodCode;
        }

        public FrameInfo[] FrameInfos
        {
            get
            {
                return _frameInfos;
            }
        }

        public void InitializeFrameInfos(FrameInfo[] frameInfos)
        {
            Debug.Assert(_frameInfos == null);
            _frameInfos = frameInfos;
        }

        public DebugLocInfo[] DebugLocInfos
        {
            get
            {
                return _debugLocInfos;
            }
        }

        public void InitializeDebugLocInfos(DebugLocInfo[] debugLocInfos)
        {
            Debug.Assert(_debugLocInfos == null);
            _debugLocInfos = debugLocInfos;
        }
    }
}
