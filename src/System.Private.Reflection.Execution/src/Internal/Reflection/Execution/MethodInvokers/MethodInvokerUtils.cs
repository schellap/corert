// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using global::System;
using global::System.Threading;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;
using global::System.Runtime.CompilerServices;
using global::Internal.Runtime.Augments;

using TargetException = System.ArgumentException;

namespace Internal.Reflection.Execution.MethodInvokers
{
    internal static class MethodInvokerUtils
    {
        public static void ValidateThis(Object thisObject, RuntimeTypeHandle declaringTypeHandle)
        {
            if (thisObject == null)
                throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
            RuntimeTypeHandle srcTypeHandle = thisObject.GetType().TypeHandle;
            if (RuntimeAugments.IsAssignableFrom(declaringTypeHandle, srcTypeHandle))
                return;

            // if Object supports ICastable interface and the declaringType is interface
            // try to call ICastable.IsInstanceOfInterface to determine whether object suppport declaringType interface
            if (RuntimeAugments.IsInterface(declaringTypeHandle))
            {
                ICastable castable = thisObject as ICastable;
                Exception castError;

                // ICastable.IsInstanceOfInterface isn't supposed to throw exception
                if (castable != null && castable.IsInstanceOfInterface(declaringTypeHandle, out castError))
                    return;
            }

            throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
        }
    }
}

