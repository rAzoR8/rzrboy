using System.Diagnostics;

namespace emu
{
    public class Cpu
    {
        public static Isa isa { get; } = new();
        public Reg reg { get; } = new();
        private Mem mem { get; }

        public byte curOpCode { get; private set; } = 0; // opcode od the currenlty executed instruction
        public ushort curInstrPC { get; private set; } = 0; // start ProgramCounter of the currently executed instruction

        public byte prevInstrCycles { get; private set; } = 0; // number of non-fetch cycles spend on the previous instructions
        public byte curInstrCycle { get; private set; } = 0; // number of Non-fetch cycles already spent on executing the current instruction

        private IInstruction? curInstr = null;

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
            if ( curInstr == null || curInstr.Eval( reg, mem ) == false ) // fetch and exec are interleaved
            {
                prevInstrCycles = curInstrCycle;
                curInstrCycle = 0;

                curInstrPC = reg.PC;
                curOpCode = mem[reg.PC++]; // fetch

                IBuilder builder = isa[curOpCode];
               
                bool firstTick = curInstr == null;

                // TODO: remove once all instructions are implemented
                if ( builder != null )
                {
                    curInstr = builder.Build();
                }

                return firstTick;
            }

            ++curInstrCycle;
            return true;
        }
    }
}
