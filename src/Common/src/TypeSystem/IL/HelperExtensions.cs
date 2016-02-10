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

            MethodDesc result = constructorMethod.OwningType.GetKnownMethod("Ctor", sig);
            return result;
        }

        public static MetadataType GetHelperType(this TypeSystemContext context, string name)
        {
            MetadataType helperType = context.SystemModule.GetKnownType("Internal.Runtime.CompilerHelpers", name);
            return helperType;
        }

        public static MethodDesc GetHelperEntryPoint(this TypeSystemContext context, string typeName, string methodName)
        {
            MetadataType helperType = context.GetHelperType(typeName);
            MethodDesc helperMethod = helperType.GetKnownMethod(methodName, null);
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

        /// <summary>
        /// Retrieves a method on <paramref name="type"/> that is well known to the compiler.
        /// Throws an exception if the method doesn't exist.
        /// </summary>
        public static MethodDesc GetKnownMethod(this TypeDesc type, string name, MethodSignature signature)
        {
            MethodDesc method = type.GetMethod(name, signature);
            if (method == null)
            {
                throw new InvalidOperationException(String.Format("Expected method '{0}' not found on type '{1}'", name, type));
            }

            return method;
        }

        /// <summary>
        /// Retrieves a field on <paramref name="type"/> that is well known to the compiler.
        /// Throws an exception if the field doesn't exist.
        /// </summary>
        public static FieldDesc GetKnownField(this TypeDesc type, string name)
        {
            FieldDesc field = type.GetField(name);
            if (field == null)
            {
                throw new InvalidOperationException(String.Format("Expected field '{0}' not found on type '{1}'", name, type));
            }

            return field;
        }

        /// <summary>
        /// Retrieves a namespace type in <paramref name= "module" /> that is well known to the compiler.
        /// Throws an exception if the type doesn't exist.
        /// </summary>
        public static MetadataType GetKnownType(this ModuleDesc module, string @namespace, string name)
        {
            MetadataType type = module.GetType(@namespace, name, false);
            if (type == null)
            {
                throw new InvalidOperationException(
                    String.Format("Expected type '{0}' not found in module '{1}'",
                    module,
                    @namespace.Length > 0 ? String.Concat(@namespace, ".", name) : name));
            }

            return type;
        }
    }
}
