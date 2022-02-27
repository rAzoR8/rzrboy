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
		ioC, // 0xFF00 + C

		B, C,
		D, E,
		H, L,
		AdrHL, // (HL)
		A,

		AdrBC, // (BC)
		AdrDE, // (DE)
		AdrHLI, // (HL+)
		AdrHLD, // (HL-)

		BC,
		DE,
		HL,
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
		// X offset
		public static byte Reg8Offset( this OperandType type, byte offset ) { return (byte)(offset + ( byte )type - (byte)OperandType.B); }
		// is B C D E H L (HL) A
		public static bool IsReg8HlA( this OperandType type ) { return type >= OperandType.B && type <= OperandType.A; }
		// is BC DE HL SP
		public static bool IsReg16( this OperandType type ) { return type >= OperandType.BC && type <= OperandType.SP; }
		// is (BC) (DE) (HL+) (HL-)
		public static bool IsReg16Adr( this OperandType type ) { return type >= OperandType.AdrBC && type <= OperandType.AdrHLD; }
		// is B D H (HL)
		public static bool IsBCDHHl( this OperandType type ) {
			return
				type == OperandType.B ||
				type == OperandType.D ||
				type == OperandType.H ||
				type == OperandType.AdrHL; }
		// is C E L A
		public static bool IsCELA( this OperandType type )
		{
			return
				type == OperandType.C ||
				type == OperandType.E ||
				type == OperandType.L ||
				type == OperandType.A;
		}

		public static byte YOffset( this OperandType type, byte offset )
		{
			switch( type )
			{
				case OperandType.AdrBC:
				case OperandType.BC:
				case OperandType.B:
				case OperandType.C:
				default: return offset;

				case OperandType.AdrDE:
				case OperandType.DE:
				case OperandType.D:
				case OperandType.E: return (byte)( offset + 0x10 );

				case OperandType.AdrHLI:
				case OperandType.HL:
				case OperandType.H:
				case OperandType.L: return (byte)( offset + 0x20 );

				case OperandType.AdrHLD:
				case OperandType.AdrHL:
				case OperandType.SP:
				case OperandType.A: return (byte)( offset + 0x30 );
			}
		}

		public static bool IsD8( this OperandType type ) => type == OperandType.d8;
		public static bool IsD16( this OperandType type ) => type == OperandType.d16;
		public static bool IsR8( this OperandType type ) => type == OperandType.r8;
		public static bool IsIo8( this OperandType type ) => type == OperandType.io8;
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
				case OperandType.ioC: return $"0xFF00+C";
				case OperandType.A:
				case OperandType.B:
				case OperandType.C:
				case OperandType.D:
				case OperandType.E:
				case OperandType.H:
				case OperandType.L:
				case OperandType.BC:
				case OperandType.DE:
				case OperandType.HL:
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
			void Ext( byte val ) { mem[pc++] = 0xCB; mem[pc++] = val; }

			void Op1D8() { mem[pc++] = this[0].d8; }
			void Op2D8() { mem[pc++] = this[1].d8; }
			void Op1D16() { mem[pc++] = this[0].d16.GetLsb(); mem[pc++] = this[0].d16.GetMsb(); }
			void Op2D16() { mem[pc++] = this[1].d16.GetLsb(); mem[pc++] = this[1].d16.GetMsb(); }

			switch( Type )
			{
				case InstrType.Db:		Op1D8(); break;
				case InstrType.Nop:		Set(0x00); break;
				case InstrType.Stop:	Set(0x10); Op1D8(); break;
				case InstrType.Halt:	Set(0x76); break;
				case InstrType.Di:		Set(0xF3); break;
				case InstrType.Ei:		Set(0xFB); break;
				case InstrType.Ld:
					switch( Lhs )
					{
						// LD r8, r8
						case OperandType.B when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x40 ) ); break;
						case OperandType.C when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x48 ) ); break;
						case OperandType.D when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x50 ) ); break;
						case OperandType.E when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x58 ) ); break;
						case OperandType.H when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x60 ) ); break;
						case OperandType.L when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x68 ) ); break;
						// LD (HL), r8
						case OperandType.AdrHL when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x70 ) ); break;
						// LD A, r8
						case OperandType.A when Rhs.IsReg8HlA(): Set( Rhs.Reg8Offset( 0x78 ) ); break;
						// LD BC, d16
						case OperandType.BC when Rhs.IsD16(): Set( 0x01 ); Op2D16(); break;
						case OperandType.DE when Rhs.IsD16(): Set( 0x11 ); Op2D16(); break;
						case OperandType.HL when Rhs.IsD16(): Set( 0x21 ); Op2D16(); break;
						case OperandType.SP when Rhs.IsD16(): Set( 0x11 ); Op2D16(); break;
						// LD (BC), A
						case OperandType.AdrBC when Rhs == OperandType.A: Set( 0x02 ); break;
						case OperandType.AdrDE when Rhs == OperandType.A: Set( 0x12 ); break;
						case OperandType.AdrHLI when Rhs == OperandType.A: Set( 0x22 ); break;
						case OperandType.AdrHLD when Rhs == OperandType.A: Set( 0x32 ); break;
						// LD B, d8
						case OperandType.B when Rhs.IsD8(): Set( 0x06 ); Op2D8(); break;
						case OperandType.D when Rhs.IsD8(): Set( 0x16 ); Op2D8(); break;
						case OperandType.H when Rhs.IsD8(): Set( 0x26 ); Op2D8(); break;
						case OperandType.AdrHL when Rhs.IsD8(): Set( 0x36 ); Op2D8(); break;
						// LD A, (BC)
						case OperandType.A when Rhs == OperandType.AdrBC: Set( 0x0A ); break;
						case OperandType.A when Rhs == OperandType.AdrDE: Set( 0x1A ); break;
						case OperandType.A when Rhs == OperandType.AdrHLI: Set( 0x2A ); break;
						case OperandType.A when Rhs == OperandType.AdrHLD: Set( 0x3A ); break;
						// LD C, d8
						case OperandType.C when Rhs.IsD8(): Set( 0x0E ); Op2D8(); break;
						case OperandType.E when Rhs.IsD8(): Set( 0x1E ); Op2D8(); break;
						case OperandType.L when Rhs.IsD8(): Set( 0x2E ); Op2D8(); break;
						case OperandType.A when Rhs.IsD8(): Set( 0x3E ); Op2D8(); break;
						// LD (a16) sp
						case OperandType.d16 when Rhs == OperandType.SP: Set( 0x08 ); Op1D16(); break;
						// LD 0xFF00+r8, A
						case OperandType.io8 when Rhs == OperandType.A: Set( 0xE0 ); Op1D8(); break;
						// LD A, 0xFF00+r8
						case OperandType.A when Rhs == OperandType.io8: Set( 0xF0 ); Op2D8(); break;
						// LD (C), A
						case OperandType.ioC when Rhs == OperandType.A: Set( 0xE2 ); break;
						// LD A, (C)
						case OperandType.A when Rhs == OperandType.ioC: Set( 0xF2 ); break;
						// LD HL, SP+r8
						case OperandType.HL when Rhs == OperandType.SPr8: Set( 0xF8 ); Op2D8(); break;
						// LD SP, HL
						case OperandType.SP when Rhs == OperandType.HL: Set( 0xF9 ); break;
						// LD (a16), A
						case OperandType.d16 when Rhs == OperandType.A: Set( 0xEA ); Op1D16(); break;
						// LD A, (a16)
						case OperandType.A when Rhs.IsD16(): Set( 0xFA ); Op2D16(); break;
						default:
							break;
					}

					break;
				case InstrType.Inc:
					if( Lhs.IsReg16Adr() ) Set( Lhs.YOffset( 0x03 ) );
					else if( Lhs.IsBCDHHl() ) Set( Lhs.YOffset( 0x04 ) );
					else if( Lhs.IsCELA() ) Set( Lhs.YOffset( 0x0C ) );
					break;
				case InstrType.Dec:
					if( Lhs.IsReg16Adr() ) Set( Lhs.YOffset( 0x0B ) );
					else if( Lhs.IsBCDHHl() ) Set( Lhs.YOffset( 0x05 ) );
					else if( Lhs.IsCELA() ) Set( Lhs.YOffset( 0x0D ) );
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
					switch( Lhs )
					{
						case OperandType.condNZ when Rhs.IsD16(): Set( 0xC2 ); Op2D16(); break;
						case OperandType.condNC when Rhs.IsD16(): Set( 0xD2 ); Op2D16(); break;
						case OperandType.d16: Set( 0xC3 ); Op2D16(); break;
						case OperandType.condZ when Rhs.IsD16(): Set( 0x2A ); Op2D8(); break;
						case OperandType.condC when Rhs.IsD16(): Set( 0x3A ); Op2D8(); break;
						case OperandType.HL: Set( 0xE9 ); break;
						default: break;
					}

					break;
				case InstrType.Jr:
					switch( Lhs )
					{
						case OperandType.condNZ when Rhs.IsR8(): Set( 0x20 ); Op2D8(); break;
						case OperandType.condNC when Rhs.IsR8(): Set( 0x30 ); Op2D8(); break;
						case OperandType.r8: Set( 0x18 ); Op1D8(); break;
						case OperandType.condZ when Rhs.IsR8(): Set( 0x28 ); Op2D8(); break;
						case OperandType.condC when Rhs.IsR8(): Set( 0x38 ); Op2D8(); break;
						default: break;
					}

					break;
				case InstrType.Ret:
					if( Count == 0 ) Set( 0xC9 ); 
					else switch( Lhs )
					{
						case OperandType.condZ: Set( 0xC8 ); break;
						case OperandType.condC: Set( 0xD8 ); break;
						case OperandType.condNZ: Set( 0xC0 ); break;
						case OperandType.condNC: Set( 0xD0 ); break;
						default:
							break;
					}
					break;
				case InstrType.Reti: Set( 0xD9 ); break;
					break;
				case InstrType.Call:
					break;
				case InstrType.Rst:
					switch( this[0].d8 )
					{
						case 0x00: Set( 0xC7 ); break;
						case 0x10: Set( 0xD7 ); break;
						case 0x20: Set( 0xE7 ); break;
						case 0x30: Set( 0xF7 ); break;

						case 0x08: Set( 0xCF ); break;
						case 0x18: Set( 0xDF ); break;
						case 0x28: Set( 0xEF ); break;
						case 0x38: Set( 0xFF ); break;
						default:
							break;
					}
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
				case InstrType.Daa: Set( 0x27 ); break;
				case InstrType.Scf: Set( 0x37 ); break;
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
