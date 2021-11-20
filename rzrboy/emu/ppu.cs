namespace emu
{
    public class ppu : IProcessingUnit
    {
        private mem mem;
        public ppu(mem memory)
        {
            mem = memory;
        }

        public void Tick()
        {
        }
    }
}
