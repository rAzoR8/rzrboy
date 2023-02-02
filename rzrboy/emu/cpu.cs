namespace rzr
{
    public class Cpu
    {
        private IEnumerable<Op>[] m_instr = new IEnumerable<Op>[256];

        public ulong tick { get; private set; } // current cycle/tick
        public byte curOpCode { get; private set; } = 0; // opcode od the currenlty executed instruction
        public ushort curInstrPC { get; private set; } = 0; // start ProgramCounter of the currently executed instruction
        public ushort prevInstrPC { get; private set; } = 0; // start ProgramCounter of the previously executed instruction

        public byte prevInstrCycles { get; private set; } = 1; // number of non-fetch cycles spend on the previous instructions
        public byte curInstrCycle { get; private set; } = 1; // number of Non-fetch cycles already spent on executing the current instruction

        private IEnumerator<Op>? m_curOp = null;

		public Cpu( ) 
        {
            // cache instructions
			Isa isa = new();
			foreach( (ExecInstr instr, int i) in isa.Indexed() )
			{
                m_instr[i] = instr.Make();
            }
        }

        /// <summary>
        /// execute one M-cycle
        /// </summary>
        /// <returns>true if executing the same instruction, false after a new one is fetched</returns>
        public bool Tick( Reg reg, Mem mem )
        {
			bool sameInstr = true;

            if( m_curOp != null )
            {
                m_curOp.Current( reg, mem );
                ++curInstrCycle;
            }

            if( m_curOp == null || m_curOp.MoveNext() == false )
            {
                sameInstr = m_curOp == null ;

                prevInstrCycles = curInstrCycle;
                curInstrCycle = 1;

                prevInstrPC = curInstrPC;
                curInstrPC = reg.PC;

                // handle interrupts, if any
                if( reg.IME == IMEState.Enabled ) 
				{
					byte interrupts = (byte)( mem[0xFF0F] & mem[0xFFFF] );
					if( interrupts != 0 )
					{
                        reg.Halted = false;

						m_curOp = Interrupt.HandlePending().GetEnumerator();
						m_curOp.MoveNext();
						return false;
					}
				}
				else if( reg.IME == IMEState.RequestEnabled )
					reg.IME = IMEState.Enabled;

                // handle HALT & STOP
                if( reg.Halted )
                {
                    return false;
                }

				curOpCode = mem[curInstrPC]; // fetch
                ++reg.PC;

                m_curOp = m_instr[curOpCode].GetEnumerator();
                m_curOp.MoveNext();
            }

            tick++;
            return sameInstr;
        }
    }
}
