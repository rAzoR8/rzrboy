using System.Diagnostics;

namespace rzr
{
    public class Cpu
    {
        public Reg reg { get => m_reg; }

        private Reg m_reg = new();
        private Mem m_mem;
        private Isa m_isa;
        private Int m_int = new();

        public byte curOpCode { get; private set; } = 0; // opcode od the currenlty executed instruction
        public ushort curInstrPC { get; private set; } = 0; // start ProgramCounter of the currently executed instruction
        public ushort prevInstrPC { get; private set; } = 0; // start ProgramCounter of the previously executed instruction

        public byte prevInstrCycles { get; private set; } = 1; // number of non-fetch cycles spend on the previous instructions
        public byte curInstrCycle { get; private set; } = 1; // number of Non-fetch cycles already spent on executing the current instruction

        IEnumerator<Op>? curOp = null;

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

				if( reg.IME == IMEState.Enabled ) // handle interrupts, if any
				{
					byte interrupts = (byte)( m_mem[0xFF0F] & m_mem[0xFFFF] );
					if( interrupts != 0 )
					{
						curOp = m_int.HandleInterrupts().GetEnumerator();
						curOp.MoveNext();
						return false;
					}
				}
				else if( reg.IME == IMEState.RequestEnabled )
					reg.IME = IMEState.Enabled;
				else if( reg.IME == IMEState.RequestDisabled )
					reg.IME = IMEState.Disabled;

				curOpCode = m_mem[curInstrPC]; // fetch

                Instruction builder = m_isa[curOpCode];
                if( builder == null )                
                {
                    return false;
                }

                ++m_reg.PC; // advance only for implemented instructions

                curOp = builder.Make().GetEnumerator();
                curOp.MoveNext();
            }

            return sameInstr;
        }
    }
}
