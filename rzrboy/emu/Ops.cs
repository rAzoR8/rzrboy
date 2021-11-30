﻿namespace emu
{
    public partial class Isa
    {
        private readonly static Builder nop = new Builder((Reg reg, Mem mem) => { }, "NOP");

        public static class Ops
        {
            public static dis mnemonic(string str) => (pc, mem) => str;
            public static dis operand(Reg8 reg) => (pc, mem) => reg.ToString();
            public static dis operand(Reg16 reg) => (pc, mem) => reg.ToString();

            public static dis operandDB8() => (pc, mem) => $"0x{mem[pc]:X2}";
            public static dis operandDB16() => (pc, mem) => $"0x{mem[pc]:X2}{mem[(ushort)(pc+1)]:X2}";
            public static dis addrDB16() => (pc, mem) => $"(0x{mem[pc]:X2}{mem[(ushort)(pc + 1)]:X2})";

            public static dis[] operand(Reg8 dst, Reg8 src) => new dis[] { operand(dst), operand(src) };
            public static dis[] operand(Reg16 dst, Reg16 src) => new dis[] { operand(dst), operand(src) };

            //private readonly static op pcinc = (Reg reg, Mem mem) => { reg.PC++; };

            // read next byte from mem[pc++], 2 m-cycles
            public static op ldimm(Reg8 target) => (Reg reg, Mem mem) => { reg[target] = mem[reg.PC++];};

            public static op ldimm(byte? val) => (Reg reg, Mem mem) => { val = mem[reg.PC++]; };

            //public static op[] ldimm(ushort? val) => new op[] {
            //    (Reg reg, Mem mem) => { val = mem[reg.PC++]; },
            //    (Reg reg, Mem mem) => { val |= (ushort)(mem[reg.PC++] << 8); }
            //};

            // read two bytes from instruction stream, 3 m-cycles
            public static op[] ldimm(Reg16 target) => new op[] {
                (Reg reg, Mem mem) => { reg[target] = mem[reg.PC++]; },
                (Reg reg, Mem mem) => { reg[target] |= (ushort)(mem[reg.PC++] << 8); }
            };

            // reg to reg, 1 m-cycle
            public static op ldreg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; };
            public static op ldreg(Reg16 dst, Reg16 src) => (reg, mem) => { reg[dst] = reg[src]; };

            // address to reg
            public static op ldadr(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; };
            // reg to address
            public static op ldadr(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; };
            // reg to address
            //public static op ldadr(ushort dst_addr, Reg8 src) => (reg, mem) => { mem[dst_addr] = reg[src]; };
            //public static op ldadr(ushort dst_addr, byte src) => (reg, mem) => { mem[dst_addr] = src; };

            public static op ldhlplus(Reg8 src) => (reg, mem) => { mem[reg.HL++] = reg[src]; };
            public static op ldhlminus(Reg8 src) => (reg, mem) => { mem[reg.HL--] = reg[src]; };

            public static IEnumerable<op> ldimm16_sp() 
            {
                byte nlow = 0, nhigh = 0;
                yield return ldimm(nlow);
                yield return ldimm(nhigh);
                ushort nn = binutil.Combine(nhigh, nlow);
                yield return (Reg reg, Mem mem) => { mem[nn] = binutil.lsb(reg.SP); };
                yield return (Reg reg, Mem mem) => { mem[++nn] = binutil.msb(reg.SP); };
            }

            private static op jp(ushort addr) => (reg, mem) => { reg.PC = addr; };
            // JP HL
            public static op jphl() => (reg, mem) => { reg.PC = reg.HL; };

            public delegate bool cond(Reg reg);
            public readonly static cond NZ = (Reg reg) => !reg.Zero;
            public readonly static cond Z = (Reg reg) => reg.Zero;
            public readonly static cond NC = (Reg reg) => !reg.Carry;
            public readonly static cond C = (Reg reg) => reg.Carry;

            // JP cc, a16
            public static IEnumerable<op> jpccimm16(cond cc)
            {
                byte nlow = 0, nhigh = 0;
                bool takeBranch = false;
                yield return ldimm(nlow);
                yield return (Reg reg, Mem mem) => { nhigh = mem[reg.PC++]; takeBranch = cc(reg); };
                if (takeBranch)
                {
                    ushort nn = binutil.Combine(nhigh, nlow);
                    yield return jp(nn);
                }
            }

            // JP a16
            public static IEnumerable<op> jpimm16()
            {
                byte nlow = 0, nhigh = 0;
                yield return ldimm(nlow);
                yield return ldimm(nhigh);
                ushort nn = binutil.Combine(nhigh, nlow);
                yield return jp(nn);
            }
        };

        private static Builder ldimm(Reg8 target) => Ops.ldimm(target).Get("LD") + Ops.operand(target) + Ops.operandDB8();
        private static Builder ldimm(Reg16 target) => Ops.ldimm(target).Get("LD") + Ops.operand(target) + Ops.operandDB16();

        private static Builder ldreg(Reg8 dst, Reg8 src) => Ops.ldreg(dst, src).Get("LD") + Ops.operand(dst, src);
        private static Builder ldreg(Reg16 dst, Reg16 src) => Ops.ldreg(dst, src).Get("LD") + Ops.operand(dst, src);

        private static Builder ldadr(Reg8 dst, Reg16 src_addr) => Ops.ldadr(dst, src_addr).Get("LD") + Ops.operand(dst) + $"({src_addr})";
        private static Builder ldadr(Reg16 dst_addr, Reg8 src) => Ops.ldadr(dst_addr, src).Get("LD") + $"({dst_addr})" + Ops.operand(src);

        // LD (a16), SP
        private static Builder ldimm16_sp() => new Builder(Ops.ldimm16_sp(), "LD") + Ops.addrDB16() + "SP";

        // LD a16
        private static Builder jpimm16() => new Builder(Ops.jpimm16(), "JP") + Ops.operandDB16();

        private static Builder jpimm16cc(Ops.cond cc, string flag) => new Builder(Ops.jpccimm16(cc), "JP") + flag + Ops.addrDB16();
    }
}