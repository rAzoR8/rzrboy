using System.Text;

namespace rzr
{
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
