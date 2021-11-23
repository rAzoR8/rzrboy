﻿using System.Diagnostics;

namespace emu
{
    public class Cpu : IProcessingUnit
    {
        private Reg reg = new();
        private Mem mem = null;

        byte cur_opcode;
        private IInstruction cur_instr = null;

        public Cpu(Mem memory) 
        {
            mem = memory;
            mem.rom0.write(boot.DMG, 0x0);            

            cur_opcode = mem.rom[reg.PC++]; // first cycle
            cur_instr = isa.Instr[cur_opcode].Build();
        }

        public void Tick()
        {
            if (cur_instr.Eval(reg, mem) == false) // fetch and exec are interleaved
            {
                cur_opcode = mem.rom[reg.PC++]; // fetch
                cur_instr = isa.Instr[cur_opcode].Build();
            }
        }
    }
}
