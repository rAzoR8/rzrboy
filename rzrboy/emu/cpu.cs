using System.Diagnostics;

namespace emu
{
    public class Cpu : IProcessingUnit
    {
        private Isa isa = new();
        private Reg reg = new();
        private Mem mem;
        private Cartridge cart;

        byte cur_opcode;
        private IInstruction cur_instr;

        public Cpu( Mem memory, byte[] cartData ) 
        {
            mem = memory;
            cart = new( mem.rom, mem.eram, mem.io, cartData );

            cur_opcode = mem.rom[reg.PC++]; // first cycle
            cur_instr = isa[cur_opcode].Build();
        }

        public void Tick()
        {
            if (cur_instr.Eval(reg, mem) == false) // fetch and exec are interleaved
            {
                cur_opcode = mem.rom[reg.PC++]; // fetch
                cur_instr = isa[cur_opcode].Build();
            }
        }
    }
}
