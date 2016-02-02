// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Internal.TypeSystem;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace Internal.IL
{
    internal static class HelperExtensions
    {
        /// <summary>
        /// NEWOBJ operation on String type is actually a call to a static method that returns a String
        /// instance (i.e. there's an explicit call to the runtime allocator from the static method body).
        /// This method returns the alloc+init helper corresponding to a given string constructor.
        /// </summary>
        public static MethodDesc GetStringInitializer(this MethodDesc constructorMethod)
        {
            Debug.Assert(constructorMethod.IsConstructor);
            Debug.Assert(constructorMethod.OwningType.IsString);

            var constructorSignature = constructorMethod.Signature;

            // There's an extra (useless) Object as the first arg to match RyuJIT expectations.
            var parameters = new TypeDesc[constructorSignature.Length + 1];
            parameters[0] = constructorMethod.Context.GetWellKnownType(WellKnownType.Object);
            for (int i = 0; i < constructorSignature.Length; i++)
                parameters[i + 1] = constructorSignature[i];

            MethodSignature sig = new MethodSignature(
                MethodSignatureFlags.Static, 0, constructorMethod.OwningType, parameters);

            MethodDesc result = constructorMethod.OwningType.GetMethod("Ctor", sig);

            // TODO: Better exception type. Should be: "CoreLib doesn't have a required thing in it".
            if (result == null)
                throw new NotImplementedException();

            return result;
        }

        public static MetadataType GetHelperType(this TypeSystemContext context, string name)
        {
            MetadataType helperType = context.SystemModule.GetType("Internal.Runtime.CompilerHelpers", name, false);
            if (helperType == null)
            {
                // TODO: throw the exception that means 'Core Library doesn't have a required thing in it'
                throw new NotImplementedException();
            }

            return helperType;
        }

        public static MethodDesc GetHelperEntryPoint(this TypeSystemContext context, string typeName, string methodName)
        {
            MetadataType helperType = context.GetHelperType(typeName);
            MethodDesc helperMethod = helperType.GetMethod(methodName, null);
            if (helperMethod == null)
            {
                // TODO: throw the exception that means 'Core Library doesn't have a required thing in it'
                throw new NotImplementedException();
            }

            return helperMethod;
        }

        /// <summary>
        /// Emits a call to a throw helper. Use this to emit calls to static parameterless methods that don't return.
        /// The advantage of using this extension method is that you don't have to deal with what code to emit after
        /// the call (e.g. do you need to make sure the stack is balanced?).
        /// </summary>
        public static void EmitCallThrowHelper(this ILCodeStream codeStream, ILEmitter emitter, MethodDesc method)
        {
            Debug.Assert(method.Signature.Length == 0 && method.Signature.IsStatic);

            // Emit a call followed by a branch to the call.

            // We are emitting this instead of emitting a tight loop that jumps to itself
            // so that the JIT doesn't generate extra GC checks within the loop.

            ILCodeLabel label = emitter.NewCodeLabel();
            codeStream.EmitLabel(label);
            codeStream.Emit(ILOpcode.call, emitter.NewToken(method));
            codeStream.Emit(ILOpcode.br, label);
        }
    }
}
