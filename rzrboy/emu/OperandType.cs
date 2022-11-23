namespace rzr
{
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
		A, // F not direclty accessible

		AdrBC, // (BC)
		AdrDE, // (DE)
		AdrHLi, // (HL+)
		AdrHLd, // (HL-)

		BC,
		DE,
		HL,
		SP,
		AF, // only for Push & Pop

		SPr8, // SP + r8

		RstAddr, // RST vec operand, stored d8
		BitIdx, // BIT, RES, SET index operand, stored d8

		condZ,
		condNZ,
		condC,
		condNC,

		none
	}

	public static class OperandExtensions
	{
		public static string ToString( this OperandType type )
		{
			switch( type )
			{
				case OperandType.BitIdx:
				case OperandType.RstAddr:
				case OperandType.d8:
				case OperandType.r8:
				case OperandType.d16:
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
					return type.ToString();
				case OperandType.io8: return "($FF00+r8)";
				case OperandType.ioC: return "($FF00+C)";
				case OperandType.SPr8: return "SP+r8";
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

		// X offset for B C D E H L (HL) A
		public static byte Reg8XOffset( this OperandType type, byte offset )
		{
			switch( type )
			{

				case OperandType.B:
				default: return offset;
				case OperandType.C: return (byte)( offset + 0x01 );
				case OperandType.D: return (byte)( offset + 0x02 );
				case OperandType.E: return (byte)( offset + 0x03 );
				case OperandType.H: return (byte)( offset + 0x04 );
				case OperandType.L: return (byte)( offset + 0x05 );
				case OperandType.AdrHL: return (byte)( offset + 0x06 );
				case OperandType.A: return (byte)( offset + 0x07 );
			}
		}
		// is A
		public static bool IsA( this OperandType type ) => type == OperandType.A;
		// is HL
		public static bool IsHl( this OperandType type ) => type == OperandType.HL;
		// is SP
		public static bool IsSP( this OperandType type ) => type == OperandType.SP;
		// is B C D E H L (HL) A
		public static bool IsReg8HlA( this OperandType type ) { return type >= OperandType.B && type <= OperandType.A; }
		// is (BC) (DE) (HL+) (HL-)
		public static bool IsReg16Adr( this OperandType type ) { return type >= OperandType.AdrBC && type <= OperandType.AdrHLd; }
		// is B D H (HL)
		public static bool IsBCDHHl( this OperandType type )
		{
			return
				type == OperandType.B ||
				type == OperandType.D ||
				type == OperandType.H ||
				type == OperandType.AdrHL;
		}
		// is C E L A
		public static bool IsCELA( this OperandType type )
		{
			return
				type == OperandType.C ||
				type == OperandType.E ||
				type == OperandType.L ||
				type == OperandType.A;
		}
		// is BC DE HL SP
		public static bool IsBcDeHlSp( this OperandType type )
		{
			return
				type == OperandType.BC ||
				type == OperandType.DE ||
				type == OperandType.HL ||
				type == OperandType.SP;
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

				case OperandType.AdrHLi:
				case OperandType.HL:
				case OperandType.H:
				case OperandType.L: return (byte)( offset + 0x20 );

				case OperandType.AdrHLd:
				case OperandType.AdrHL:
				case OperandType.SP:
				case OperandType.AF:
				case OperandType.A: return (byte)( offset + 0x30 );
			}
		}

		public static bool IsD8( this OperandType type ) => type == OperandType.d8;
		public static bool IsD16( this OperandType type ) => type == OperandType.d16;
		public static bool IsR8( this OperandType type ) => type == OperandType.r8;
		public static bool IsIo8( this OperandType type ) => type == OperandType.io8;

		public static AsmInstr Ops( this InstrType type, AsmOperand lhs, AsmOperand rhs ) { return new AsmInstr( type, lhs, rhs ); }
		public static AsmInstr Ops( this InstrType type, OperandType lhs, OperandType rhs ) { return new AsmInstr( type, lhs, rhs ); }
	}
}
