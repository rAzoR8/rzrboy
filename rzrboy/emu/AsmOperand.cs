namespace rzr
{
	public static class AddressExt
	{
		public static Address Adr( this ushort adr ) => new( adr );
		public static Address Adr( this int adr ) => new( (ushort)adr );
	}

	public record struct Address( ushort adr )
	{
		public static implicit operator ushort( Address adr ) => adr.adr;
	}

	public class AsmOperand : IEquatable<AsmOperand>
	{
		public AsmOperand( OperandType type ) { Type = type; }
		public AsmOperand( OperandType type, byte val ) { Type = type; d16 = val; }
		public AsmOperand( OperandType type, sbyte val ) { Type = type; d16 = (byte)val; }
		public AsmOperand( OperandType type, ushort val ) { Type = type; d16 = val; }

		public AsmOperand( byte d8 ) { Type = OperandType.d8; d16 = d8; }
		public AsmOperand( sbyte r8 ) { Type = OperandType.r8; d16 = (byte)r8; }
		public AsmOperand( ushort d16 ) { Type = OperandType.d16; this.d16 = d16; }
		public AsmOperand( Address adr ) { Type = OperandType.a16; this.d16 = adr; }

		public static implicit operator AsmOperand( OperandType type ) { return new AsmOperand( type ); }
		public static implicit operator OperandType( AsmOperand op ) { return op.Type; }

		public OperandType Type { get; set; }
		public ushort d16 { get; set; } = 0;
		public sbyte r8 { get => (sbyte)d16.GetLsb(); set => d16 = (byte)value; }
		public byte d8 { get => d16.GetLsb(); set => d16 = value; }

		public string ToString( ushort? pc )
		{
			switch( Type )
			{
				case OperandType.BitIdx:
				case OperandType.RstAddr:
				case OperandType.d8: return $"${d8:X2}";
				case OperandType.r8 when !pc.HasValue: return $"${r8:X2}";
				case OperandType.r8 when pc.HasValue: return $"{r8} (->${pc + r8:X4})";
				case OperandType.d16: return $"${d16:X4}";
				case OperandType.a16: return $"(${d16:X4})";
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
				case OperandType.SPr8: return $"SP+{r8:X2} ({r8})";
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

		public override string ToString()
		{
			return ToString( null );
		}

		public bool Equals( AsmOperand? other )
		{
			if( other == null ) return false;
			if( this == other ) return true;

			return Type == other.Type && d16 == other.d16;
		}
	}
}
