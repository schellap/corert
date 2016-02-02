// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    // This node represents the concept of a virtual method being used.
    // It has no direct depedencies, but may be referred to by conditional static 
    // dependencies, or static dependencies from elsewhere.
    //
    // It is used to keep track of uses of virtual methods to ensure that the
    // vtables are properly constructed
    internal class VirtualMethodUseNode : DependencyNodeCore<NodeFactory>
    {
        private MethodDesc _decl;

        public VirtualMethodUseNode(MethodDesc decl)
        {
            _decl = decl;
        }

        public override string GetName()
        {
            return "VirtualMethodUse" + _decl.ToString();
        }

        protected override void OnMarked(NodeFactory factory)
        {
            // For each virtual method use in the graph, ensure that our side
            // table of live virtual method slots is kept up to date.

            MethodDesc virtualMethod = (MethodDesc)_decl;
            TypeDesc typeOfVirtual = virtualMethod.OwningType;

            List<MethodDesc> virtualSlots;
            if (!factory.VirtualSlots.TryGetValue(typeOfVirtual, out virtualSlots))
            {
                virtualSlots = new List<MethodDesc>();
                factory.VirtualSlots.Add(typeOfVirtual, virtualSlots);
            }
            if (!virtualSlots.Contains(virtualMethod))
            {
                virtualSlots.Add(virtualMethod);
            }
        }

        public override bool HasConditionalStaticDependencies
        {
            get
            {
                return false;
            }
        }

        public override bool HasDynamicDependencies
        {
            get
            {
                return false;
            }
        }

        public override bool InterestingForDynamicDependencyAnalysis
        {
            get
            {
                return false;
            }
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return true;
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            return null;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            return null;
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context)
        {
            return null;
        }
    }
}
