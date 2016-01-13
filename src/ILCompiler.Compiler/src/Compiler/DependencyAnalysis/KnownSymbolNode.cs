// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public class KnownSymbolNode : EmbeddedObjectNode, ISymbolNode
    {
        private ISymbolNode _knownSymbol;

        public KnownSymbolNode(ISymbolNode knownSymbol, NodeFactory factory)
        {
            _knownSymbol = knownSymbol;
        }

        public override string GetName()
        {
            return ((ISymbolNode)this).MangledName;
        }

        protected override void OnMarked(NodeFactory factory)
        {
            factory.KnownSymbols.AddEmbeddedObject(this);
        }

        string ISymbolNode.MangledName
        {
            get
            {
                return "__KnownSymbols_" + _knownSymbol.MangledName;
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            return new DependencyListEntry[] { new DependencyListEntry(_knownSymbol, "KnownSymbol " + _knownSymbol.MangledName) };
        }

        int ISymbolNode.Offset
        {
            get
            {
                return Offset;
            }
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return true;
            }
        }

        public override void EncodeData(ref ObjectDataBuilder builder, NodeFactory factory, bool relocsOnly)
        {
            builder.RequirePointerAlignment();
            builder.EmitPointerReloc(_knownSymbol);
        }
    }
}
