using System.Diagnostics;

namespace rzr
{
    public class Cpu
    {
        public Reg reg { get => m_reg; }

        private Reg m_reg = new();
        private Mem m_mem;
        private Isa m_isa;

        public byte curOpCode { get; private set; } = 0; // opcode od the currenlty executed instruction
        public ushort curInstrPC { get; private set; } = 0; // start ProgramCounter of the currently executed instruction
        public ushort prevInstrPC { get; private set; } = 0; // start ProgramCounter of the previously executed instruction

        public byte prevInstrCycles { get; private set; } = 1; // number of non-fetch cycles spend on the previous instructions
        public byte curInstrCycle { get; private set; } = 1; // number of Non-fetch cycles already spent on executing the current instruction

        IEnumerator<op>? curOp = null;

        public Cpu( Mem memory, Isa isa ) 
        {
            m_mem = memory;
            m_isa = isa;
        }

        /// <summary>
        /// execute one M-cycle
        /// </summary>
        /// <returns>true if executing the same instruction, false after a new one is fetched</returns>
        public bool Tick()
        {
            bool sameInstr = true;

            if( curOp != null )
            {
                curOp.Current( m_reg, m_mem );
                ++curInstrCycle;
            }

            if( curOp == null || curOp.MoveNext() == false )
            {
                sameInstr = curOp == null ;

                prevInstrCycles = curInstrCycle;
                curInstrCycle = 1;

                prevInstrPC = curInstrPC;
                curInstrPC = m_reg.PC;

                curOpCode = m_mem[curInstrPC]; // fetch

                Builder builder = m_isa[curOpCode];
                if( builder == null )                
                {
                    return false;
                }

                ++m_reg.PC; // advance only for implemented instructions

                curOp = builder.Instr().GetEnumerator();
                curOp.MoveNext();
            }

            return sameInstr;
        }
    }
}
