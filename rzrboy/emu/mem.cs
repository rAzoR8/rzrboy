namespace rzr
{
	public class AddressNotMappedException : rzr.ExecException
	{
		public AddressNotMappedException( ushort address ) :
			base( $"0x{address.ToString( "X4" )} not mapped to any memory section" )
		{ }
	}

	public class Mem : ISection
	{
        public const ushort RomBankSize = 0x4000; // 16KiB
        public const ushort VRamSize = 0x2000; // 8KiB
        public const ushort ERamSize = 0x2000; // 8KiB
        public const ushort WRamSize = 0x1000; // 4KiB
        public const ushort EchoRamSize = 0xFE00 - 0xE000; //7680B
        public const ushort OAMSize = 0xFEA0 - 0xFE00; // 160B
        public const ushort UnusedSize = 0xFF00 - 0xFEA0; // 60B
        public const ushort IOSize = 0xFF80 - 0xFF00;// 128B
        public const ushort HRamSize = 0xFFFF - 0xFF80; // 127B

        // TODO: have some minimal boot rom
        public Section boot { get; set; } = new( 0x0000, 0xFF, "nopboot" ); // 0x0000

        public Mbc     cart { get; set; } = new Mbc(); // including eram (external)
        public Section vram { get; set; } = new (0x8000, 0x2000, "vram"); // In CGB mode, switchable bank 0/1        
        public Section wram0 { get; set; } = new (0xC000, 0x1000, "wram0");        
        public Section wramx { get; set; } = new (0xD000, 0x1000, "wramx"); //In CGB mode, switchable bank 1-7

        public RemapSection echo { get; set; }
        public Section oam { get; set; } = new(0xFE00, OAMSize, "oam");
        public Section unused { get; set; } = new(0xFEA0, UnusedSize, "unused");
        public Section io { get; set; } = new(0xFF00, IOSize, "io");
        public Section hram { get; set; } = new(0xFF80, HRamSize, "ram");
        public ByteSection IE { get; set; } = new(0xFFFF, val: 0, name: "IE");

		// TODO: remove wram0 and wramx and just have on wram section, and switching happens inside
		public CombiSection wram => new( wram0, wramx );

		public ushort StartAddr => 0;
		public ushort Length => 0xFFFF;

        private byte BootOrCart( ushort address ) => io[0xFF50] != 0 ? cart[address] : boot[address];

		public byte this[ushort address]
        {
            get
            {
                switch( address )
                {
                    case >= 0x0000 and < 0x8000: return BootOrCart(address);    // 0000-7FFF 32KiB switchable
					case >= 0x8000 and < 0xA000: return vram[address];          // 8000-9FFF 8KiB
					case >= 0xA000 and < 0xC000: return cart[address];          // A000-BFFF 8KiB external ram on cartridge
					case >= 0xC000 and < 0xD000: return wram0[address];         // C000-CFFF 4KiB
					case >= 0xD000 and < 0xE000: return wramx[address];         // D000-DFFF 4KiB
					case >= 0xE000 and < 0xFE00: return echo[address];          // E000-FE00 7680B
					case >= 0xFE00 and < 0xFEA0: return oam[address];           // FE00-FEA0 160B
					case >= 0xFEA0 and < 0xFF00: return unused[address];        // FEA0-FEFF 60B Not Usable
					case >= 0xFF00 and < 0xFF80: return io[address];            // FF00-FF80 128B
					case >= 0xFF80 and < 0xFFFF: return hram[address];          // FF80-FFFF 127B
					case 0xFFFF: return IE[address];                            // 0xFFFF
					default: throw new AddressNotMappedException( address );
                }
            }

            set
            {
				switch( address )
				{
					case >= 0x0000 and < 0x8000: cart[address] = value; break;      // 0000-7FFF 32KiB switchable
					case >= 0x8000 and < 0xA000: vram[address] = value; break;      // 8000-9FFF 8KiB
					case >= 0xA000 and < 0xC000: cart[address] = value; break;      // A000-BFFF 8KiB external ram on cartridge
					case >= 0xC000 and < 0xD000: wram0[address] = value; break;     // C000-CFFF 4KiB
					case >= 0xD000 and < 0xE000: wramx[address] = value; break;     // D000-DFFF 4KiB
					case >= 0xE000 and < 0xFE00: echo[address] = value; break;      // E000-FE00 7680B
					case >= 0xFE00 and < 0xFEA0: oam[address] = value; break;       // FE00-FEA0 160B
					case >= 0xFEA0 and < 0xFF00: unused[address] = value; break;	// FEA0-FEFF 60B Not Usable
					case >= 0xFF00 and < 0xFF80: io[address] = value; break;		// FF00-FF80 128B
					case >= 0xFF80 and < 0xFFFF: hram[address] = value; break;      // FF80-FFFF 127B
					case 0xFFFF: IE[address] = value; break;						// 0xFFFF
					default: throw new AddressNotMappedException( address );
				}
			}
        }

		public Mem( )
		{
			// todo: remove echo section, do remapping in the switch
			echo = new( ( ushort address ) => (ushort)( address - 0x2000 ), 0xE000, EchoRamSize, src: wram );
        }
    }
}
