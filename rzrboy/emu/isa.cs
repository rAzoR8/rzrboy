namespace emu
{
    public static class isa
    {
        public static readonly instr[] Instr = new instr[256];
        public static readonly instr[] ExtInstr = new instr[256];

        public static readonly instrdesc[] Desc = new instrdesc[256];
        public static readonly instrdesc[] ExtDesc = new instrdesc[256];

        public readonly static op nop = (reg reg, mem mem) => 0;

        // read next byte from mem[pc++]
        //
        public static op ld_imm(byte? target, int next = 0) => (reg reg, mem mem) => { target = mem.rom[reg.PC++]; return next; };
        public static op ld_imm(Reg8 target, int next = 0) => (reg reg, mem mem) => { reg[target] = mem.rom[reg.PC++]; return next; };
        public static op[] ld_imm(Reg8 t1, Reg8 t2, int next = 0) => new op[] { ld_imm(t1, next + 1), ld_imm(t2, next) };

        // reg to reg
        public static op ld_reg(Reg8 dst, Reg8 src) => (reg, mem) => { reg[dst] = reg[src]; return 0; };
        public static op ld_reg_addr(Reg8 dst, Reg16 src_addr) => (reg, mem) => { reg[dst] = mem[reg[src_addr]]; return 0; };
        public static op ld_reg_addr(Reg16 dst_addr, Reg8 src) => (reg, mem) => { mem[reg[dst_addr]] = reg[src]; return 0; };

        static isa() 
        {
            Instr[0x00] = new();
            Instr[0x01] = new(ld_imm(Reg8.B, Reg8.C));
            Instr[0x02] = new(ld_reg_addr(Reg16.BC, Reg8.A));
        }
    }
}
