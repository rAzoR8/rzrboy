using System.Text;

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
		
		A,F,
		B,C,
		D,E,
		H,L,

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
		public AsmInstr( InstrType type, params Operand[] operands ) : base(operands) { Type = type; }
		public AsmInstr( InstrType type, OperandType lhs ) { Type = type; Add( new( lhs ) ); }
		public AsmInstr( InstrType type, OperandType lhs, OperandType rhs ) { Type = type; Add( new( lhs ) ); Add( new( rhs ) ); }


		public InstrType Type { get; }

		/// <summary>
		/// Assemble to machine code
		/// </summary>
		/// <param name="pc"></param>
		/// <param name="mem"></param>
		/// <returns>Opcode</returns>
		public byte Assemble( ref ushort pc, ISection mem ) { return 0; }

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

	public delegate AsmInstr DisAsm( ref ushort pc, ISection mem );

	public class Ref<T> where T : struct
	{
		public Ref( T val )
		{
			Value = val;
		}

		public T Value = default;
		public static implicit operator T( Ref<T> val ) { return val.Value; }

		public override string ToString()
		{
			return $"{Value}";
		}
	}

	/// <summary>
	/// Each op takes one m-cycle.
	/// </summary>
	/// <param name="reg"></param>
	/// <param name="mem"></param>
	public delegate void Op( Reg reg, ISection mem );

	/// <summary>
	/// Mnemonic and operand name for this op
	/// </summary>
	/// <param name="pc"></param>
	/// <param name="mem"></param>
	/// <returns></returns>
	public delegate string Dis( ref ushort pc, ISection mem );

	public delegate IEnumerable<Op> ProduceInstruction();

	/// <summary>
	/// Instruction op does not include instruction fetch cycle
	/// </summary>
	public class Instruction
	{
		public ProduceInstruction Make { get; }
		private List<Dis> m_dis = new();

		public Instruction( ProduceInstruction ops, Dis? dis = null )
		{
			Make = ops;
			if( dis != null )
			{
				m_dis.Add( dis );
			}
		}

		public Instruction( Op op, Dis? dis = null )
			: this( () => Enumerable.Repeat( op, 1 ), dis )
		{
		}

		public Instruction( ProduceInstruction ops, string mnemonic )
		: this( ops, Ops.mnemonic( mnemonic ) )
		{
		}

		public static implicit operator Instruction( Op op ) { return new Instruction( op ); }

		public static implicit operator Instruction( ProduceInstruction ops ) { return new Instruction( ops ); }

		public virtual IEnumerable<string> Operands( Ref<ushort> pc, Section mem )
		{
			return m_dis.Select( dis => dis( ref pc.Value, mem ) );
		}

		public string ToString( ref ushort pc, Section mem )
		{
			Ref<ushort> ref_pc = new( pc );
			string[] seps = { " ", ", ", "" };
			string[] ops = Operands( ref_pc, mem ).ToArray();

			StringBuilder sb = new();

			int i = 0;
			foreach( string op in ops )
			{
				sb.Append( op );
				if( i + 1 < ops.Length )
				{
					sb.Append( seps[i++] );
				}
			}
			pc = ref_pc;

			return sb.ToString();
		}

		public static Instruction operator +( Instruction b, Dis op ) { b.m_dis.Add( op ); return b; }
		public static Instruction operator +( Instruction b, IEnumerable<Dis> dis ) { b.m_dis.AddRange( dis ); return b; }
		public static Instruction operator +( Instruction b, string str ) { b.m_dis.Add( Ops.mnemonic( str ) ); return b; }
	}

	public static class InstructionExtensions
	{
		public static Instruction Get( this ProduceInstruction op ) => new Instruction( op );
		public static Instruction Get( this ProduceInstruction op, string mnemonic ) => new Instruction( op ) + mnemonic;
		public static Instruction Get( this ProduceInstruction op, Dis dis ) => new Instruction( op ) + dis;

		public static Instruction Get( this Op op ) => new Instruction( op );
		public static Instruction Get( this Op op, string mnemonic ) => new Instruction( op ) + mnemonic;
		public static Instruction Get( this Op op, Dis dis ) => new Instruction( op ) + dis;

		// Debug name
		public static string ToString( this ProduceInstruction ops )
		{
			return ops.Method.Name;
		}
	}
}
