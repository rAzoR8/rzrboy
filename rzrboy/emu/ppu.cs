namespace rzr
{
		/// <summary>
	/// Each op (operation) takes one m-cycle.
	/// </summary>
	/// <param name="io">IO section</param>
	/// <param name="vram"></param>
	/// <param name="oam"></param>
	/// <param name="pix"></param>
	public delegate void PpuOp( IOSection io, ISection vram, ISection oam, Pix pix );

	/// <summary>
	/// Operation stream producer
	/// </summary>
	/// <returns></returns>
	public delegate IEnumerable<PpuOp> PpuOpFactory();

	/// <summary>
	/// ExecInstr op does not include instruction fetch cycle
	/// </summary>
	public class PpuInstr
	{
		public PpuOpFactory Make { get; }

		public PpuInstr( PpuOpFactory ops )
		{
			Make = ops;
		}

		public PpuInstr( PpuOp op )
			: this( () => Enumerable.Repeat( op, 1 ) )
		{
		}

		public static implicit operator PpuInstr( PpuOp op ) { return new PpuInstr( op ); }
	}

	public static class PpuInstrExtensions
	{
		// Debug name
		public static string ToString( this PpuOpFactory ops )
		{
			return ops.Method.Name;
		}
	}

    public class Ppu
    {
        public Ppu()
        {
        }

		// TODO: make static
        public void Tick( IEmuState state )
		{
			//state.pix.Tick++;
        }
    }
}
