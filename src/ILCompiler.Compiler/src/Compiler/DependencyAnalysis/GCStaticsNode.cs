// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public class GCStaticsNode : ObjectNode, ISymbolNode
    {
        private MetadataType _type;
        public int GCStaticBaseOffset;
        private TargetDetails _target;

        public GCStaticsNode(MetadataType type, NodeFactory factory)
        {
            _type = type;
            _target = factory.Target;
        }

        public override string GetName()
        {
            return ((ISymbolNode)this).MangledName;
        }

        protected override void OnMarked(NodeFactory factory)
        {
            factory.GCStaticEEType(_type, out GCStaticBaseOffset);
        }

        string ISymbolNode.MangledName
        {
            get
            {
                return "__GCStaticBase_" + NodeFactory.NameMangler.GetMangledTypeName(_type);
            }
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory context)
        {
            DependencyList result = new DependencyList();
            if (context.TypeInitializationManager.HasEagerStaticConstructor(_type))
            {
                result.Add(new DependencyListEntry(context.EagerCctorIndirection(_type.GetStaticConstructor()), "Eager .cctor"));
            }
            result.Add(new DependencyListEntry(context.GCStaticBase, "GCStatic Base"));
            return result;
        }

        public override string Section
        {
            get
            {
                if (_target.IsWindows)
                    return "rdata";
                else
                    return "data";
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectDataBuilder builder = new ObjectDataBuilder(factory);
            builder.RequirePointerAlignment();
            builder.DefinedSymbols.Add(this);
            builder.EmitInt(GCStaticBaseOffset);
            return builder.ToObjectData();
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return true;
            }
        }

        public int Offset
        {
            get
            {
                return 0;
            }
        }
    }
}
