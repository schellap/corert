// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public class StringIndirectionNode : EmbeddedObjectNode, ISymbolNode
    {
        public string _data;

        public StringIndirectionNode(string data)
        {
            _data = data;
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return true;
            }
        }

        string ISymbolNode.MangledName
        {
            get
            {
                return NodeFactory.NameMangler.CompilationUnitPrefix + "__str" + Offset.ToString(CultureInfo.InvariantCulture);
            }
        }

        public override void EncodeData(ref ObjectDataBuilder dataBuilder, NodeFactory factory, bool relocsOnly)
        {
            dataBuilder.RequirePointerAlignment();

            StringDataNode stringDataNode = factory.StringData(_data);
            if (!relocsOnly)
                stringDataNode.SetId(base.Offset);

            dataBuilder.EmitPointerReloc(stringDataNode);
        }

        public override string GetName()
        {
            return ((ISymbolNode)this).MangledName;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            return new DependencyListEntry[] { new DependencyListEntry(context.StringData(_data), "string contents") };
        }

        protected override void OnMarked(NodeFactory context)
        {
            context.StringTable.AddEmbeddedObject(this);
        }
    }
}
