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

	public delegate IEnumerable<Op> ProduceInstruction();

	/// <summary>
	/// Instruction op does not include instruction fetch cycle
	/// </summary>
	public class Instruction
	{
		public ProduceInstruction Make { get; }

		public Instruction( ProduceInstruction ops )
		{
			Make = ops;
		}

		public Instruction( Op op )
			: this( () => Enumerable.Repeat( op, 1 ) )
		{
		}

		public static implicit operator Instruction( Op op ) { return new Instruction( op ); }
	}

	public static class InstructionExtensions
	{
		// Debug name
		public static string ToString( this ProduceInstruction ops )
		{
			return ops.Method.Name;
		}
	}
}
