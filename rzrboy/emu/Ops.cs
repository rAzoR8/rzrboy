namespace emu
{
    public partial class Isa
    {
        private readonly static Builder nop = new Builder((Reg reg, Mem mem) => true);

        public static class Ops
        {
            // read next byte from mem[pc++]
            public static op ldrom(Reg8 target) => (Reg reg, Mem mem) => { reg[target] = mem[reg.PC++]; return true; };
            public static op[] ldrom(Reg8 t1, Reg8 t2) => new op[] { ldrom(t1), ldrom(t2) };
            public static op[] ldrom(Reg16 target) => new op[] {
                (Reg reg, Mem mem) => { reg[target] = mem[reg.PC++]; return true; },
                (Reg reg, Mem mem) => { reg[target] |= (ushort)(mem[reg.PC++] << 8); return true; }
            };

            public static dis mnemonic(string str) => (pc, mem) => str;
            public static dis operand(Reg8 reg) => (pc, mem) => reg.ToString();
            public static dis operand(Reg16 reg) => (pc, mem) => reg.ToString();

            public static dis[] operand(Reg8 dst, Reg8 src) => new dis[] { operand(dst), operand(src) };
            public static dis[] operand(Reg16 dst, Reg16 src) => new dis[] { operand(dst), operand(src) };

            // reg to reg
            public static op ldreg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; return true; };
            public static op ldreg(Reg16 dst, Reg16 src) => (reg, mem) => { reg[dst] = reg[src]; return true; };

            // reg to address
            public static op ldadr(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; return true; };
            // address to reg
            public static op ldadr(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; return true; };
        };

        private static Builder ldrom(Reg8 target) => Ops.ldrom(target).Get("LD") + Ops.operand(target) + ".DB";

        private static Builder ldreg(Reg8 dst, Reg8 src) => Ops.ldreg(dst, src).Get("LD") + Ops.operand(dst, src);
        private static Builder ldreg(Reg16 dst, Reg16 src) => Ops.ldreg(dst, src).Get("LD") + Ops.operand(dst, src);

        private static Builder ldadr(Reg8 dst, Reg16 src_addr) => Ops.ldadr(dst, src_addr).Get("LD") + Ops.operand(dst) + $"({src_addr})";
        private static Builder ldadr(Reg16 dst_addr, Reg8 src) => Ops.ldadr(dst_addr, src).Get("LD") + $"({dst_addr})" + Ops.operand(src);
    }
}
