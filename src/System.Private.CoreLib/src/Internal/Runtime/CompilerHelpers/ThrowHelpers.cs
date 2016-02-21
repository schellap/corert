﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to throw exceptions from generated code.
    /// </summary>
    internal static class ThrowHelpers
    {
        private static void ThrowOverflowException()
        {
            throw new IndexOutOfRangeException();
        }

        private static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        private static void ThrowNullReferenceException()
        {
            throw new IndexOutOfRangeException();
        }

        private static void ThrowDivideByZeroException()
        {
            throw new IndexOutOfRangeException();
        }

        private static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }
    }
}
