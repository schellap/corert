using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;
using ILCompiler.Compiler.IL.Stubs;
using System;

namespace Internal.IL.Stubs.Bootstrap
{
    public sealed class BootstrapMainMethod : BootstrapMethod
    {
        private object _data;
        private ILEmitter _emitter;
        private ILCodeStream _codeStream;

        public BootstrapMainMethod(DefType owningType, object data)
            : base(owningType)
        {
            _data = data;
        }

        public override string Name
        {
            get
            {
                return "__Internal_IL_Stubs_BootstrapMain";
            }
        }

        public override MethodIL EmitIL()
        {
            _emitter = new ILEmitter();
            _codeStream = _emitter.NewCodeStream();

            TypeDesc type = Context.SystemModule.GetType("System.Runtime", "BootstrapHelpers");
            MethodDesc main = type.GetMethod("Main", null);

            var fs = new StaticSymbolField(_owningType, _data);
            _codeStream.Emit(ILOpcode.ldsflda, _emitter.NewToken(fs));
            _codeStream.EmitLdArg(0);
            _codeStream.EmitLdArg(1);
            _codeStream.Emit(ILOpcode.call, _emitter.NewToken(main));
            _codeStream.Emit(ILOpcode.ret);

            return _emitter.Link();
        }

        protected override MethodSignature BootstrapMethodSignature()
        {
            return new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Int32),
                new TypeDesc[2] { Context.GetWellKnownType(WellKnownType.Int32), Context.GetWellKnownType(WellKnownType.IntPtr) });
        }
    }
}

