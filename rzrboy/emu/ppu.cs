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

		public const uint FrameDots = 70224;

		public void Tick( IEmuState state )
		{
			if( state is State s )
				TickInternal( s );			
        }

		private void TickInternal( State state ) 
		{
			// Single Speed: 4 Dots = 1 M-Cycle
			// Double Speed: 2 Dots = 1 M-Cycle

			// 456		Dots per Scanline
			// 80		Dots OAM search (Mode 2)
			// 172-289	Dots Drawing (Mode 3)
			// 87–204	Dots HBlank (Mode 0) = (376 - Drawing, Mode 3)
			// 10x456	Dots VBlank (Mode 1)

			Pix pix = state.pix;
			VRam vram = state.m_mem.vram;
			Section oam = state.m_mem.oam;
			IOSection io = state.m_mem.io;

			if( pix.Dot >= FrameDots ) // done with frame, start anew
				pix.Dot -= FrameDots;

			LCDC lcdc = new() { Value = io.LcdControl };
			STAT stat = new() { Value = io.LcdStatus };

			ushort drawing = 0;

			// TODO: interrupts
			if( lcdc.LCDOn )
			{
				PPUMode mode = stat.Mode;
				byte LY = io.LY;
				// [0..456] dot in the current line
				var dot = pix.Dot - LY * 456;
				if( LY > 143 ) // VBlank LY 144->153
					mode = PPUMode.VBlank;
				else if( dot < 80 ) // OAM Search -> drawing
					mode = PPUMode.Drawing;
				else if( dot - 80 - drawing > 0 )
					mode = PPUMode.HBlank;
			}

			pix.Dot++;
		}
	}
}
