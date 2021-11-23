namespace emu
{
    public class ppu : IProcessingUnit
    {
        private Mem mem;
        public ppu(Mem memory)
        {
            mem = memory;
        }

        public void Tick()
        {
        }
    }
}
