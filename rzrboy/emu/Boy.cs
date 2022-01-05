using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace rzr
{
    public class Boy
    {
		public Isa isa { get; }
		public Mem mem { get; }
		public Ppu ppu { get; }
		public Cpu cpu { get; }
        public Reg reg { get; }
        public Apu apu { get; }
		public Cartridge cart { get; }

		public bool IsRunning { get; private set; }
        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public delegate bool Callback( Reg reg, Mem mem );

        public List<Callback> StepCallbacks { get; } = new();

		public Boy()
		{
			reg = new Reg();
			mem = new Mem();
			isa = new Isa();

			cpu = new Cpu( reg, mem, isa );
			ppu = new Ppu( mem );
			apu = new Apu( mem );

			cart = new( mem.rom, mem.eram, mem.io );
		}

		public Boy(byte[] cart)  : this()
        {
            LoadCart( cart );
        }

        public Boy( string cartPath ) : this()
        {
            byte[] data;
            try
            {
                data = File.ReadAllBytes( cartPath );
            }
            catch ( Exception )
            {
                data = new byte[0x8000];
            }

            LoadCart( data );
        }

        public bool LoadCart( byte[] cartData )
        {
            return cart.Load( cartData );
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
            // TODO: handle interupts

            bool cont = cpu.Tick();
            ppu.Tick();
            apu.Tick();

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
                Debug.WriteLine( $"{isa.Disassemble( ref pc, mem )} {cycles}:{cpu.prevInstrCycles} cycles|fetch" );
            }

            foreach ( Callback fun in StepCallbacks )
            {
                fun( reg, mem );
            }

            return cycles;
        }
    }
}