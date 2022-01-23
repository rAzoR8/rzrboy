using System.Diagnostics;

namespace rzr
{
    public class AddressNotMappedException : System.AccessViolationException 
    {
        public AddressNotMappedException(ushort address) : base($"0x{address.ToString("X4")} not mapped to any memory section") { }
    }

    public class Mem : ListSection
    {
        public const ushort RomBankSize = 0x4000; // 16KIB
        public const ushort VRamSize = 0x2000; // 8KiB
        public const ushort ERamSize = 0x2000; // 8KiB
        public const ushort WRamSize = 0x1000; // 4KiB
        public const ushort EchoRamSize = 0xFE00 - 0xE000; //7680B
        public const ushort OAMSize = 0xFEA0 - 0xFE00; // 160B
        public const ushort UnusedSize = 0xFF00 - 0xFEA0; // 60B
        public const ushort IOSize = 0xFF80 - 0xFF00;// 128B
        public const ushort HRamSize = 0xFFFF - 0xFF80; // 127B
        
        public ProxySection rom { get; } = new(new Section(0, RomBankSize * 2, "rom") ); // nonswitchable        
        public Section vram { get; } = new Section(0x8000, 0x2000, "vram"); // In CGB mode, switchable bank 0/1        
        public ProxySection eram { get; } = new(new Section(0xA000, 0x2000, "eram")); // From cartridge, switchable bank if any
        public Section wram0 { get; } = new (0xC000, 0x1000, "wram0");        
        public Section wramx { get; } = new Section(0xD000, 0x1000, "wramx"); //In CGB mode, switchable bank 1-7
        public RemapSection echo { get; } = new((ushort address) => (ushort)(address - 0x2000), 0xE000, EchoRamSize);
        public Section oam { get; } = new(0xFE00, OAMSize, "oam");
        public Section unused { get; } = new(0xFEA0, UnusedSize, "unused");
        public Section io { get; } = new(0xFF00, IOSize, "io");
        public Section hram { get; } = new(0xFF80, HRamSize, "ram");
        public ByteSection IE { get; } = new(0xFFFF, val: 0, name: "IE");

        // helper sections:
        public CombiSection wram { get; }

        public Mem()
        {
            Add(rom);   // 0000-7FFF 32KiB switchable
            Add(vram);   // 8000-9FFF 8KiB
            Add(eram);   // A000-BFFF 8KiB
            Add(wram0);  // C000-CFFF 4KiB
            Add(wramx);  // D000-DFFF 4KiB
            Add(echo);   // E000-FE00 7680B
            Add(oam);    // FE00-FEA0 160B
            Add(unused); // FEA0-FEFF 60B Not Usable
            Add(io);     // FF00-FF80 128B
            Add(hram);   // FF80-FFFF 127B

            Debug.Assert(Length == 0xFFFF);
            Add(IE);     // FFFF      1B

            wram = new(wram0, wramx);

            echo.Source = wram;
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
