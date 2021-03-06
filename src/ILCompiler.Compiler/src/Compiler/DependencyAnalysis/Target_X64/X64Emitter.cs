// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace ILCompiler.DependencyAnalysis.X64
{
    public struct X64Emitter
    {
        public X64Emitter(NodeFactory factory)
        {
            Builder = new ObjectDataBuilder(factory);
            TargetRegister = new TargetRegisterMap(factory.Target.OperatingSystem);
        }

        public ObjectDataBuilder Builder;
        public TargetRegisterMap TargetRegister;

        // Assembly stub creation api. TBD, actually make this general purpose
        public void EmitMOV(Register regDst, ref AddrMode memory)
        {
            EmitIndirInstructionSize(0x8a, regDst, ref memory);
        }

        public void EmitMOV(Register regDst, Register regSrc)
        {
            AddrMode rexAddrMode = new AddrMode(regSrc, null, 0, 0, AddrModeSize.Int64);
            EmitRexPrefix(regDst, ref rexAddrMode);
            Builder.EmitByte(0x89);
            Builder.EmitByte((byte)(0xC0 | (((int)regSrc & 0x07) << 3) | (((int)regDst & 0x07))));
        }

        public void EmitLEAQ(Register reg, ISymbolNode symbol)
        {
            AddrMode rexAddrMode = new AddrMode(Register.RAX, null, 0, 0, AddrModeSize.Int64);
            EmitRexPrefix(reg, ref rexAddrMode);
            Builder.EmitByte(0x8D);
            Builder.EmitByte((byte)(0x05 | (((int)reg) & 0x07) << 3));
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32);
        }

        public void EmitCMP(ref AddrMode addrMode, sbyte immediate)
        {
            if (addrMode.Size == AddrModeSize.Int16)
                Builder.EmitByte(0x66);
            EmitIndirInstruction((byte)((addrMode.Size != AddrModeSize.Int8) ? 0x83 : 0x80), 0x7, ref addrMode);
            Builder.EmitByte((byte)immediate);
        }

        public void EmitADD(ref AddrMode addrMode, sbyte immediate)
        {
            if (addrMode.Size == AddrModeSize.Int16)
                Builder.EmitByte(0x66);
            EmitIndirInstruction((byte)((addrMode.Size != AddrModeSize.Int8) ? 0x83 : 0x80), (byte)0, ref addrMode);
            Builder.EmitByte((byte)immediate);
        }

        public void EmitJMP(ISymbolNode symbol)
        {
            Builder.EmitByte(0xE9);
            Builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_REL32);
        }

        public void EmitINT3()
        {
            Builder.EmitByte(0xCC);
        }

        public void EmitJmpToAddrMode(ref AddrMode addrMode)
        {
            EmitIndirInstruction(0xFF, 0x4, ref addrMode);
        }

        public void EmitRET()
        {
            Builder.EmitByte(0xC3);
        }

        public void EmitRETIfEqual()
        {
            // jne @+1
            Builder.EmitByte(0x75);
            Builder.EmitByte(0x01);

            // ret
            Builder.EmitByte(0xC3);
        }

        private bool InSignedByteRange(int i)
        {
            return i == (int)(sbyte)i;
        }

        private void EmitImmediate(int immediate, int size)
        {
            switch (size)
            {
                case 0:
                    break;
                case 1:
                    Builder.EmitByte((byte)immediate);
                    break;
                case 2:
                    Builder.EmitShort((short)immediate);
                    break;
                case 4:
                    Builder.EmitInt(immediate);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void EmitModRM(byte subOpcode, ref AddrMode addrMode)
        {
            byte modRM = (byte)((subOpcode & 0x07) << 3);
            if (addrMode.BaseReg > Register.None)
            {
                Debug.Assert(addrMode.BaseReg >= Register.RegDirect);

                Register reg = (Register)(addrMode.BaseReg - Register.RegDirect);
                Builder.EmitByte((byte)(0xC0 | modRM | ((int)reg & 0x07)));
            }
            else
            {
                byte lowOrderBitsOfBaseReg = (byte)((int)addrMode.BaseReg & 0x07);
                modRM |= lowOrderBitsOfBaseReg;
                int offsetSize = 0;

                if (addrMode.Offset == 0 && (lowOrderBitsOfBaseReg != (byte)Register.RBP))
                {
                    offsetSize = 0;
                }
                else if (InSignedByteRange(addrMode.Offset))
                {
                    offsetSize = 1;
                    modRM |= 0x40;
                }
                else
                {
                    offsetSize = 4;
                    modRM |= 0x80;
                }

                bool emitSibByte = false;
                Register sibByteBaseRegister = addrMode.BaseReg;

                if (addrMode.BaseReg == Register.None)
                {
                    //# ifdef _TARGET_AMD64_          
                    // x64 requires SIB to avoid RIP relative address
                    emitSibByte = true;
                    //#else
                    //                    emitSibByte = (addrMode.m_indexReg != MDIL_REG_NO_INDEX);
                    //#endif

                    modRM &= 0x38;    // set Mod bits to 00 and clear out base reg
                    offsetSize = 4;   // this forces 32-bit displacement

                    if (emitSibByte)
                    {
                        // EBP in SIB byte means no base
                        // ModRM base register forced to ESP in SIB code below
                        sibByteBaseRegister = Register.RBP;
                    }
                    else
                    {
                        // EBP in ModRM means no base
                        modRM |= (byte)(Register.RBP);
                    }
                }
                else if (lowOrderBitsOfBaseReg == (byte)Register.RSP || addrMode.IndexReg.HasValue)
                {
                    emitSibByte = true;
                }

                if (!emitSibByte)
                {
                    Builder.EmitByte(modRM);
                }
                else
                {
                    // MDIL_REG_ESP as the base is the marker that there is a SIB byte
                    modRM = (byte)((modRM & 0xF8) | (int)Register.RSP);
                    Builder.EmitByte(modRM);

                    int indexRegAsInt = (int)(addrMode.IndexReg.HasValue ? addrMode.IndexReg.Value : Register.RSP);

                    Builder.EmitByte((byte)((addrMode.Scale << 6) + ((indexRegAsInt & 0x07) << 3) + ((int)sibByteBaseRegister & 0x07)));
                }
                EmitImmediate(addrMode.Offset, offsetSize);
            }
        }

        private void EmitExtendedOpcode(int opcode)
        {
            if ((opcode >> 16) != 0)
            {
                if ((opcode >> 24) != 0)
                {
                    Builder.EmitByte((byte)(opcode >> 24));
                }
                Builder.EmitByte((byte)(opcode >> 16));
            }
            Builder.EmitByte((byte)(opcode >> 8));
        }

        private void EmitRexPrefix(Register reg, ref AddrMode addrMode)
        {
            byte rexPrefix = 0;

            // Check the situations where a REX prefix is needed

            // Are we accessing a byte register that wasn't byte accessible in x86?
            if (addrMode.Size == AddrModeSize.Int8 && reg >= Register.RSP)
            {
                rexPrefix |= 0x40;
            }

            // Is this a 64 bit instruction?
            if (addrMode.Size == AddrModeSize.Int64)
            {
                rexPrefix |= 0x48;
            }

            // Is the destination register one of the new ones?
            if (reg >= Register.R8)
            {
                rexPrefix |= 0x44;
            }

            // Is the index register one of the new ones?
            if (addrMode.IndexReg.HasValue && addrMode.IndexReg.Value >= Register.R8 && addrMode.IndexReg.Value <= Register.R15)
            {
                rexPrefix |= 0x42;
            }

            // Is the base register one of the new ones?
            if (addrMode.BaseReg >= Register.R8 && addrMode.BaseReg <= Register.R15
               || addrMode.BaseReg >= (int)Register.R8 + Register.RegDirect && addrMode.BaseReg <= (int)Register.R15 + Register.RegDirect)
            {
                rexPrefix |= 0x41;
            }

            // If we have anything so far, emit it.
            if (rexPrefix != 0)
            {
                Builder.EmitByte(rexPrefix);
            }
        }

        private void EmitIndirInstruction(int opcode, byte subOpcode, ref AddrMode addrMode)
        {
            EmitRexPrefix(Register.RAX, ref addrMode);
            if ((opcode >> 8) != 0)
            {
                EmitExtendedOpcode(opcode);
            }
            Builder.EmitByte((byte)opcode);
            EmitModRM(subOpcode, ref addrMode);
        }

        private void EmitIndirInstruction(int opcode, Register dstReg, ref AddrMode addrMode)
        {
            EmitRexPrefix(dstReg, ref addrMode);
            if ((opcode >> 8) != 0)
            {
                EmitExtendedOpcode(opcode);
            }
            Builder.EmitByte((byte)opcode);
            EmitModRM((byte)((int)dstReg & 0x07), ref addrMode);
        }

        private void EmitIndirInstructionSize(int opcode, Register dstReg, ref AddrMode addrMode)
        {
            //# ifndef _TARGET_AMD64_
            // assert that ESP, EBP, ESI, EDI are not accessed as bytes in 32-bit mode
            //            Debug.Assert(!(addrMode.Size == AddrModeSize.Int8 && dstReg > Register.RBX));
            //#endif
            Debug.Assert(addrMode.Size != 0);
            if (addrMode.Size == AddrModeSize.Int16)
                Builder.EmitByte(0x66);
            EmitIndirInstruction(opcode + ((addrMode.Size != AddrModeSize.Int8) ? 1 : 0), dstReg, ref addrMode);
        }
    }
}
