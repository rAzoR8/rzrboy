﻿namespace emu
{
    public class cpu : IProcessingUnit
    {
        private reg reg = reg.DMG();
        private mem mem = null;

        byte cur_opcode;
        private instr cur_instr = null;
        private int next_op = 0;

        public cpu(mem memory) 
        {
            mem = memory;
            mem.Add(new RSection(0x0000, 0x3FFF)); // rom bank 0



            op imm = instr.imm(1, reg.A);

        }

        public void Tick()
        {
            if(next_op != 0) // execute next uOP
            {
                next_op = cur_instr[next_op](reg, mem);
            }
            else
            {
                cur_opcode = mem[reg.PC++]; // first cycle of this sinstr
                if(cur_opcode != 0xCB)
                {
                    cur_instr = isa.Instr[cur_opcode];
                }
                else
                {
                    cur_instr = isa.ExtInstr[cur_opcode];
                }
            }
        }
    }
}
