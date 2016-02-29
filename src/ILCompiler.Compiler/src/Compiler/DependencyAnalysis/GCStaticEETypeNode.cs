// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Internal.TypeSystem;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis
{
    internal class GCStaticEETypeNode : ObjectNode, ISymbolNode
    {
        private List<int> _runLengths = new List<int>(); // First is offset to first gc field, second is length of gc static run, third is length of non-gc data, etc
        private int _targetPointerSize;
        private TargetDetails _target;
        private int _currentOffsetFromBase;

        public GCStaticEETypeNode(NodeFactory factory)
        {
            _targetPointerSize = factory.Target.PointerSize;
            _target = factory.Target;
        }

        //public int Add(MetadataType type)
        //{
        //    _types.Add(type);
        //    int previousOffset = _currentOffsetFromBase;
        //    _currentOffsetFromBase += _fieldSizeGetter(type);
        //    return previousOffset;
        //}
        public int AddGCDesc(bool[] gcDesc)
        {
            bool encodingGCPointers = false;
            int currentPointerCount = 0;
            foreach (bool pointerIsGC in gcDesc)
            {
                if (encodingGCPointers == pointerIsGC)
                {
                    currentPointerCount++;
                }
                else
                {
                    _runLengths.Add(currentPointerCount * _target.PointerSize);
                    encodingGCPointers = pointerIsGC;
                }
            }
            _runLengths.Add(currentPointerCount);
            int previousOffset = _currentOffsetFromBase;
            _currentOffsetFromBase += gcDesc.Length;
            return previousOffset;
        }

        public override string GetName()
        {
            return ((ISymbolNode)this).MangledName;
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
                StringBuilder nameBuilder = new StringBuilder();
                nameBuilder.Append(NodeFactory.NameMangler.CompilationUnitPrefix + "__GCStaticBaseEEType");
                return nameBuilder.ToString();
            }
        }

        int ISymbolNode.Offset
        {
            get
            {
                if (NumSeries > 0)
                {
                    return _targetPointerSize * ((NumSeries * 2) + 1);
                }
                else
                {
                    return 0;
                }
            }
        }

        private int NumSeries
        {
            get
            {
                return (_runLengths.Count - 1) / 2;
            }
        }

        public override ObjectData GetData(NodeFactory factory, bool relocsOnly)
        {
            ObjectDataBuilder dataBuilder = new ObjectDataBuilder(factory);
            dataBuilder.Alignment = 16;
            dataBuilder.DefinedSymbols.Add(this);

            bool hasPointers = NumSeries > 0;
            if (hasPointers)
            {
                for (int i = ((_runLengths.Count / 2) * 2) - 1; i >= 0; i--)
                {
                    if (_targetPointerSize == 4)
                    {
                        dataBuilder.EmitInt(_runLengths[i]);
                    }
                    else
                    {
                        dataBuilder.EmitLong(_runLengths[i]);
                    }
                }
                if (_targetPointerSize == 4)
                {
                    dataBuilder.EmitInt(NumSeries);
                }
                else
                {
                    dataBuilder.EmitLong(NumSeries);
                }
            }

            int totalSize = 0;
            foreach (int run in _runLengths)
            {
                totalSize += run * _targetPointerSize;
            }

            dataBuilder.EmitShort(0); // ComponentSize is always 0

            if (hasPointers)
                dataBuilder.EmitShort(0x20); // TypeFlags.HasPointers
            else
                dataBuilder.EmitShort(0x00);

            totalSize = Math.Max(totalSize, _targetPointerSize * 3); // minimum GC eetype size is 3 pointers
            dataBuilder.EmitInt(totalSize);

            return dataBuilder.ToObjectData();
        }
    }
}
