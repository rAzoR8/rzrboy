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
	/// Each op (operation) takes one m-cycle.
	/// </summary>
	/// <param name="reg"></param>
	/// <param name="mem"></param>
	public delegate void Op( Reg reg, ISection mem );

	public delegate IEnumerable<Op> OpFactory();

	/// <summary>
	/// ExecInstr op does not include instruction fetch cycle
	/// </summary>
	public class ExecInstr
	{
		public OpFactory Make { get; }

		public ExecInstr( OpFactory ops )
		{
			Make = ops;
		}

		public ExecInstr( Op op )
			: this( () => Enumerable.Repeat( op, 1 ) )
		{
		}

		public static implicit operator ExecInstr( Op op ) { return new ExecInstr( op ); }
	}

	public static class InstructionExtensions
	{
		// Debug name
		public static string ToString( this OpFactory ops )
		{
			return ops.Method.Name;
		}
	}
}
