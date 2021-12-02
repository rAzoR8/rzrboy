using System.Diagnostics;

namespace emu
{
    public class Cpu
    {
        public static Isa isa { get; } = new();
        public Reg reg { get; } = new();
        private Mem mem { get; }

        private byte cur_opcode = 0;
        private IInstruction? cur_instr = null;

        public Cpu( Mem memory ) 
        {
            mem = memory;
        }

        /// <summary>
        /// execute one M-cycle
        /// </summary>
        /// <returns>true if executing the same instruction, false after a new one is fetched</returns>
        public bool Tick()
        {
            if ( cur_instr == null || cur_instr.Eval( reg, mem ) == false ) // fetch and exec are interleaved
            {
                cur_opcode = mem[reg.PC]; // fetch
                IBuilder builder = isa[cur_opcode];
               
                ushort dis_pc = reg.PC++;

                Debug.WriteLine( isa.Disassemble( ref dis_pc, mem ) );

                bool firstTick = cur_instr == null;

                // TODO: remove once all instructions are implemented
                if ( builder != null )
                {
                    cur_instr = builder.Build();
                }

                return firstTick;
            }
            return true;
        }
    }
}
