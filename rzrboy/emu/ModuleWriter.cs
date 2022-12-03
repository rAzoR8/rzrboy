namespace rzr
{
	public class AsmWriter
	{
		private List<AsmInstr> m_instructions = new List<AsmInstr>();
		private void Add( InstrType instr, params AsmOperand[] operands ) => m_instructions.Add( new( instr, operands ) );

		public void Nop() => Add( InstrType.Nop );
		public void Stop( params AsmOperand[] ops ) => Add( InstrType.Stop, ops );
		public void Halt() => Add( InstrType.Halt );
		public void Ld( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Ld, lhs, rhs );
		public void Jr( params AsmOperand[] ops ) => Add( InstrType.Jr, ops );
		public void Jp( params AsmOperand[] ops ) => Add( InstrType.Jp, ops );
		public void Inc( AsmOperand lhs ) => Add( InstrType.Inc, lhs );
		public void Dec( AsmOperand lhs ) => Add( InstrType.Dec, lhs );
		public void Add( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Add, lhs, rhs );
		public void Adc( AsmOperand rhs ) => Add( InstrType.Adc, rhs );
		public void Sub( AsmOperand rhs ) => Add( InstrType.Sub, rhs );
		public void Sbc( AsmOperand rhs ) => Add( InstrType.Sbc, rhs );
		public void And( AsmOperand rhs ) => Add( InstrType.And, rhs );
		public void Or( AsmOperand rhs ) => Add( InstrType.Or, rhs );
		public void Xor( AsmOperand rhs ) => Add( InstrType.Xor, rhs );
		public void Cp( AsmOperand rhs ) => Add( InstrType.Cp, rhs );
		public void Ret( params AsmOperand[] ops ) => Add( InstrType.Ret, ops );
		public void Reti() => Add( InstrType.Reti );
		public void Pop( AsmOperand lhs ) => Add( InstrType.Pop, lhs );
		public void Push( AsmOperand lhs ) => Add( InstrType.Push, lhs );
		public void Call( params AsmOperand[] ops ) => Add( InstrType.Call, ops );
		public void Di() => Add( InstrType.Di );
		public void Ei() => Add( InstrType.Ei );
		public void Rlca() => Add( InstrType.Rlca );
		public void Rla() => Add( InstrType.Rla );
		public void Daa() => Add( InstrType.Daa );
		public void Scf() => Add( InstrType.Scf );
		public void Rrca() => Add( InstrType.Rrca );
		public void Rra() => Add( InstrType.Rra );
		public void Cpl() => Add( InstrType.Cpl );
		public void Ccf() => Add( InstrType.Ccf );
		public void Rst( AsmOperand vec ) => Add( InstrType.Rst, vec );

		public ushort Write( ushort pc, ISection mem, bool throwException = true )
		{
			foreach( AsmInstr instr in m_instructions )
			{
				instr.Assemble( ref pc, mem, throwException: throwException );
			}

			return pc;
		}
	}

	public class ModuleWriter : AsmWriter
	{
		public AsmWriter Rst0 { get; } = new();
		public AsmWriter Rst8 { get; } = new();
		public AsmWriter Rst10 { get; } = new();
		public AsmWriter Rst18 { get; } = new();
		public AsmWriter Rst20 { get; } = new();
		public AsmWriter Rst28 { get; } = new();
		public AsmWriter Rst30 { get; } = new();
		public AsmWriter Rst38 { get; } = new();
		public AsmWriter Rst40 { get; } = new();
		public AsmWriter Rst48 { get; } = new();

		public AsmWriter VSync { get; } = new();

		public new ushort Write( ushort pc, ISection mem, bool throwException = true )
		{
			void write( AsmWriter writer, ushort offset, ushort bound )
			{
				ushort end = writer.Write( pc += offset, mem, throwException );
				if( end > bound )
				{
					pc = bound; // rest pc to acceptible bounds
					if( throwException )
						throw new rzr.AsmException( $"Invalid PC bound for Writer: {end:X4} expected {bound}" );
				}
			}

			write( Rst0, 0x0, 0x8 );
			write( Rst8, 0x08, 0x18 );
			write( Rst18, 0x18, 0x20 );
			write( Rst20, 0x20, 0x28 );
			write( Rst28, 0x28, 0x30 );
			write( Rst30, 0x30, 0x38 );
			write( Rst38, 0x38, 0x40 );
			write( Rst40, 0x40, 0x48 );
			//write( Rst48, 0x48, VSync );

			base.Write( pc+= 0x100, mem, throwException );

			// TODO: handle banksize overrun
			return pc;
		}
	}
}
