using System.Diagnostics;

namespace rzr
{
    public class Cpu
    {
        private Reg m_reg;
        private Mem m_mem;
        private IEnumerable<Op>[] m_instr = new IEnumerable<Op>[256];

        public byte curOpCode { get; private set; } = 0; // opcode od the currenlty executed instruction
        public ushort curInstrPC { get; private set; } = 0; // start ProgramCounter of the currently executed instruction
        public ushort prevInstrPC { get; private set; } = 0; // start ProgramCounter of the previously executed instruction

        public byte prevInstrCycles { get; private set; } = 1; // number of non-fetch cycles spend on the previous instructions
        public byte curInstrCycle { get; private set; } = 1; // number of Non-fetch cycles already spent on executing the current instruction

        private IEnumerator<Op>? curOp = null;

        public Cpu( Reg reg, Mem memory, Isa isa ) 
        {
            m_reg = reg;
            m_mem = memory;

            // cache instructions
			foreach( (ExecInstr instr, int i) in isa.Indexed() )
			{
                m_instr[i] = instr.Make();
            }
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

                // handle interrupts, if any
                if( m_reg.IME == IMEState.Enabled ) 
				{
					byte interrupts = (byte)( m_mem[0xFF0F] & m_mem[0xFFFF] );
					if( interrupts != 0 )
					{
                        m_reg.Halted = false;

						curOp = Interrupt.HandlePending().GetEnumerator();
						curOp.MoveNext();
						return false;
					}
				}
				else if( m_reg.IME == IMEState.RequestEnabled )
					m_reg.IME = IMEState.Enabled;

                // handle HALT & STOP
                if( m_reg.Halted )
                {
                    return false;
                }

				curOpCode = m_mem[curInstrPC]; // fetch
                ++m_reg.PC;

                curOp = m_instr[curOpCode].GetEnumerator();
                curOp.MoveNext();
            }

            return sameInstr;
        }
    }
}
