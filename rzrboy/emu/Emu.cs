namespace rzr
{
    public class Emu
    {
		public Ppu ppu { get; }
		public Cpu cpu { get; }
        public Apu apu { get; }

        public ILogger Logger { get; }
        
		public bool IsRunning { get; private set; }
        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public delegate void Callback( State state );

		public List<Callback> PreStepCallbacks { get; } = new();
		public List<Callback> PostStepCallbacks { get; } = new();

		public Emu(ILogger logger)
        {
            cpu = new Cpu();
            ppu = new Ppu();
            apu = new Apu();
            Logger = logger;
        }

		public delegate State NextStateFn();
        public async Task<ulong> Execute( NextStateFn nextState, CancellationToken token = default )
        {
            ulong cycles = 0;

            try
            {
                await Task.Run( () =>
                {
                    IsRunning = true;
                    while( true )
                    {
                        token.ThrowIfCancellationRequested();
                        cycles += Step( state: nextState(), token: token, debugPrint: true );
                    }
                } );
            }
            catch( OperationCanceledException e )
            {
                if(e.CancellationToken == token)
                    IsRunning = false;
                else
                    return await Execute( nextState: nextState, token );
            }

            return cycles;
        }

		public bool Tick( State state, CancellationToken? token = null )
		{
            bool cont;
			try
			{
				cont = cpu.Tick( state );
				ppu.Tick( state );
				apu.Tick( state.reg, state.mem );
			}
			catch( rzr.ExecException e )
			{
				Logger.Log( e );
                cont = false;
				if( token != null )
					throw new OperationCanceledException( message: e.Message, innerException: e, token.Value );
			}

			return cont;
        }

        /// <summary>
        /// execute one complete instruction
        /// </summary>
        /// <returns>number of M-cycles the current instruction took with overlapped fetch</returns>
        public uint Step( State state, bool debugPrint, CancellationToken? token = null )
        {
            uint cycles = 1;

			foreach( Callback fun in PreStepCallbacks )
			{
				fun( state );
			}

			// execute all ops
			while ( Tick( state, token ) ) { ++cycles; }

            if ( debugPrint )
            {
                ushort pc = state.prevInstrPC;
                Logger.Log( $"{Isa.Disassemble( ref pc, state.mem )} {cycles}:{state.prevInstrCycles} cycles|fetch" );
            }

            foreach ( Callback fun in PostStepCallbacks )
            {
                fun( state );
            }

            return cycles;
        }
    }
}