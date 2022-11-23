namespace rzr
{
	public enum InstrType
	{
		Invalid,
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

	public class Operand : IEquatable<Operand>
	{
		public Operand( OperandType type ) { Type = type; }
		public Operand( OperandType type, byte val ) { Type = type; d16 = val; }
		public Operand( OperandType type, sbyte val ) { Type = type; d16 = (byte)val; }
		public Operand( OperandType type, ushort val ) { Type = type; d16 = val; }

		public Operand( byte d8 ) { Type = OperandType.d8; d16 = d8; }
		public Operand( sbyte r8 ) { Type = OperandType.r8; d16 = (byte)r8; }
		public Operand( ushort d16 ) { Type = OperandType.d16; this.d16 = d16; }

		public static implicit operator Operand( OperandType type ) { return new Operand( type ); }
		public static implicit operator OperandType( Operand op ) { return op.Type; }

		public OperandType Type { get; set; }
		public ushort d16 { get; set; } = 0;
		public sbyte r8 { get => (sbyte)d16.GetLsb(); set => d16 = (byte)value; }
		public byte d8 {get => d16.GetLsb(); set => d16 = value; }

		public override string ToString()
		{
			switch( Type )
			{
				case OperandType.BitIdx:
				case OperandType.RstAddr:
				case OperandType.d8: return $"${d8:X2}";
				case OperandType.r8: return $"${r8:X2}";
				case OperandType.d16: return $"${d16:X4}";
				case OperandType.io8: return $"($FF00+{d8:X2})";
				case OperandType.ioC: return $"($FF00+C)";
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
				case OperandType.AF:
					return Type.ToString();
				case OperandType.SPr8: return $"SP+{r8:X2}";
				case OperandType.AdrHL: return "(HL)";
				case OperandType.AdrHLi: return "(HL+)";
				case OperandType.AdrHLd: return "(HL-)";
				case OperandType.AdrBC: return "(BC)";
				case OperandType.AdrDE: return "(DE)";
				case OperandType.condZ: return "Z";
				case OperandType.condNZ: return "NZ";
				case OperandType.condC: return "C";
				case OperandType.condNC: return "NC";
				case OperandType.none: return "";
				default: return "?";
			}
		}

		public bool Equals( Operand? other )
		{
			if( other == null ) return false;
			if( this == other ) return true;

			return Type == other.Type && d16 == other.d16;
		}
	}

	public class AsmInstr : List<Operand>
	{
		public static readonly AsmInstr Invalid = new AsmInstr( InstrType.Invalid );
		public AsmInstr( InstrType type ) { Type = type; }
		public AsmInstr( InstrType type, params Operand[] operands ) : base( operands ) { Type = type; }

		public static implicit operator AsmInstr( InstrType type ) { return new AsmInstr( type ); }
		public static AsmInstr operator +( AsmInstr i, Operand op ) { i.Add(op); return i; }
		public static AsmInstr operator +( AsmInstr i, OperandType op ) { i.Add( op ); return i; }

		public InstrType Type { get; set; }

		public OperandType Lhs { get => this[0].Type; set => this[0].Type = value; }
		public OperandType Rhs { get => this[1].Type; set => this[1].Type = value; }

		public Operand L { get => this[0]; set => this[0] = value; }
		public Operand R { get => this[1]; set => this[1] = value; }

		public void SetL( Operand op ) 
		{
			if( Count == 0 )
				Add( op );
			else
				this[0] = op;
		}

		public void SetR( Operand op, OperandType defaultL = OperandType.A )
		{
			if( Count == 0 )
			{
				Add( defaultL );
				Add( op );
			}
			else if( Count == 1 )
				Add( op );
			else
				this[1] = op;
		}

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
				case InstrType.Stop:	Set(0x10); if(Count>0) Op1D8(); break;
				case InstrType.Halt:	Set(0x76); break;
				case InstrType.Di:		Set(0xF3); break;
				case InstrType.Ei:		Set(0xFB); break;
				case InstrType.Ld:
					switch( Lhs )
					{
						// LD r8, r8
						case OperandType.B when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x40 ) ); break;
						case OperandType.C when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x48 ) ); break;
						case OperandType.D when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x50 ) ); break;
						case OperandType.E when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x58 ) ); break;
						case OperandType.H when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x60 ) ); break;
						case OperandType.L when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x68 ) ); break;
						// LD (HL), r8
						case OperandType.AdrHL when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x70 ) ); break;
						// LD A, r8
						case OperandType.A when Rhs.IsReg8HlA(): Set( Rhs.Reg8XOffset( 0x78 ) ); break;
						// LD BC, d16
						case OperandType.BC when Rhs.IsD16(): Set( 0x01 ); Op2D16(); break;
						case OperandType.DE when Rhs.IsD16(): Set( 0x11 ); Op2D16(); break;
						case OperandType.HL when Rhs.IsD16(): Set( 0x21 ); Op2D16(); break;
						case OperandType.SP when Rhs.IsD16(): Set( 0x11 ); Op2D16(); break;
						// LD (BC), A
						case OperandType.AdrBC when Rhs == OperandType.A: Set( 0x02 ); break;
						case OperandType.AdrDE when Rhs == OperandType.A: Set( 0x12 ); break;
						case OperandType.AdrHLi when Rhs == OperandType.A: Set( 0x22 ); break;
						case OperandType.AdrHLd when Rhs == OperandType.A: Set( 0x32 ); break;
						// LD B, d8
						case OperandType.B when Rhs.IsD8(): Set( 0x06 ); Op2D8(); break;
						case OperandType.D when Rhs.IsD8(): Set( 0x16 ); Op2D8(); break;
						case OperandType.H when Rhs.IsD8(): Set( 0x26 ); Op2D8(); break;
						case OperandType.AdrHL when Rhs.IsD8(): Set( 0x36 ); Op2D8(); break;
						// LD A, (BC)
						case OperandType.A when Rhs == OperandType.AdrBC: Set( 0x0A ); break;
						case OperandType.A when Rhs == OperandType.AdrDE: Set( 0x1A ); break;
						case OperandType.A when Rhs == OperandType.AdrHLi: Set( 0x2A ); break;
						case OperandType.A when Rhs == OperandType.AdrHLd: Set( 0x3A ); break;
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
				// ADD [B C D E H L (HL) A]
				case InstrType.Add when Lhs.IsA() && Rhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0x80 ) ); break;
				case InstrType.Adc when Lhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0x88 ) ); break;
				case InstrType.Sub when Lhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0x90 ) ); break;
				case InstrType.Sbc when Lhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0x98 ) ); break;
				case InstrType.And when Lhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0xA0 ) ); break;
				case InstrType.Xor when Lhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0xA8 ) ); break;
				case InstrType.Or when Lhs.IsReg8HlA():	Set( Lhs.Reg8XOffset( 0xB0 ) ); break;
				case InstrType.Cp when Lhs.IsReg8HlA(): Set( Lhs.Reg8XOffset( 0xB8 ) ); break;
				// ADD db8
				case InstrType.Add when Lhs.IsA() && Rhs.IsD8(): Set( 0xC6 ); Op1D8(); break;
				case InstrType.Sub when Lhs.IsD8(): Set( 0xD6 ); Op1D8(); break;
				case InstrType.And when Lhs.IsD8(): Set( 0xE6 ); Op1D8(); break;
				case InstrType.Or when Lhs.IsD8(): Set( 0xF6 ); Op1D8(); break;
				case InstrType.Adc when Lhs.IsD8(): Set( 0xCE ); Op1D8(); break;
				case InstrType.Sbc when Lhs.IsD8(): Set( 0xDE ); Op1D8(); break;
				case InstrType.Xor when Lhs.IsD8(): Set( 0xEE ); Op1D8(); break;
				case InstrType.Cp when Lhs.IsD8(): Set( 0xFE ); Op1D8(); break;
				// ADD HL, [BC DE HL SP]
				case InstrType.Add when Lhs.IsHl() && Rhs.IsBCDHHl(): Set( Rhs.YOffset( 0x09 ) ); break;
				// ADD SP, r8
				case InstrType.Add when Lhs.IsSP() && Rhs.IsR8(): Set(0xE8); break;
				case InstrType.Jp:
					switch( Lhs )
					{
						case OperandType.condNZ when Rhs.IsD16(): Set( 0xC2 ); Op2D16(); break;
						case OperandType.condNC when Rhs.IsD16(): Set( 0xD2 ); Op2D16(); break;
						case OperandType.d16: Set( 0xC3 ); Op1D16(); break;
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
						case OperandType.condNZ: Set( 0xC0 ); break;
						case OperandType.condNC: Set( 0xD0 ); break;
						case OperandType.condZ: Set( 0xC8 ); break;
						case OperandType.condC: Set( 0xD8 ); break;
						default:
							break;
					}
					break;
				case InstrType.Reti: Set( 0xD9 ); break;
				case InstrType.Call:
					if( Count == 1 ) { Set( 0xCD ); Op2D16(); }
					else if( Count == 2 ) switch( Lhs )
					{
						case OperandType.condNZ: Set( 0xC4 ); Op2D16(); break;
						case OperandType.condNC: Set( 0xD4 ); Op2D16(); break;
						case OperandType.condZ: Set( 0xCC ); Op2D16(); break;
						case OperandType.condC: Set( 0xDC ); Op2D16(); break;
						default:
							break;
					}
					break;
				case InstrType.Rst when Lhs == OperandType.RstAddr:
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
					switch( Lhs )
					{
						case OperandType.BC: Set( 0xC1 ); break;
						case OperandType.DE: Set( 0xD1 ); break;
						case OperandType.HL: Set( 0xE1 ); break;
						case OperandType.AF: Set( 0xF1 ); break;
						default:
							break;
					}
					break;
				case InstrType.Pop:
					switch( Lhs )
					{
						case OperandType.BC: Set( 0xC5 ); break;
						case OperandType.DE: Set( 0xD5 ); break;
						case OperandType.HL: Set( 0xE5 ); break;
						case OperandType.AF: Set( 0xF5 ); break;
						default:
							break;
					}
					break;
				case InstrType.Rlca:Set( 0x07 ); break;
				case InstrType.Rla: Set( 0x17 ); break;
				case InstrType.Rrca:Set( 0x0F ); break;
				case InstrType.Rra: Set( 0x1F ); break;
				case InstrType.Daa: Set( 0x27 ); break;
				case InstrType.Scf: Set( 0x37 ); break;
				case InstrType.Cpl: Set( 0x2F ); break;
				case InstrType.Ccf: Set( 0X3F ); break;
				case InstrType.Rlc when Lhs.IsReg8HlA(): Ext( Lhs.Reg8XOffset( 0x00 ) ); break;
				case InstrType.Rrc when Lhs.IsReg8HlA(): Ext( Lhs.Reg8XOffset( 0x08 ) ); break;
				case InstrType.Rl when Lhs.IsReg8HlA():  Ext( Lhs.Reg8XOffset( 0x10 ) ); break;
				case InstrType.Rr when Lhs.IsReg8HlA():  Ext( Lhs.Reg8XOffset( 0x18 ) ); break;
				case InstrType.Sla when Lhs.IsReg8HlA(): Ext( Lhs.Reg8XOffset( 0x20 ) ); break;
				case InstrType.Sra when Lhs.IsReg8HlA(): Ext( Lhs.Reg8XOffset( 0x28 ) ); break;
				case InstrType.Swap when Lhs.IsReg8HlA():Ext( Lhs.Reg8XOffset( 0x30 ) ); break;
				case InstrType.Srl when Lhs.IsReg8HlA(): Ext( Lhs.Reg8XOffset( 0x38 ) ); break;
				case InstrType.Bit when Lhs == OperandType.BitIdx && Rhs.IsReg8HlA():
					switch( this[0].d8 )
					{
						case 0: Ext( Rhs.Reg8XOffset( 0x40 ) ); break;
						case 2: Ext( Rhs.Reg8XOffset( 0x50 ) ); break;
						case 4: Ext( Rhs.Reg8XOffset( 0x60 ) ); break;
						case 6: Ext( Rhs.Reg8XOffset( 0x70 ) ); break;

						case 1: Ext( Rhs.Reg8XOffset( 0x48 ) ); break;
						case 3: Ext( Rhs.Reg8XOffset( 0x58 ) ); break;
						case 5: Ext( Rhs.Reg8XOffset( 0x68 ) ); break;
						case 7: Ext( Rhs.Reg8XOffset( 0x78 ) ); break;
						default:
							break;
					}
					break;
				case InstrType.Res when	Lhs == OperandType.BitIdx && Rhs.IsReg8HlA():
					switch( this[0].d8 )
					{
						case 0: Ext( Rhs.Reg8XOffset( 0x80 ) ); break;
						case 2: Ext( Rhs.Reg8XOffset( 0x90 ) ); break;
						case 4: Ext( Rhs.Reg8XOffset( 0xA0 ) ); break;
						case 6: Ext( Rhs.Reg8XOffset( 0xB0 ) ); break;

						case 1: Ext( Rhs.Reg8XOffset( 0x88 ) ); break;
						case 3: Ext( Rhs.Reg8XOffset( 0x98 ) ); break;
						case 5: Ext( Rhs.Reg8XOffset( 0xA8 ) ); break;
						case 7: Ext( Rhs.Reg8XOffset( 0xB8 ) ); break;
						default:
							break;
					}
					break;
				case InstrType.Set when Lhs == OperandType.BitIdx && Rhs.IsReg8HlA():
					switch( this[0].d8 )
					{
						case 0: Ext( Rhs.Reg8XOffset( 0xC0 ) ); break;
						case 2: Ext( Rhs.Reg8XOffset( 0xD0 ) ); break;
						case 4: Ext( Rhs.Reg8XOffset( 0xE0 ) ); break;
						case 6: Ext( Rhs.Reg8XOffset( 0xF0 ) ); break;

						case 1: Ext( Rhs.Reg8XOffset( 0xC8 ) ); break;
						case 3: Ext( Rhs.Reg8XOffset( 0xD8 ) ); break;
						case 5: Ext( Rhs.Reg8XOffset( 0xE8 ) ); break;
						case 7: Ext( Rhs.Reg8XOffset( 0xF8 ) ); break;
						default:
							break;
					}
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
