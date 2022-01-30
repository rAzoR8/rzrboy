﻿using System.Diagnostics;
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
            cart = new Cartridge();

            reg = new Reg();
			mem = new Mem( cart );
			isa = new Isa();

			cpu = new Cpu( reg, mem, isa );
			ppu = new Ppu( mem );
			apu = new Apu( mem );
		}

		public Boy(byte[] cart, byte[]? boot)  : this()
        {
            LoadCart( cart, boot );
        }

        public Boy( string cartPath, string bootRomPath ) : this()
        {
            byte[] data;
            byte[]? boot = null;
            try
            {
                data = File.ReadAllBytes( cartPath );

				if( bootRomPath != null )
				{
					boot = File.ReadAllBytes( bootRomPath );
				}
			}
            catch ( Exception )
            {
                data = new byte[0x8000];
            }

            LoadCart( data, boot );
        }

        public bool LoadCart( byte[] cartData, byte[]? boot )
        {
            BootRom ?brom = null;

            if( boot != null )
            {
                if( boot.Length == 0x100 ) // dmg
                {
                    brom = new BootRom( mem.io, boot, (0, 0x100) );
                }
                else if (boot.Length == 0x800 ) // cgb
                {
                    brom = new BootRom( mem.io, boot, (0, 0x100), (0x200, 0x900) );
				}
				else // unknown,map everything and hope for the best
                {
                    brom = new BootRom( mem.io, boot, (0, (ushort)boot.Length) );
                }
            }

            return cart.Load( cartData, brom );
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