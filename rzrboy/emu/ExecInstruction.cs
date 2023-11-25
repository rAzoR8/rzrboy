namespace rzr
{
	/// <summary>
	/// Each op (operation) takes one m-cycle.
	/// </summary>
	/// <param name="reg"></param>
	/// <param name="mem"></param>
	public delegate void CpuOp( RegView reg, ISection mem );

	/// <summary>
	/// Operation stream producer
	/// </summary>
	/// <returns></returns>
	public delegate IEnumerable<CpuOp> CpuOpFactory();

	/// <summary>
	/// ExecInstr op does not include instruction fetch cycle
	/// </summary>
	public class ExecInstr // TODO: rename to CpuInstruction & move to CPU class?
	{
		public CpuOpFactory Make { get; }

		public ExecInstr( CpuOpFactory ops )
		{
			Make = ops;
		}

		public ExecInstr( CpuOp op )
			: this( () => Enumerable.Repeat( op, 1 ) )
		{
		}

		public static implicit operator ExecInstr( CpuOp op ) { return new ExecInstr( op ); }
	}

	public static class ExecInstrExtensions
	{
		// Debug name
		public static string ToString( this CpuOpFactory ops )
		{
			return ops.Method.Name;
		}
	}
}
