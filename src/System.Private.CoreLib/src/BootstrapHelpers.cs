// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime
{
    internal static class BootstrapHelpers
    {
        private static unsafe int DecodeUnsigned(byte** ppStream, byte* pStreamEnd, uint* pValue)
        {
            if (*ppStream >= pStreamEnd)
                return -1;

            uint value = 0;
            uint val = **ppStream;
            if ((val & 1) == 0)
            {
                value = (val >> 1);
                *ppStream += 1;
            }
            else if ((val & 2) == 0)
            {
                if (*ppStream + 1 >= pStreamEnd)
                    return -1;

                value = (val >> 2) |
                    (((uint) * (*ppStream + 1)) << 6);
                *ppStream += 2;
            }
            else if ((val & 4) == 0)
            {
                if (*ppStream + 2 >= pStreamEnd)
                    return -1;

                value = (val >> 3) |
                    (((uint) * (*ppStream + 1)) << 5) |
                    (((uint) * (*ppStream + 2)) << 13);
                *ppStream += 3;
            }
            else if ((val & 8) == 0)
            {
                if (*ppStream + 3 >= pStreamEnd)
                    return -1;

                value = (val >> 4) |
                    (((uint) * (*ppStream + 1)) << 4) |
                    (((uint) * (*ppStream + 2)) << 12) |
                    (((uint) * (*ppStream + 3)) << 20);
                *ppStream += 4;
            }
            else if ((val & 16) == 0)
            {
                if (*ppStream + 4 >= pStreamEnd)
                    return -1;
                *ppStream += 1;
                value = *(uint*)(*ppStream); // Assumes little endian and unaligned access
                *ppStream += 4;
            }
            else
            {
                return -1;
            }

            *pValue = value;
            return 0;
        }

        private class StringLiteralLoadException : Exception
        {
        }

        enum SymbolIds
        {
            StringFixupStart,
            StringFixupEnd,
            StringEEPtrType,
            MainMethod
        };

        private static unsafe int CStrLen(byte* str)
        {
            int len = 0;
            for (; str[len] != 0; len++, str++) { }
            return len;
        }

        private static unsafe char[] UTF8ToWideChar(byte* bytes, int len)
        {
            return AsciiToWideChar(bytes, len);
        }

        private static unsafe char[] AsciiToWideChar(byte* bytes, int len)
        {
            // TODO: Convert UTF8 to wide char.
            char[] chars = new char[len];
            for (int i = 0; i < len; ++i)
            {
                chars[i] = (char)bytes[i];
            }
            return chars;
        }

        private static unsafe string AllocateString(char[] chars, int len, IntPtr eeType)
        {
            string newStr = RuntimeImports.RhNewArrayAsString(new EETypePtr(eeType), len);
            fixed (char* dest = newStr, source = chars)
            {
                string.wstrcpy(dest, source, chars.Length);
            }
            return newStr;
        }

        private static unsafe string[] GetCommandLine(int argc, IntPtr argv, IntPtr eeType)
        {
            string[] args = new string[argc];
            for (int i = 0; i < argc; ++i)
            {
                byte** argval = (byte**)argv;
                int len = CStrLen(*argval);
                args[i] = AllocateString(AsciiToWideChar(*argval, len), len, eeType);
            }
            return args;
        }

        private static unsafe void StringLiteralFixup(IntPtr start, IntPtr end, IntPtr eeType)
        {
            IntPtr* s = (IntPtr*)start;
            IntPtr* e = (IntPtr*)end;
            for (IntPtr* tab = s; tab < e; tab++)
            {
                byte* bytes = (byte*) *tab;
                int len;
                if (DecodeUnsigned(&bytes, (byte*)bytes + 5, (uint*)&len) == -1)
                {
                    throw new StringLiteralLoadException();
                }

                char[] chars = UTF8ToWideChar(bytes, len);

                GCHandle handle = GCHandle.Alloc(AllocateString(chars, len, eeType));
                *tab = (IntPtr)handle;
            }
        }

        private static unsafe int Main(IntPtr[] symbols, int argc, IntPtr argv)
        {
            StringLiteralFixup(
                symbols[(int)SymbolIds.StringFixupStart],
                symbols[(int)SymbolIds.StringFixupEnd],
                symbols[(int)SymbolIds.StringEEPtrType]);

            var args = GetCommandLine(argc, argv, symbols[(int)SymbolIds.StringEEPtrType]);
            return RawCalliHelper.Call<int>(symbols[(int)SymbolIds.MainMethod], args);
        }
    }
}
