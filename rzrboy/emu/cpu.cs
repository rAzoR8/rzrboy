namespace rzr
{
    public class Cpu
    {
        private static IEnumerable<Op>[] m_instr = new IEnumerable<Op>[256];

		static Cpu( ) 
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
        public bool Tick( State state )
        {
			bool moreOps = true;
            if( state.curOp != null )
            {
				state.curOp.Current( state.reg, state.mem );
				moreOps = state.curOp.MoveNext();
				++state.curInstrCycle;
            }

			// fetch
            if( state.curOp == null || !moreOps )
            {
				state.prevInstrCycles = state.curInstrCycle;
				state.curInstrCycle = 1;

				state.prevInstrPC = state.curInstrPC;
				state.curInstrPC = state.reg.PC;

                // handle interrupts, if any
                if( state.reg.IME == IMEState.Enabled ) 
				{
					byte interrupts = (byte)( state.mem[0xFF0F] & state.mem[0xFFFF] );
					if( interrupts != 0 )
					{
						state.reg.Halted = false;

						state.curOp = Interrupt.HandlePending().GetEnumerator();
						state.curOp.MoveNext();
						return false; // TODO: check if this is still correct
					}
				}
				else if( state.reg.IME == IMEState.RequestEnabled )
					state.reg.IME = IMEState.Enabled;

                // handle HALT & STOP
                if( state.reg.Halted )
                {
                    return false;
                }

				state.curOpCode = state.mem[state.curInstrPC]; // fetch
                ++state.reg.PC;

				state.curOp = m_instr[state.curOpCode].GetEnumerator();
				state.curOp.MoveNext();
            }

			state.tick++;
            return moreOps;
        }
    }
}
