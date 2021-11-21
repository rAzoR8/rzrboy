using System.Collections;

namespace emu
{
    public class AddressNotMappedException : System.AccessViolationException 
    {
        public AddressNotMappedException(ushort address) : base($"0x{address.ToString("X4")} not mapped to any memory section") { }
    }

    public class mem : ListSection, IEnumerable<byte>
    {
        public const ushort BankSize = 0x4000; // 16k

        // nonswitchable
        private RSection rom = new (0, 0x4000);
        // switchable
        private ProxySection romx = new(new RSection(0x4000, 0x8000));

        // In CGB mode, switchable bank 0/1
        private ProxySection vram = new(new RWSection(0x8000, 0xA000));

        // From cartridge, switchable bank if any
        private ProxySection eram = new (new RWSection(0xA000, 0xC000));

        private RWSection wram = new (0xC000, 0xD000);

        //In CGB mode, switchable bank 1-7
        private ProxySection wramx = new (new RWSection(0xD000, 0xE000));

        private RemapSection echo = new((ushort address) => (ushort)(address - 4096), 0xE000, 0xFE00);

        private RWSection oam = new(0xFE00, 0xFEA0);

        private RWSection io = new(0xFF00, 0xFF80);

        private RWSection hram = new(0xFF80, 0xFFFF);

        private ByteSection IE = new(0xFFFF, 0);
        public mem()
        {
            Add(rom);  // 0000-3FFF 16KiB
            Add(romx); // 4000-7FFF 16KiB
            Add(vram); // 8000-9FFF 8KiB
            Add(eram); // A000-BFFF 8KiB
            Add(wram); // C000-CFFF 4KiB
            Add(wramx);// D000-DFFF 4KiB
            Add(echo); // E000-FE00 7679B
            Add(oam);  // FE00-FEA0 160B
            // FEA0-FEFF Not Usable
            Add(io);   // FF00-FF80 128B
            Add(hram); // FF80-FFFF 127B
            Add(IE);   // FFFF      1B
        }

        public IEnumerator<byte> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void write(byte[] src, ushort address, ushort len = 0) 
        {
            len = len != 0 ? Math.Min(len, (ushort)src.Length) : (ushort)src.Length;

            for (ushort i = 0; i < len; i++)
            {
                this[(ushort)(address+i)] = src[i];
            }
        }
    }
}
