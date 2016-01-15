using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Internal.NativeFormat;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace System.Runtime.Bootstrap
{
    internal static class StringFixup
    {
        private class StringLiteralLoadException : Exception
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int CStrLen(byte* str)
        {
            int len = 0;
            for (; str[len] != 0; len++) { }
            return len;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe char[] UTF8ToWideChar(byte* bytes, int len)
        {
            int count = BootstrapUTF8Encoding.GetCharCount(bytes, len);
            Contract.Assert(count >= 0) ;

            char[] wchars = new char[count];
            fixed (char* converted = wchars)
            {
                int newCount = BootstrapUTF8Encoding.GetChars(bytes, len, converted, count);
                Contract.Assert(newCount == count);
            }
            return wchars;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string AllocateString(char[] chars, int len, IntPtr eeType)
        {
            string newStr = RuntimeImports.RhNewArrayAsString(new EETypePtr(eeType), len);
            fixed (char* dest = newStr, source = chars)
            {
                string.wstrcpy(dest, source, chars.Length);
            }
            return newStr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string AllocateString(byte* bytes, int len, IntPtr eeType)
        {
            string newStr = RuntimeImports.RhNewArrayAsString(new EETypePtr(eeType), len);
            fixed (char* dest = newStr)
            {
                for (int i = 0; i < len; ++i)
                    dest[i] = (char)bytes[i];
            }
            return newStr;
        }

        internal static unsafe string[] GetCommandLine(int argc, IntPtr argv, IntPtr eeType)
        {
            string[] args = new string[argc];
            for (int i = 0; i < argc; ++i)
            {
                byte* argval = ((byte**)argv)[i];
                int len = CStrLen(argval);
                args[i] = AllocateString(argval, len, eeType);
            }
            return args;
        }

        public static unsafe void Initialize(IntPtr start, IntPtr end, IntPtr eeType)
        {
            for (IntPtr* tab = (IntPtr*)start; tab < (IntPtr*)end; tab++)
            {
                byte* bytes = (byte*)*tab;

                int len = (int)NativePrimitiveDecoder.DecodeUnsigned(ref bytes);

                if (len < 0)
                {
                    throw new StringLiteralLoadException();
                }

                char[] chars = UTF8ToWideChar(bytes, len);

                GCHandle handle = GCHandle.Alloc(AllocateString(chars, len, eeType));
                *tab = (IntPtr)handle;
            }
        }
    }
}