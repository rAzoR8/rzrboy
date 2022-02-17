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

	public enum OperandType : byte
	{
		d8, // data / unsigned
		r8, // relative addr / signed
		d16, // unsigned / addr
		io8, // 0xFF00 + d8

		B, C,
		D, E,
		H, L,
		AdrHL, // (HL)
		A, F,

		AdrHLI, // (HL+)
		AdrHLD, // (HL-)
		AdrBC, // (BC)
		AdrDE, // (DE)

		AF,
		BC,
		DE,
		HL,
		PC,
		SP,
		SPr8, // SP + r8

		RstAddr,

		condZ,
		condNZ,
		condC,
		condNC
	}

	public static class OperandExtensions 
	{
		public static byte Reg8Offset( this OperandType type, byte offset ) { return (byte)(offset + ( byte )type - (byte)OperandType.B); }
		// is B C D E H L (HL) A
		public static bool IsReg8HlA( this OperandType type ) { return type >= OperandType.B && type < OperandType.F; }
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
				case OperandType.AdrBC: return "(BC)";
				case OperandType.AdrDE: return "(DE)";
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

		public OperandType Lhs => this[0].Type;
		public OperandType Rhs => this[1].Type;


		/// <summary>
		/// Assemble to machine code
		/// </summary>
		/// <param name="pc"></param>
		/// <param name="mem"></param>
		/// <returns>Opcode</returns>
		public void Assemble( ref ushort _pc, ISection mem )
		{
			ushort pc = _pc;

			void Set( byte val ) { mem[pc++] = val; }
			void Set16( ushort val ) { mem[pc++] = val.GetLsb(); mem[pc++] = val.GetMsb(); }

			void Op1Db8() { if( Count > 0 ) mem[pc++] = this[0].d8; }
			void Op2Db8() { if( Count > 1 ) mem[pc++] = this[1].d8; }

			switch( Type )
			{
				case InstrType.Db:		Op1Db8(); break;
				case InstrType.Nop:		Set(0x00); break;
				case InstrType.Stop:	Set(0x10); Op1Db8(); break;
				case InstrType.Halt:	Set(0x76); break;
				case InstrType.Di:		Set(0xF3); break;
				case InstrType.Ei:		Set(0xFB); break;
				case InstrType.Ld:

					switch( Lhs )
					{
						case OperandType.B when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x40 ) ); break;
						case OperandType.C when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x48 ) ); break;
						case OperandType.D when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x50 ) ); break;
						case OperandType.E when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x58 ) ); break;
						case OperandType.H when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x60 ) ); break;
						case OperandType.L when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x68 ) ); break;

						case OperandType.AdrHL when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x70 ) ); break;
						case OperandType.A when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x78 ) ); break;
						default:
							break;
					}

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
				case InstrType.Cpl: Set( 0x2F ); break;
				case InstrType.Ccf: Set( 0X3F ); break;
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
			_pc = pc;
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
