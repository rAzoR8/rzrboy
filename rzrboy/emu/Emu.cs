using System.Diagnostics;

namespace rzr
{
    public class Emu
    {
		public Ppu ppu { get; }
		public Cpu cpu { get; }
        public Apu apu { get; }

		public bool IsRunning { get; private set; }
        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public delegate void Callback( State state );

		public List<Callback> PreStepCallbacks { get; } = new();
		public List<Callback> PostStepCallbacks { get; } = new();

		public Emu()
        {
            cpu = new Cpu();
            ppu = new Ppu();
            apu = new Apu();
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

		public bool Tick( State state, CancellationToken token = default )
		{
            bool cont;
			try
			{
				cont = cpu.Tick( state.reg, state.mem );
				ppu.Tick( state.reg, state.mem );
				apu.Tick( state.reg, state.mem );
			}
			catch( rzr.ExecException e )
			{
				// TODO: handle
				Debug.Write( e.Message );
                cont = false;
                throw new OperationCanceledException( message: e.Message, innerException: e, token );
			}

			return cont;
        }

        /// <summary>
        /// execute one complete instruction
        /// </summary>
        /// <returns>number of M-cycles the current instruction took with overlapped fetch</returns>
        public uint Step( State state, bool debugPrint, CancellationToken token = default )
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
                ushort pc = cpu.prevInstrPC;
                Debug.WriteLine( $"{Isa.Disassemble( ref pc, state.mem )} {cycles}:{cpu.prevInstrCycles} cycles|fetch" );
            }

            foreach ( Callback fun in PostStepCallbacks )
            {
                fun( state );
            }

            return cycles;
        }
    }
}