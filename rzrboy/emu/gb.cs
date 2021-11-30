﻿namespace emu
{
    public class Gb
    {
        public Mem mem { get; private set; }
        public Ppu ppu{ get; private set; }
        public Cpu cpu{ get; private set; }
        public Apu apu { get; private set; }
        public Cartridge cart { get; private set; }

        private ulong cycle = 0u;
        private bool run = true;

        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public Gb(byte[] cart)
        {
            Reset( cart );
        }

        public Gb( string cartPath )
        {
            Reset( File.ReadAllBytes(cartPath) );
        }

        public void Reset( byte[] cartData )
        {
            run = false;

            cycle = 0;

            mem = new();

            cpu = new Cpu( mem );
            ppu = new Ppu( mem );
            apu = new Apu( mem );

            cart = new( mem.rom, mem.eram, mem.io, cartData );

            run = true;
        }

        public IEnumerable<ulong> Run() 
        {
            while (run)
            {
                Tick();

                yield return cycle;

                ++cycle;
            }
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
        /// <returns>number of M-cycles the current instruction took</returns>
        public int Step()
        {
            int cycles = 0;
            // execute all ops
            while ( Tick() ) { ++cycles; }
            return cycles;
        }
    }
}