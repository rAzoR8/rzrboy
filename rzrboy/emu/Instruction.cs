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

		Rst00,
		Rst10,
		Rst20,
		Rst30,

		Rst08,
		Rst18,
		Rst28,
		Rst38,

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
	}

	public class InstrAsm : List<Operand> 
	{
		public InstrAsm( InstrType type ) { Type = type; }
		public InstrAsm( InstrType type, params Operand[] operands ) : base(operands) { Type = type; }
		public InstrType Type { get; }

		// todo:
		public void Assemble( ref ushort pc, ISection mem ) { }

		public override string ToString()
		{
			return $"{Type}";
		}
	}

	public delegate InstrAsm DisAsm( ref ushort pc, ISection mem );

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
