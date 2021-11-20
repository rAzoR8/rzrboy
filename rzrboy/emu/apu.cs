namespace emu
{
    public class apu : IProcessingUnit
    {
        private mem mem;
        public apu(mem memory)
        {
            mem = memory;
        }

        public void Tick()
        {
        }
    }
}
