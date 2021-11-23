namespace emu
{
    public class apu : IProcessingUnit
    {
        private Mem mem;
        public apu(Mem memory)
        {
            mem = memory;
        }

        public void Tick()
        {
        }
    }
}
