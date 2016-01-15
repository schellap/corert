// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Internal.NativeFormat;
using System.Runtime.Bootstrap;

namespace System.Runtime
{
    internal static class BootstrapHelpers
    {
        internal static IntPtr s_strEETypePtr = IntPtr.Zero;
        internal static IntPtr s_strTableStart = IntPtr.Zero;
        internal static IntPtr s_strTableEnd = IntPtr.Zero;

        internal enum ModuleSectionIds
        {
            StringEETypePtr,
            StringFixupStart
        };

        internal static void Initialize()
        {
            InitializeStringTable();
        }

        internal static void Shutdown()
        {

        }

        internal static string[] GetCommandLineArgs(int argc, IntPtr argv)
        {
            int length = 0;
            System.Diagnostics.Debug.Assert(length == IntPtr.Size);
            return StringFixup.GetCommandLine(argc, argv, s_strEETypePtr);
        }

        private static unsafe void InitializeStringTable()
        {
            int length = 0;
            s_strEETypePtr = GetModuleSection((int)ModuleSectionIds.StringEETypePtr, out length);
            System.Diagnostics.Debug.Assert(length == IntPtr.Size);

            s_strTableStart = GetModuleSection((int)ModuleSectionIds.StringFixupStart, out length);
            System.Diagnostics.Debug.Assert(length % IntPtr.Size == 0);

            s_strTableEnd = (IntPtr)((byte*)s_strTableStart + length);

            StringFixup.Initialize(s_strTableStart, s_strTableEnd, s_strEETypePtr);
        }

        [RuntimeImport(".", "GetModuleSection")]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecurityCritical] // required to match contract
        private static extern IntPtr GetModuleSection(int id, out int length);
    }
}
