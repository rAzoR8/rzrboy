using System.Diagnostics;

namespace rzr
{
    public class Boy
    {
		public Mem mem { get; }
		public Ppu ppu { get; }
		public Cpu cpu { get; }
        public Reg reg { get; }
        public Apu apu { get; }
		public Cartridge cart { get; }

		public bool IsRunning { get; private set; }
        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public delegate void Callback( Reg reg, ISection mem );

		public List<Callback> PreStepCallbacks { get; } = new();
		public List<Callback> PostStepCallbacks { get; } = new();

		public Boy()
        {
            cart = new Cartridge();
            mem = new Mem(); // TODO: move outside
            reg = new Reg();

            cpu = new Cpu();
            ppu = new Ppu();
            apu = new Apu();
        }

        public void LoadBootRom( byte[] boot )
        {
            mem.boot = new Section( start: 0x0000, len: (ushort)boot.Length, "bootrom", boot );
        }

        public void LoadRom( byte[] rom )
        {
            cart.Load( rom );
            mem.cart = cart.Mbc;
        }

        public async Task<ulong> Execute( CancellationToken token = default )
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
                        cycles += Step( token: token, debugPrint: true );
                    }
                } );
            }
            catch( OperationCanceledException e )
            {
                if(e.CancellationToken == token)
                    IsRunning = false;
                else
                    return await Execute( token );
            }

            return cycles;
        }

		public bool Tick( CancellationToken token = default )
		{
            bool cont;
			try
			{
				cont = cpu.Tick( reg, mem );
				ppu.Tick( reg, mem );
				apu.Tick( reg, mem );
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
        public uint Step( bool debugPrint, CancellationToken token = default )
        {
            uint cycles = 1;

			foreach( Callback fun in PreStepCallbacks )
			{
				fun( reg, mem );
			}

			// execute all ops
			while ( Tick( token ) ) { ++cycles; }

            if ( debugPrint )
            {
                ushort pc = cpu.prevInstrPC;
                Debug.WriteLine( $"{Isa.Disassemble( ref pc, mem )} {cycles}:{cpu.prevInstrCycles} cycles|fetch" );
            }

            foreach ( Callback fun in PostStepCallbacks )
            {
                fun( reg, mem );
            }

            return cycles;
        }
    }
}