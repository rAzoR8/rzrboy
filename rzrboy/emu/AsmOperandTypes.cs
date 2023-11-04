namespace rzr
{
	public static class AsmOperandTypes
	{
		public interface IOpType { OperandType Type { get; } }

		// B D H (HL)
		public interface BDHhl : IOpType { }
		// C E L A
		public interface CELA : IOpType { }
		// BC DE HL SP
		public interface BcDeHlSp : IOpType { }
		// (BC) (DE) (HL+) (HL-)
		public interface AdrBcDeHliHld : IOpType { }

		public struct Atype : CELA { public OperandType Type => OperandType.A; }
		public struct Btype : BDHhl { public OperandType Type => OperandType.B; }
		public struct Ctype : CELA { public OperandType Type => OperandType.C; }
		public struct Dtype : BDHhl { public OperandType Type => OperandType.D; }
		public struct Etype : CELA { public OperandType Type => OperandType.E; }
		public struct Htype : BDHhl { public OperandType Type => OperandType.H; }
		public struct Ltype : CELA { public OperandType Type => OperandType.L; }

		// (HL)
		public struct AdrHLtype : BDHhl { public OperandType Type => OperandType.AdrHL; }

		public struct AdrBCtype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrBC; }
		public struct AdrDEtype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrDE; }
		public struct AdrHLitype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrHLi; }
		public struct AdrHLdtype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrHLd; }

		// IoC 0xFF00+C
		public struct AdrCtype : IOpType { public OperandType Type => OperandType.ioC; }

		public struct BCtype : BcDeHlSp { public OperandType Type => OperandType.BC; public AdrBCtype Adr => adrBC; }
		public struct DEtype : BcDeHlSp { public OperandType Type => OperandType.DE; public AdrDEtype Adr => adrDE; }
		public struct HLtype : BcDeHlSp { public OperandType Type => OperandType.HL; public AdrHLtype Adr => adrHL; }
		public struct SPtype : BcDeHlSp { public OperandType Type => OperandType.SP; }

		public struct AFtype : IOpType { public OperandType Type => OperandType.AF; }

		public interface Condtype : IOpType { }
		public struct CondZtype : Condtype { public OperandType Type => OperandType.condZ; }
		public struct CondNZtype : Condtype { public OperandType Type => OperandType.condNZ; }
		public struct CondCtype : Condtype { public OperandType Type => OperandType.condC; }
		public struct CondNCtype : Condtype { public OperandType Type => OperandType.condNC; }

		public static readonly Atype A;
		public static readonly Btype B;
		public static readonly Ctype C;
		public static readonly Dtype D;
		public static readonly Etype E;
		public static readonly Htype H;
		public static readonly Ltype L;

		public static readonly BCtype BC;
		public static readonly DEtype DE;
		public static readonly HLtype HL;
		public static readonly SPtype SP;
		public static readonly AFtype AF; // Push/Pop only

		public static readonly AdrBCtype adrBC;
		public static readonly AdrDEtype adrDE;
		public static readonly AdrHLtype adrHL;
		public static readonly AdrHLitype adrHLi;
		public static readonly AdrHLdtype adrHLd;
		public static readonly AdrCtype adrC; // IoC

		public static readonly CondZtype isZ;
		public static readonly CondNZtype isNZ;
		public static readonly CondCtype isC;
		public static readonly CondNCtype isNC;
	}

}
