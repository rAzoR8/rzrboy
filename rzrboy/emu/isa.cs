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
            Instr[0x00] =  new nop();
        }
    }
}
