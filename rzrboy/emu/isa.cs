using System.Collections;

namespace emu
{
    public static class isa
    {
        public static readonly IBuilder[] Instr = new IBuilder[256];
        private static readonly IBuilder[] ExtInstr = new IBuilder[256];

        private class ExtInstruction : IInstruction // this is just a proxy to extension ISA
        {
            private IInstruction cur = null;

            public bool Eval(Reg reg, Mem mem)
            {
                if (cur == null)
                {
                    byte opcode = mem.rom[reg.PC++]; // fetch
                    cur = ExtInstr[opcode].Build();
                    return true;
                }
                else
                {
                    return cur.Eval(reg, mem);
                }
            }
        }

        private class ExtBuilder : IBuilder
        {
            public IInstruction Build() { return new ExtInstruction(); }
        }

        public readonly static op nop = (Reg reg, Mem mem) => true;

        // read next byte from mem[pc++]
        public static op ld_rom(Reg8 target) => (Reg reg, Mem mem) => { reg[target] = mem.rom[reg.PC++]; return true; };
        public static op[] ld_rom(Reg8 t1, Reg8 t2) => new op[] { ld_rom(t1), ld_rom(t2) };
        public static op[] ld_rom(Reg16 target) => new op[] {
            (Reg reg, Mem mem) => { reg[target] = mem.rom[reg.PC++]; return true; },
            (Reg reg, Mem mem) => { reg[target] |= (ushort)(mem.rom[reg.PC++] << 8); return true; }
        };

        // reg to reg
        public static op ld_reg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; return true; };
        public static op ld_reg_addr(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; return true; };
        public static op ld_reg_addr(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; return true; };

        static isa() 
        {
            Instr[0xCB] = new ExtBuilder();

            Instr[0x00] = new Builder(nop);
            Instr[0x01] = new Builder(ld_rom(Reg8.B, Reg8.C));
            Instr[0x02] = new Builder(ld_reg_addr(Reg16.BC, Reg8.A));
        }
    }
}
