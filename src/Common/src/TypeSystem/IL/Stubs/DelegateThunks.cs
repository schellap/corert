// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.TypeSystem;
using Internal.IL;

namespace Internal.IL.Stubs
{
    public sealed class DelegateShuffleThunk : ILStubMethod
    {
        private MethodDesc _target;
        private MethodSignature _signature;

        internal DelegateShuffleThunk(MethodDesc target)
        {
            _target = target;
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _target.Context;
            }
        }

        public override TypeDesc OwningType
        {
            get
            {
                return _target.OwningType;
            }
        }

        public override MethodSignature Signature
        {
            get
            {
                if (_signature == null)
                {
                    MethodSignature template = _target.Signature;
                    MethodSignatureBuilder builder = new MethodSignatureBuilder(template);

                    builder.Flags = 0;

                    _signature = builder.ToSignature();
                }

                return _signature;
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();

            var codeStream = emitter.NewCodeStream();

            for (int i = 0; i < Signature.Length; i++)
            {
                codeStream.EmitLdArg(i + 1);
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(_target));
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link();
        }

        public override Instantiation Instantiation
        {
            get
            {
                return _target.Instantiation;
            }
        }

        public override string Name
        {
            get
            {
                return "__DelegateShuffleThunk__" + _target.Name;
            }
        }
    }
}
