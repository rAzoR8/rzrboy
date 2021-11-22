namespace emu
{
    public class gb
    {
        private mem mem;
        private ppu ppu;
        private cpu cpu;
        private apu apu;

        private ulong cycle = 0u;
        private bool run = true;

        public uint Speed { get; set; } = 1;
        public uint MCyclesPerSec => 1048576u * Speed;

        public gb()
        {
            mem = new();
            cpu = new cpu(mem);
            ppu = new ppu(mem);
            apu = new apu(mem);
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