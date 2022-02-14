namespace rzr
{
	public enum InstrType
	{
		Db, //just data, not and actual instruction

		Nop,
		Stop,
		Halt,

		Di,
		Ei,

		Ld,
		Ldh,

		Inc,
		Dec,

		Add,
		Adc,
		Sub,
		Sbc,

		And,
		Or,
		Xor,
		Cp,

		Jp,
		Jr,
		Ret,
		Reti,
		Call,

		Rst,

		Push,
		Pop,

		Rla,
		Rlca,
		Rra,
		Rrca,

		Daa,
		Scf,

		Cpl,
		Ccf,

		// Ext / Prefix
		Rlc,
		Rrc,
		Rl,
		Rr,
		Sla,
		Sra,
		Swap,
		Srl,
		Bit,
		Res,
		Set
	}

	public enum OperandType
	{
		d8, // data / unsigned
		r8, // relative addr / signed
		d16, // unsigned / addr
		io8, // 0xFF00 + d8

		A, F,
		B, C,
		D, E,
		H, L,

		AF,
		BC,
		DE,
		HL,
		PC,
		SP,
		SPr8, // SP + r8

		AdrHL, // (HL)
		AdrHLI, // (HL+)
		AdrHLD, // (HL-)

		RstAddr,

		condZ,
		condNZ,
		condC,
		condNC
	}

	public class Operand
	{
		public Operand( OperandType type ) { Type = type; }
		public Operand( OperandType type, byte io8 ) { Type = type; d16 = io8; }
		public Operand( byte d8 ) { Type = OperandType.d8; d16 = d8; }
		public Operand( sbyte r8 ) { Type = OperandType.r8; d16 = (byte)r8; }
		public Operand( ushort d16 ) { Type = OperandType.d16; this.d16 = d16; }

		public OperandType Type { get; }
		public ushort d16 { get; } = 0;
		public sbyte r8 => (sbyte)d16.GetLsb();
		public byte d8 => d16.GetLsb();

		public override string ToString()
		{
			switch( Type )
			{
				case OperandType.RstAddr:
				case OperandType.d8: return $"{d8:X2}";
				case OperandType.r8: return $"{r8:X2}";
				case OperandType.d16: return $"{d16:X4}";
				case OperandType.io8: return $"0xFF00+{d8:X2}";
				case OperandType.A:
				case OperandType.F:
				case OperandType.B:
				case OperandType.C:
				case OperandType.D:
				case OperandType.E:
				case OperandType.H:
				case OperandType.L:
				case OperandType.AF:
				case OperandType.BC:
				case OperandType.DE:
				case OperandType.HL:
				case OperandType.PC:
				case OperandType.SP:
					return Type.ToString();
				case OperandType.SPr8: return $"SP+{r8:X2}";
				case OperandType.AdrHL: return "(HL)";
				case OperandType.AdrHLI: return "(HL+)";
				case OperandType.AdrHLD: return "(HL-)";
				case OperandType.condZ: return "Z";
				case OperandType.condNZ: return "NZ";
				case OperandType.condC: return "C";
				case OperandType.condNC: return "NC";
				default: return "?";
			}
		}
	}

	public class AsmInstr : List<Operand>
	{
		public AsmInstr( InstrType type ) { Type = type; }
		public AsmInstr( InstrType type, params Operand[] operands ) : base( operands ) { Type = type; }
		public AsmInstr( InstrType type, OperandType lhs ) { Type = type; Add( new( lhs ) ); }
		public AsmInstr( InstrType type, OperandType lhs, OperandType rhs ) { Type = type; Add( new( lhs ) ); Add( new( rhs ) ); }


		public InstrType Type { get; }

		/// <summary>
		/// Assemble to machine code
		/// </summary>
		/// <param name="pc"></param>
		/// <param name="mem"></param>
		/// <returns>Opcode</returns>
		public void Assemble( ref ushort pc, ISection mem )
		{
			switch( Type )
			{
				case InstrType.Db:		mem[pc++] = this[0].d8; break;
				case InstrType.Nop:		mem[pc++] = 0x00; break;
				case InstrType.Stop:	mem[pc++] = 0x10; break; // TODO: 2nd operand
				case InstrType.Halt:	mem[pc++] = 0x76; break;
				case InstrType.Di:		mem[pc++] = 0xF3; break;
				case InstrType.Ei:		mem[pc++] = 0xFB; break;
				case InstrType.Ld:
					break;
				case InstrType.Ldh:
					break;
				case InstrType.Inc:
					break;
				case InstrType.Dec:
					break;
				case InstrType.Add:
					break;
				case InstrType.Adc:
					break;
				case InstrType.Sub:
					break;
				case InstrType.Sbc:
					break;
				case InstrType.And:
					break;
				case InstrType.Or:
					break;
				case InstrType.Xor:
					break;
				case InstrType.Cp:
					break;
				case InstrType.Jp:
					break;
				case InstrType.Jr:
					break;
				case InstrType.Ret:
					break;
				case InstrType.Reti:
					break;
				case InstrType.Call:
					break;
				case InstrType.Rst:
					break;
				case InstrType.Push:
					break;
				case InstrType.Pop:
					break;
				case InstrType.Rla:
					break;
				case InstrType.Rlca:
					break;
				case InstrType.Rra:
					break;
				case InstrType.Rrca:
					break;
				case InstrType.Daa:
					break;
				case InstrType.Scf:
					break;
				case InstrType.Cpl:
					break;
				case InstrType.Ccf:
					break;
				case InstrType.Rlc:
					break;
				case InstrType.Rrc:
					break;
				case InstrType.Rl:
					break;
				case InstrType.Rr:
					break;
				case InstrType.Sla:
					break;
				case InstrType.Sra:
					break;
				case InstrType.Swap:
					break;
				case InstrType.Srl:
					break;
				case InstrType.Bit:
					break;
				case InstrType.Res:
					break;
				case InstrType.Set:
					break;
				default:
					break;
			}
		}

		public override string ToString()
		{
			switch( Count )
			{
				case 2: return $"{Type} {this[0]}, {this[1]}";
				case 1: return $"{Type} {this[0]}";
				case 0: default: return Type.ToString();
			}
		}
	}
}
