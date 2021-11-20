namespace emu
{
    public class AddressNotMappedException : System.AccessViolationException 
    {
        public AddressNotMappedException(ushort address) : base($"0x{address.ToString("X4")} not mapped to any memory section") { }
    }

    public class mem : ListSection
    {
        public const ushort BankSize = 0x4000; // 16k

        // nonswitchable
        private RSection rom0 = null;
        // switchable
        private ProxySection romX = new();

        private List<RSection> rombanks = new();

        // todo: switchable
        private ProxySection vram = new(new RWSection(0x8000, 0xA000));

        // todo: switchable
        private ProxySection eram = new (new RWSection(0xA000, 0xC000));

        private RWSection wram = new RWSection(0xC000, 0xD000);

        private RemapSection echo = new((ushort address) => (ushort)(address - 4096), 0xE000, 0xFDFF);

        public mem()
        {
            Add(rom0);
            Add(romX);
            Add(vram);
            Add(eram);
            Add(wram);
            Add(echo);
        }
    }
}
