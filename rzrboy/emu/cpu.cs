namespace rzr
{
    public class Cpu : ICpuState
    {
        public static IEnumerable<CpuOp>[] Instructions = new IEnumerable<CpuOp>[256];

		static Cpu( ) 
        {
            // cache instructions
			Isa isa = new();
			foreach( (ExecInstr instr, int i) in isa.Indexed() )
			{
                Instructions[i] = instr.Make();
            }
        }

		private IEnumerator<CpuOp>? curOp = null;

		// ICpuState
		public ushort CurrentInstrPC { get; /*private*/ set; }
		public byte CurrentInstrCycle { get; set; }

		// internal for now
		public ulong CurrentCycle { get; private set; } = 0;
		public byte CurrentOpCode { get; private set; } = 0;

		/// <summary>
		/// execute one M-cycle
		/// </summary>
		/// <returns>true if executing the same instruction, false after a new one is fetched</returns>
		public bool Tick( IEmuState state )
        {
			bool moreOps = true;
            if( curOp != null )
            {
				curOp.Current( state.reg.AsView() , state.mem );
				moreOps = curOp.MoveNext();
				++CurrentInstrCycle;
            }

			// fetch
            if( curOp == null || !moreOps )
            {
				CurrentInstrCycle = 1;
				CurrentInstrPC = state.reg.PC;

                // handle interrupts, if any
                if( state.reg.IME == IMEState.Enabled ) 
				{
					byte interrupts = (byte)( state.mem[0xFF0F] & state.mem[0xFFFF] );
					if( interrupts != 0 )
					{
						state.reg.Halted = false;

						curOp = Interrupt.HandlePending().GetEnumerator();
						curOp.MoveNext();
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

				CurrentOpCode = state.mem[CurrentInstrPC]; // fetch
                ++state.reg.PC;

				curOp = Instructions[CurrentOpCode].GetEnumerator();
				curOp.MoveNext();
            }

			CurrentCycle++;
            return moreOps;
        }

		public void Load( byte[] data )
		{
			BinaryReader br = new( data );
			CurrentCycle = br.ReadUInt32();
			CurrentInstrCycle = br.ReadByte();
			CurrentInstrPC = br.ReadUInt16();
			CurrentOpCode = br.ReadByte();

			// catch up on the passed cycles on this instruction
			curOp = Instructions[CurrentOpCode].GetEnumerator();
			for( int i = 0; i < CurrentInstrCycle; ++i )
				curOp.MoveNext();
		}

		public byte[] Save()
		{
			BinaryWriter bw = new();
			bw.Write( CurrentCycle );
			bw.Write( CurrentInstrCycle );
			bw.Write( CurrentInstrPC );
			bw.Write( CurrentOpCode );
			return bw.ToArray();
		}
	}
}
