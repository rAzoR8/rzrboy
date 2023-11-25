namespace rzr
{
    public class Emu : IEmulator
    {
		public Ppu ppu { get; }
		public Cpu cpu { get; }
        public Apu apu { get; }

        public ILogger Logger { get; }
        
		public bool IsRunning { get; private set; }
        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public delegate void Callback( IEmuState state );

		public List<Callback> PreStepCallbacks { get; } = new();
		public List<Callback> PostStepCallbacks { get; } = new();

		public Emu(ILogger logger)
        {
            cpu = new Cpu();
            ppu = new Ppu();
            apu = new Apu();
            Logger = logger;
        }

		public bool Tick( IEmuState state )
		{
            bool cont;
			try
			{
				cont = cpu.Tick( state );
				ppu.Tick( state );
				apu.Tick( state );
			}
			catch( rzr.ExecException e )
			{
				Logger.Log( e );
				cont = false;
			}

			return cont;
        }

        /// <summary>
        /// execute one complete instruction
        /// </summary>
        /// <returns>number of M-cycles the current instruction took with overlapped fetch</returns>
        public void Step( IEmuState state )
        {
            uint cycles = 1;

			foreach( Callback fun in PreStepCallbacks )
			{
				fun( state );
			}

			// execute all ops
			while ( Tick( state ) ) { ++cycles; }

			//{
			//	ushort pc = state.prevInstrPC;
			//	Logger.Log( $"{Isa.Disassemble( ref pc, state.mem )} {cycles}:{state.prevInstrCycles} cycles|fetch" );
			//}

			foreach ( Callback fun in PostStepCallbacks )
            {
                fun( state );
            }

            //return cycles;
        }
    }
}