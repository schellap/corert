using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;
using ILCompiler.Compiler.IL.Stubs;
using System;

namespace Internal.IL.Stubs.Bootstrap
{
    public sealed class BootstrapMainMethod : BootstrapMethod
    {
        private BootstrapData _data;
        private ILEmitter _emitter;
        private ILCodeStream _codeStream;

        public BootstrapMainMethod(BootstrapData data)
            : base(data.OwningType)
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

            object[] symbols = {
                _data.StringFixupStart,
                _data.StringFixupEnd,
                _data.StringEEType
            };

            var totalLocalNum = _emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Array));

            _codeStream.EmitLdc(symbols.Length + 1);
            _codeStream.Emit(ILOpcode.newarr, _emitter.NewToken(Context.GetWellKnownType(WellKnownType.IntPtr)));
            _codeStream.Emit(ILOpcode.stloc_0);

            int current = 0;
            foreach (var symbol in symbols)
            {
                var fs = new StaticSymbolField(_data.OwningType, symbol);
                current = AppendToSymbolArray(current, ILOpcode.ldsflda, _emitter.NewToken(fs));
            }
            current = AppendToSymbolArray(current, ILOpcode.ldftn, _emitter.NewToken(_data.MainMethod));

            _codeStream.Emit(ILOpcode.ldloc_0);
            _codeStream.EmitLdArg(0);
            _codeStream.EmitLdArg(1);
            _codeStream.Emit(ILOpcode.call, _emitter.NewToken(main));
            _codeStream.Emit(ILOpcode.ret);

            return _emitter.Link();
        }

        private int AppendToSymbolArray(int index, ILOpcode ldSymbolOp, ILToken symbol)
        {
            var intPtrType = Context.GetWellKnownType(WellKnownType.IntPtr);

            _codeStream.Emit(ILOpcode.ldloc_0);
            _codeStream.EmitLdc(index++);
            _codeStream.Emit(ILOpcode.ldelema, _emitter.NewToken(intPtrType));
            _codeStream.Emit(ldSymbolOp, symbol);
            _codeStream.Emit(ILOpcode.stobj, _emitter.NewToken(intPtrType));

            return index;
        }

        protected override MethodSignature BootstrapMethodSignature()
        {
            return new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Int32),
                new TypeDesc[2] { Context.GetWellKnownType(WellKnownType.Int32), Context.GetWellKnownType(WellKnownType.IntPtr) });
        }
    }
}

