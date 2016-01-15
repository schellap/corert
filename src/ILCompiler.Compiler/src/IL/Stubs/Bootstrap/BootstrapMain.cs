using Internal.TypeSystem;
using Internal.IL;
using Internal.IL.Stubs;
using ILCompiler.Compiler.IL.Stubs;
using System;

namespace Internal.IL.Stubs.Bootstrap
{
    /// <summary>
    /// Bootstrap code that does initialization, Main invocation
    /// and shutdown of the runtime.
    /// </summary>
    public sealed class BootstrapMainMethod : BootstrapMethod
    {
        private MethodDesc _mainMethod;

        public BootstrapMainMethod(DefType owningType, MethodDesc mainMethod)
            : base(owningType)
        {
            _mainMethod = mainMethod;
        }

        public override string Name
        {
            get
            {
                return "Internal_IL_Stubs_BootstrapMain";
            }
        }

        public override MethodIL EmitIL()
        {
            ILEmitter emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();
            ILLocalVariable returnValue = emitter.NewLocal(Context.GetWellKnownType(WellKnownType.Int32));

            TypeDesc bootstrap = Context.SystemModule.GetType("System.Runtime", "BootstrapHelpers");

            codeStream.Emit(ILOpcode.call, emitter.NewToken(bootstrap.GetMethod("Initialize", null)));
            if (_mainMethod.Signature.Length > 0)
            {
                MethodDesc getArgs = bootstrap.GetMethod("GetCommandLineArgs", null);
                codeStream.Emit(ILOpcode.ldarg_0); // argc
                codeStream.Emit(ILOpcode.ldarg_1); // argv
                codeStream.Emit(ILOpcode.call, emitter.NewToken(getArgs));
            }

            codeStream.Emit(ILOpcode.call, emitter.NewToken(_mainMethod));

            if (_mainMethod.Signature.ReturnType == Context.GetWellKnownType(WellKnownType.Void))
            {
                // Get default exit code if void return;
                TypeDesc environment = Context.SystemModule.GetType("System", "Environment");
                MethodDesc defaultExitCode = environment.GetMethod("get_ExitCode", null);
                codeStream.Emit(ILOpcode.call, emitter.NewToken(defaultExitCode));
            }
            codeStream.EmitStLoc(returnValue);

            codeStream.Emit(ILOpcode.call, emitter.NewToken(bootstrap.GetMethod("Shutdown", null)));

            codeStream.EmitLdLoc(returnValue);
            codeStream.Emit(ILOpcode.ret);

            return emitter.Link();
        }

        protected override MethodSignature GetBootstrapMethodSignature()
        {
            return new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Int32),
                new TypeDesc[2] { Context.GetWellKnownType(WellKnownType.Int32), Context.GetWellKnownType(WellKnownType.IntPtr) });
        }
    }
}

