namespace emu
{
    public static class isa
    {
        public static readonly instr[] Instr = new instr[0xff];
        public static readonly instr[] ExtInstr = new instr[0xff];

        public static readonly instrdesc[] Desc = new instrdesc[0xff];
        public static readonly instrdesc[] ExtDesc = new instrdesc[0xff];

        static isa() 
        {
            Instr[0x00] = new();
            Instr[0x01] = new(instr.ld_imm(Reg8.B, Reg8.C));
            Instr[0x02] = new(instr.ld_reg_addr(Reg16.BC, Reg8.A));
        }
    }
}
