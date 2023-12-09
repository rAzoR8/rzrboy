using System.Diagnostics;
using System.Linq;
using System.Text;

namespace rzr
{
    public enum HeaderOffsets : ushort
    {
        EntryPointStart = 0x100, //4b trampoline to jump to the game code
        EntryPointEnd = 0x103,
        LogoStart = 0x104,
        LogoEnd = 0x133, // inclusive
        TitleStart = 0x134,
        TitleEnd = 0x143, // inclusive           
        ManufacturerStart = 0x13F,// overlaps Title
        ManufacturerEnd = 0x142, // inclusuvie
        CGBFlag = 0x143, // overlaps title
        NewLicenseeCodeStart = 0x144, // only used if OldLicenseeCode == 0x33
		NewLicenseeCodeEnd = 0x145,
        SGBFlag = 0x146,
        Type = 0x147, // MBC type
        RomBanks = 0x148,
        RamBanks = 0x149,
        DestinationCode = 0x14A, // 0 = japanese
        OldLicenseeCode = 0x14B,
        Version = 0x14C, // game version
        HeaderChecksum = 0x14D, // 0x134-14C
        RomChecksumStart = 0x14E,
        RomChecksumEnd = 0x14F,

        HeaderEnd = RomChecksumEnd,
        HeaderSize = HeaderEnd + 1,

        EntryPointLength = EntryPointEnd + 1 - EntryPointStart, // 4b
        LogoLength = LogoEnd + 1 - LogoStart, // 48 byte
        TitleLength = TitleEnd + 1 - TitleStart, // 16
        ManufacturerLength = ManufacturerEnd + 1 - ManufacturerStart, // 4b
    }

    public class HeaderView
	{
        private IList<byte> m_data;

        public HeaderView( IList<byte> data )
        {
            m_data = data;
        }

        public byte HeaderChecksum
        {
            get => m_data[(ushort)HeaderOffsets.HeaderChecksum];
            set => m_data[(ushort)HeaderOffsets.HeaderChecksum] = value;
        }

        public ushort RomChecksum
        {
            get => Binutil.Combine( msb: m_data[(ushort)HeaderOffsets.RomChecksumStart], lsb: m_data[(ushort)HeaderOffsets.RomChecksumEnd] );
            set
            {
                m_data[(ushort)HeaderOffsets.RomChecksumStart] = value.GetMsb();
                m_data[(ushort)HeaderOffsets.RomChecksumEnd] = value.GetLsb();
            }
        }

		public ushort NewLicenseeCode
		{
			get => Binutil.Combine( msb: m_data[(ushort)HeaderOffsets.NewLicenseeCodeStart], lsb: m_data[(ushort)HeaderOffsets.NewLicenseeCodeEnd] );
			set
			{
				m_data[(ushort)HeaderOffsets.NewLicenseeCodeStart] = value.GetMsb();
				m_data[(ushort)HeaderOffsets.NewLicenseeCodeEnd] = value.GetLsb();
			}
		}

		public byte OldLicenseeCode
		{
			get => m_data[(ushort)HeaderOffsets.OldLicenseeCode];
			set => m_data[(ushort)HeaderOffsets.OldLicenseeCode] = value;
		}

        public enum CGBFlag : byte
        {
            PGB = 0b0100_1001, // Values with bit 7 and either bit 2 or 3 set will switch the Game Boy into a special non-CGB-mode called “PGB mode”.
		    CGBMonochrome = 0x80,
            CGBOnly = 0xC0
        }

		public byte CGBSupport
		{
			get => m_data[(ushort)HeaderOffsets.CGBFlag];
			set => m_data[(ushort)HeaderOffsets.CGBFlag] = value;
		}

		public enum DestinationCode : byte
        {
            Japan = 0,
            NotJapan = 1
        }

        public bool Japan
        {
            get => (byte)DestinationCode.Japan == m_data[(ushort)HeaderOffsets.DestinationCode];
            set => m_data[(ushort)( HeaderOffsets.DestinationCode )] = (byte)( value ? DestinationCode.Japan : DestinationCode.NotJapan );
        }

        public byte Version { get => m_data[(ushort)HeaderOffsets.Version]; set => m_data[(ushort)HeaderOffsets.Version] = value; }

        public enum SGBFlag
        {
            None = 0,
            SGBSupport = 0x3
        }

        public bool SGBSupport
        {
            get => (byte)SGBFlag.SGBSupport == m_data[(ushort)HeaderOffsets.DestinationCode];
            set => m_data[(ushort)( HeaderOffsets.SGBFlag )] = (byte)( value ? SGBFlag.SGBSupport : SGBFlag.None );
        }

        public CartridgeType Type { get => (CartridgeType)m_data[(ushort)HeaderOffsets.Type]; set => m_data[(ushort)HeaderOffsets.Type] = (byte)value; }

		public IEnumerable<byte> Logo
        {
            get
            {
                return m_data.Skip( (int) HeaderOffsets.LogoStart ).Take( (int)HeaderOffsets.LogoLength );
            }
            set
            {
                if( value.Count() != (int)HeaderOffsets.LogoLength )
                    throw new ArgumentException( $"Logo must be exaclty {(int)HeaderOffsets.LogoLength} bytes long" );

				for( int i = 0; i < (int)HeaderOffsets.LogoLength; i++ )
				{
                    m_data[(int)HeaderOffsets.LogoStart + i] = value.ElementAt(i);
                }
            }
        }

        public string Title
        {
            get => GetHeaderString( HeaderOffsets.TitleStart, HeaderOffsets.TitleEnd );
            set => SetHeaderString( HeaderOffsets.TitleStart, HeaderOffsets.TitleLength, value );
        }

        public string Manufacturer
        {
            get => GetHeaderString( HeaderOffsets.ManufacturerStart, HeaderOffsets.ManufacturerEnd );
            set => SetHeaderString( HeaderOffsets.ManufacturerStart, HeaderOffsets.ManufacturerLength, value );
        }

        public int RomBanks
        {
            get => ( 2 << m_data[(ushort)HeaderOffsets.RomBanks] );
            set => m_data[(ushort)HeaderOffsets.RomBanks] = (byte)( value >> 2 );
        }

        public static readonly byte[] RamBankSizes = { 0, 1, 4, 8, 16 };
        public int RamBanks
        {
            get
            {
                byte banks = m_data[(ushort)HeaderOffsets.RamBanks];
                switch( banks )
                {
                    case 0: return 0;
                    case 1: return 0; // unused
                    case 2: return 1;
                    case 3: return 4;
                    case 4: return 16;
                    case 5: return 8;
                    default: throw new ArgumentOutOfRangeException( "Unknown ram bank specifier" );
                }
            }
            set
            {
				switch( value )
				{
                    case 0:
                        m_data[(ushort)HeaderOffsets.RamBanks] = 0;
                        break;
                    case 1:
                        m_data[(ushort)HeaderOffsets.RamBanks] = 2;
                        break;
                    case 4:
                        m_data[(ushort)HeaderOffsets.RamBanks] = 3;
                        break;
                    case 16:
                        m_data[(ushort)HeaderOffsets.RamBanks] = 4;
                        break;
                    case 8:
                        m_data[(ushort)HeaderOffsets.RamBanks] = 5;
                        break;
                    default: throw new ArgumentOutOfRangeException( "Unknown invalid ram bank count, must be [0, 1, 4, 8, 16]" );
                }
            }
        }

		public bool Valid()
		{
			Debug.WriteLine( $"Loaded cartridge {Title} v{Version} [{m_data.Count()}B] Type: {Type} {RomBanks} Rom & {RamBanks} Ram Banks" );
			Debug.WriteLine( $"Manufactuer: {Manufacturer} Destination: {( Japan ? "Japan" : "Other" )} SGB support: {SGBSupport} CGB flag: {(CGBFlag)CGBSupport}" );

			var hCheck = ComputeHeaderChecksum( m_data );
			var rCheck = ComputeRomChecksum( m_data );

			Debug.WriteLine( $"Computed\tHeader|Rom checksum {hCheck:X2}|{rCheck:X4}" );
			Debug.WriteLine( $"Stored\tHeader|Rom checksum {HeaderChecksum:X2}|{RomChecksum:X4}" );

			return hCheck == HeaderChecksum && rCheck == RomChecksum;
		}

		private string GetHeaderString( HeaderOffsets start, HeaderOffsets end )
		{
			StringBuilder sb = new();

			for( var i = start; i <= end; i++ )
			{
				char c = (char)m_data[(ushort)i];
				if( c == 0 ) break;
				sb.Append( c );
			}

			return sb.ToString();
		}

		private void SetHeaderString( HeaderOffsets start, HeaderOffsets len, string str )
		{
			var strLen = Math.Min( (int)len, str.Length );
			for( int i = 0; i < strLen; i++ )
			{
				m_data[(int)start + i] = (byte)str[i];
			}
			if(strLen<(int)len)
				m_data[(int)start + strLen] = 0;
		}

		/// <summary>
		/// uint8_t checksum = 0;
		/// for (uint16_t address = 0x0134; address <= 0x014C; address++) {
		///     checksum = checksum - rom[address] - 1;
		/// }
		/// </summary>
		/// <param name="header"></param>
		/// <param name="start"></param>
		/// <returns></returns>

		public static byte ComputeHeaderChecksum( IEnumerable<byte> header, int start = (int)HeaderOffsets.TitleStart )
		{
			int end = start + 25; // 0x19 length of header to validate
			byte checksum = 0;
			for( int i = start; i < end; i++ )
			{
				checksum -= header.ElementAt( i );
				checksum--;
			}
			return checksum;
		}

		/// <summary>
		/// Contains a 16 bit checksum (upper byte first) across the whole cartridge ROM.
		/// Produced by adding all bytes of the cartridge (except for the two checksum bytes).
		/// The Game Boy doesn’t verify this checksum.
		/// </summary>
		/// <param name="banks"></param>
		/// <returns>Checksum lsb first, needs to be byte swapped!</returns>
		public static ushort ComputeRomChecksum( IEnumerable<byte> banks, bool subtractRomCheck = true )
		{
			ushort checksum = 0;

			foreach( byte b in banks )
			{
				checksum += b;
			}

			if (subtractRomCheck)
			{
				checksum -= banks.ElementAt((int)HeaderOffsets.RomChecksumStart);
				checksum -= banks.ElementAt((int)HeaderOffsets.RomChecksumEnd);
			}

			return checksum;
		}
	}
}
