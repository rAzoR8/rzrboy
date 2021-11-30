namespace emu
{
    public class gb
    {
        private Mem mem;
        private ppu ppu;
        private Cpu cpu;
        private apu apu;
        private Cartridge cart;

        private ulong cycle = 0u;
        private bool run = true;

        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public gb(byte[] cart)
        {
            Reset( cart );
        }

        public gb( string cartPath )
        {
            Reset( File.ReadAllBytes(cartPath) );
        }

        public void Reset( byte[] cartData )
        {
            run = false;

            cycle = 0;

            mem = new();

            cpu = new Cpu( mem );
            ppu = new ppu( mem );
            apu = new apu( mem );

            cart = new( mem.rom, mem.eram, mem.io, cartData );

            run = true;
        }

        public IEnumerable<ulong> Run() 
        {
            while (run)
            {
                // TODO: handle interupts

                cpu.Tick();
                ppu.Tick();
                apu.Tick();

                yield return cycle;

                ++cycle;
            }
        }
    }
}