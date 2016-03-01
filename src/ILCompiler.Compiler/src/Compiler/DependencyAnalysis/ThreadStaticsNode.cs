// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.TypeSystem;
using System.Collections.Generic;
using System;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the thread static region of a given type. This is very similar to <see cref="GCStaticsNode"/>,
    /// since the actual storage will be allocated on the GC heap at runtime and is allowed to contain GC pointers.
    /// </summary>
    public class ThreadStaticsNode : ObjectNode, ISymbolNode
    {
        private MetadataType _type;
        public int ThreadStaticBaseOffset;
        private TargetDetails _target;

        public ThreadStaticsNode(MetadataType type, NodeFactory factory)
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
            factory.ThreadStaticEEType(_type, out ThreadStaticBaseOffset);
        }

        string ISymbolNode.MangledName
        {
            get
            {
                return "__ThreadStaticBase_" + NodeFactory.NameMangler.GetMangledTypeName(_type);
            }
        }

        protected override DependencyList ComputeNonRelocationBasedDependencies(NodeFactory context)
        {
            DependencyList result = new DependencyList();
            if (context.TypeInitializationManager.HasEagerStaticConstructor(_type))
            {
                result.Add(new DependencyListEntry(context.EagerCctorIndirection(_type.GetStaticConstructor()), "Eager .cctor"));
            }
            result.Add(new DependencyListEntry(context.ThreadStaticBase, "ThreadStatic Base"));
            return result;
        }

        int ISymbolNode.Offset
        {
            get
            {
                return 0;
            }
        }

        public override bool StaticDependenciesAreComputed
        {
            get
            {
                return true;
            }
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
            builder.EmitInt(ThreadStaticBaseOffset);
            return builder.ToObjectData();
        }
    }
}
