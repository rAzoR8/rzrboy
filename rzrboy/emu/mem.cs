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
		public enum HWMode
		{
			DMG,
			GBC
		}

		public HWMode Mode { get; set; } = HWMode.DMG;

        public const ushort RomBankSize = 0x4000; // 16KiB
        public const ushort VRamSize = 0x2000; // 8KiB
        public const ushort ERamSize = 0x2000; // 8KiB
        public const ushort WRamSize = 0x1000; // 4KiB
        public const ushort EchoRamSize = 0xFE00 - 0xE000; //7680B
        public const ushort OAMSize = 0xFEA0 - 0xFE00; // 160B
        public const ushort UnusedSize = 0xFF00 - 0xFEA0; // 60B
        public const ushort IOSize = 0xFF80 - 0xFF00;// 128B
        public const ushort HRamSize = 0xFFFF - 0xFF80; // 127B

		public bool Booting => io[0xFF50] == 0;

		public Section boot { get; set; } = Boot.Minimal; // 0x0000

		public Mbc			mbc { get; set; } = new(); // including eram (external)
		public Section		vram { get; set; } = new( 0x8000, 0x2000, "vram", SectionAccess.ReadWrite ); // In CGB mode, switchable bank 0/1        
		public WRam			wram { get; set; } = new(); // C000
		public RemapSection echo => new( (address) => (ushort)( address - 0x2000 ), start: 0xE000, len: EchoRamSize, src: wram );
		public Section		oam { get; set; } = new( 0xFE00, OAMSize, "oam", SectionAccess.ReadWrite );
		public Section		unused { get; set; } = new( 0xFEA0, UnusedSize, "unused", SectionAccess.None ); // for arbitrary roms it might be necessary to allow ReadWrite access
		public IOSection	io { get; set; } //  new( 0xFF00, IOSize, "io", SectionAccess.ReadWrite );
		public Section		hram { get; set; } = new( 0xFF80, HRamSize, "ram", SectionAccess.ReadWrite);
		public ByteSection	IE { get; set; } = new( 0xFFFF, val: 0, name: "IE" );

		// ISection
		public ushort StartAddr => 0;
		public ushort Length => 0xFFFF;

		public List<OnRead> ReadCallbacks { get; } = new();
		public List<OnWrite> WriteCallbacks { get; } = new();

		private ISection GetSection( ushort address ) 
		{
			switch( address )
			{
				case >= 0x0000 and < 0x8000: return Booting && ((ISection)boot).Accepts(address) ? boot : mbc; // 0000-7FFF 32KiB switchable
				case >= 0x8000 and < 0xA000: return vram;		// 8000-9FFF 8KiB
				case >= 0xA000 and < 0xC000: return mbc;		// A000-BFFF 8KiB external ram on cartridge
				case >= 0xC000 and < 0xE000: return wram;       // C000-E000 4KiB + 4KiB banked
				case >= 0xE000 and < 0xFE00: return echo;		// E000-FE00 7680B
				case >= 0xFE00 and < 0xFEA0: return oam;		// FE00-FEA0 160B
				case >= 0xFEA0 and < 0xFF00: return unused;		// FEA0-FEFF 60B Not Usable
				case >= 0xFF00 and < 0xFF80: return io;			// FF00-FF80 128B
				case >= 0xFF80 and < 0xFFFF: return hram;		// FF80-FFFF 127B
				case 0xFFFF: return IE;							// 0xFFFF	 1B
				default: throw new AddressNotMappedException( address );
			}
		}

		public byte this[ushort address]
        {
			get
			{
				var section = GetSection( address );
				foreach( OnRead onRead in ReadCallbacks )
				{
					onRead( section, address );
				}
				return section[address];
			}

			set
            {
				var section = GetSection( address );
				section[address] = value;
				foreach( OnWrite onWrite in WriteCallbacks )
				{
					onWrite( section, address, value );
				}
			}
        }

		public Mem( )
		{
			io = new( this );
        }
    }
}
