namespace rzr
{
	/// <summary>
	/// Each op (operation) takes one m-cycle.
	/// </summary>
	/// <param name="reg"></param>
	/// <param name="mem"></param>
	public delegate void Op( Reg reg, ISection mem );

	/// <summary>
	/// Operation stream producer
	/// </summary>
	/// <returns></returns>
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

	public static class ExecInstrExtensions
	{
		// Debug name
		public static string ToString( this OpFactory ops )
		{
			return ops.Method.Name;
		}
	}
}
