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

        public delegate void Callback( Reg reg, Mem mem );

        public List<Callback> StepCallbacks { get; } = new();

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
                        cycles += Step( false );
                    }
                } );
            }
            catch( OperationCanceledException )
            {
                IsRunning = false;
            }

            return cycles;
        }

        public bool Tick() 
        {
            bool cont = cpu.Tick( reg, mem );
			ppu.Tick( reg, mem );
			apu.Tick( reg, mem );

			return cont;
        }

        /// <summary>
        /// execute one complete instruction
        /// </summary>
        /// <returns>number of M-cycles the current instruction took with overlapped fetch</returns>
        public uint Step( bool debugPrint )
        {
            uint cycles = 1;

            // execute all ops
            while ( Tick() ) { ++cycles; }

            if ( debugPrint )
            {
                ushort pc = cpu.prevInstrPC;
                Debug.WriteLine( $"{Isa.Disassemble( ref pc, mem )} {cycles}:{cpu.prevInstrCycles} cycles|fetch" );
            }

            foreach ( Callback fun in StepCallbacks )
            {
                fun( reg, mem );
            }

            return cycles;
        }
    }
}