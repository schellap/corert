// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Internal.Metadata.NativeFormat.Writer;

using Cts = Internal.TypeSystem;
using Ecma = System.Reflection.Metadata;

using Debug = System.Diagnostics.Debug;
using MethodAttributes = System.Reflection.MethodAttributes;
using MethodImplAttributes = System.Reflection.MethodImplAttributes;

namespace ILCompiler.Metadata
{
    partial class Transform<TPolicy>
    {
        internal EntityMap<Cts.MethodDesc, MetadataRecord> _methods
            = new EntityMap<Cts.MethodDesc, MetadataRecord>(EqualityComparer<Cts.MethodDesc>.Default);

        private Action<Cts.MethodDesc, Method> _initMethodDef;
        private Action<Cts.MethodDesc, MemberReference> _initMethodRef;

        public override MetadataRecord HandleQualifiedMethod(Cts.MethodDesc method)
        {
            MetadataRecord rec;

            if (_policy.GeneratesMetadata(method))
            {
                rec = new QualifiedMethod
                {
                    EnclosingType = (TypeDefinition)HandleType(method.OwningType),
                    Method = HandleMethodDefinition(method),
                };
            }
            else
            {
                rec = HandleMethodReference(method);
            }

            Debug.Assert(rec is QualifiedMethod || rec is MemberReference);

            return rec;
        }

        private Method HandleMethodDefinition(Cts.MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            Debug.Assert(_policy.GeneratesMetadata(method));
            return (Method)_methods.GetOrCreate(method, _initMethodDef ?? (_initMethodDef = InitializeMethodDefinition));
        }

        private void InitializeMethodDefinition(Cts.MethodDesc entity, Method record)
        {
            record.Name = HandleString(entity.Name);
            record.Signature = HandleMethodSignature(entity.Signature);

            if (entity.HasInstantiation)
            {
                record.GenericParameters.Capacity = entity.Instantiation.Length;
                foreach (var p in entity.Instantiation)
                    record.GenericParameters.Add(HandleGenericParameter((Cts.GenericParameterDesc)p));
            }

            if (entity.Signature.Length > 0)
            {
                record.Parameters.Capacity = entity.Signature.Length;
                for (ushort i = 0; i < entity.Signature.Length; i++)
                {
                    record.Parameters.Add(new Parameter
                    {
                        Sequence = i
                    });
                }

                var ecmaEntity = entity as Cts.Ecma.EcmaMethod;
                if (ecmaEntity != null)
                {
                    Ecma.MetadataReader reader = ecmaEntity.MetadataReader;
                    Ecma.MethodDefinition methodDef = reader.GetMethodDefinition(ecmaEntity.Handle);
                    Ecma.ParameterHandleCollection paramHandles = methodDef.GetParameters();

                    Debug.Assert(paramHandles.Count == entity.Signature.Length);

                    int i = 0;
                    foreach (var paramHandle in paramHandles)
                    {
                        Ecma.Parameter param = reader.GetParameter(paramHandle);
                        record.Parameters[i].Flags = param.Attributes;
                        record.Parameters[i].Name = HandleString(reader.GetString(param.Name));

                        Ecma.ConstantHandle defaultValue = param.GetDefaultValue();
                        if (!defaultValue.IsNil)
                        {
                            record.Parameters[i].DefaultValue = HandleConstant(ecmaEntity.Module, defaultValue);
                        }

                        // TODO: CustomAttributes

                        i++;
                    }
                }
            }

            record.Flags = GetMethodAttributes(entity);
            record.ImplFlags = GetMethodImplAttributes(entity);
            
            //TODO: RVA
            //TODO: CustomAttributes
        }

        private MemberReference HandleMethodReference(Cts.MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            Debug.Assert(!_policy.GeneratesMetadata(method));
            return (MemberReference)_methods.GetOrCreate(method, _initMethodRef ?? (_initMethodRef = InitializeMethodReference));
        }

        private void InitializeMethodReference(Cts.MethodDesc entity, MemberReference record)
        {
            record.Name = HandleString(entity.Name);
            record.Parent = HandleType(entity.OwningType);
            record.Signature = HandleMethodSignature(entity.Signature);
        }

        public override MethodSignature HandleMethodSignature(Cts.MethodSignature signature)
        {
            // TODO: if Cts.MethodSignature implements Equals/GetHashCode, we could enable pooling here.

            var result = new MethodSignature
            {
                // TODO: CallingConvention
                GenericParameterCount = signature.GenericParameterCount,
                ReturnType = new ReturnTypeSignature
                {
                    // TODO: CustomModifiers
                    Type = HandleType(signature.ReturnType)
                },
                // TODO-NICE: VarArgParameters
            };

            result.Parameters.Capacity = signature.Length;
            for (int i = 0; i < signature.Length; i++)
            {
                result.Parameters.Add(HandleParameterTypeSignature(signature[i]));
            }

            return result;
        }

        private MethodAttributes GetMethodAttributes(Cts.MethodDesc method)
        {
            var ecmaMethod = method as Cts.Ecma.EcmaMethod;
            if (ecmaMethod != null)
            {
                Ecma.MetadataReader reader = ecmaMethod.MetadataReader;
                Ecma.MethodDefinition methodDef = reader.GetMethodDefinition(ecmaMethod.Handle);
                return methodDef.Attributes;
            }
            else
                throw new NotImplementedException();
        }

        private MethodImplAttributes GetMethodImplAttributes(Cts.MethodDesc method)
        {
            var ecmaMethod = method as Cts.Ecma.EcmaMethod;
            if (ecmaMethod != null)
            {
                Ecma.MetadataReader reader = ecmaMethod.MetadataReader;
                Ecma.MethodDefinition methodDef = reader.GetMethodDefinition(ecmaMethod.Handle);
                return methodDef.ImplAttributes;
            }
            else
                throw new NotImplementedException();
        }
    }
}
