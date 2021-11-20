namespace emu
{
    public class cpu : IProcessingUnit
    {
        private reg reg = reg.DMG();
        private mem mem = null;

        public cpu(mem memory) 
        {
            mem = memory;
        }

        public void Tick()
        {
        }
    }
}
