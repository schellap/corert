// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Internal.TypeSystem;
using Internal.IL;
using ILCompiler.Compiler.IL.Stubs;
using Internal.IL.Stubs.Bootstrap;

namespace Internal.IL.Stubs
{
    public class StaticSymbolField : FieldDesc
    {
        private DefType _owningType;
        private object _symbol;

        public StaticSymbolField(DefType owningType, object symbol)
        {
            _owningType = owningType;
            _symbol = symbol;
        }

        public object Symbol
        {
            get
            {
                return _symbol;
            }
        }

        public override int Offset
        {
            get
            {
                return 0;
            }
        }

        public override TypeDesc FieldType
        {
            get
            {
                return this.Context.GetWellKnownType(WellKnownType.IntPtr);
            }
        }

        public override bool IsThreadStatic
        {
            get
            {
                return false;
            }
        }
        public override bool HasRva
        {
            get
            {
                return true;
            }
        }
        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }
        public override bool IsInitOnly
        {
            get
            {
                return false;
            }
        }
        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return false;
        }
        public override bool IsLiteral
        {
            get
            {
                return false;
            }
        }

        public override DefType OwningType
        {
            get
            {
                return _owningType;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _owningType.Context;
            }
        }
    }
}
