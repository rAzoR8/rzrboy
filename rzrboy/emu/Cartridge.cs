using System.Diagnostics;
using System.Text;

namespace rzr
{
    public enum CartridgeType : byte
    {
        ROM_ONLY = 0x00,

        MBC1 = 0x01,
        MBC1_RAM = 0x02,
        MBC1_RAM_BATTERY = 0x03,

        MBC2 = 0x05,
        MBC2_BATTERY = 0x06,

        ROM_RAM = 0x08,
        ROM_RAM_BATTERY = 0x09,

        MMM01 = 0x0B,
        MMM01_RAM = 0x0C,
        MMM01_RAM_BATTERY = 0x0D,

        MBC3_TIMER_BATTERY = 0x0F,
        MBC3_TIMER_RAM_BATTERY = 0x10,
        MBC3 = 0x11,
        MBC3_RAM = 0x12,
        MBC3_RAM_BATTERY = 0x13,

        MBC5 = 0x19,
        MBC5_RAM = 0x1A,
        MBC5_RAM_BATTERY = 0x1B,
        MBC5_RUMBLE = 0x1C,
        MBC5_RUMBLE_RAM = 0x1D,
        MBC5_RUMBLE_RAM_BATTERY = 0x1E,

        MBC6 = 0x20,
        MBC7_SENSOR_RUMBLE_RAM_BATTERY = 0x22,
        POCKET_CAMERA = 0xFC,
        BANDAI_TAMA5 = 0xFD,
        HuC3 = 0xFE,
        HuC1_RAM_BATTERY = 0xFF
    }

    public class Cartridge
    {
        public Mbc Mbc { get; private set; }

        public enum Header : ushort
        {
            EntryPoint = 0x100,
            LogoStart = 0x104,
            LogoEnd = 0x133, // inclusive
            TitleStart = 0x134,
            TitleEnd = 0x143, // inclusive           
            ManufacturerStart = 0x13F,
            ManufacturerEnd = 0x142, // inclusuvie
            CGBFlag = 0x143,
            NewLicenseeCodeStart = 0x144,
            NewLicenseeCodeEnd = 0x145,
            SGBFlag = 0x146,
            Type = 0x147,
            RomBanks = 0x148,
            RamBanks = 0x149,
            DestinationCode = 0x14A, // 0 = japanese
            OldLicenseeCode = 0x14B,
            Version = 0x14C, // game version
            HeaderChecksum = 0x14D, // 0x134-14C
            RomChecksumStart = 0x14E,
            RomChecksumEnd = 0x14F,

            HeaderEnd = RomChecksumEnd,

            LogoLength = LogoEnd + 1 - LogoStart,
            TitleLength = TitleEnd + 1 - TitleStart,
            ManufacturerLength = ManufacturerEnd + 1 - ManufacturerStart,
        }

        public byte HeaderChecksum
        {
            get => Mbc[(ushort)Header.HeaderChecksum];
            set => Mbc[(ushort)Header.HeaderChecksum] = value;
        }
        public ushort RomChecksum
        {
            get => binutil.Combine( msb: Mbc[(ushort)Header.RomChecksumStart], lsb: Mbc[(ushort)Header.RomChecksumEnd] );
            set
            {
                Mbc[(ushort)Header.RomChecksumStart] = value.GetMsb();
                Mbc[(ushort)Header.RomChecksumEnd] = value.GetLsb();
            }
        }

        public enum DestinationCode : byte 
        {
            Japan = 0,
            NotJapan = 1
        }

        public bool Japan
        {
            get => (byte)DestinationCode.Japan == Mbc[(ushort)Header.DestinationCode];
            set => Mbc[(ushort)( Header.DestinationCode )] = (byte)( value ? DestinationCode.Japan : DestinationCode.NotJapan );
        }

        public byte Version { get => Mbc[(ushort)Header.Version]; set => Mbc[(ushort)Header.Version] = value; }

        public enum SGBFlag
        {
            None = 0,
            SGBSupport = 0x3
        }

        public bool SGBSupport
        {
            get => (byte)SGBFlag.SGBSupport == Mbc[(ushort)Header.DestinationCode];
            set => Mbc[(ushort)( Header.SGBFlag )] = (byte)( value ? SGBFlag.SGBSupport : SGBFlag.None );
        }

        public CartridgeType Type { get => (CartridgeType)Mbc[(ushort)Header.Type]; set => Mbc[(ushort)Header.Type] = (byte)value; }

        public Span<byte> Logo
        {
            get
            {
                var len = Header.LogoEnd + 1 - Header.LogoStart;
                return new Span<byte>( Mbc, (int)Header.LogoStart, (int)len );
            }
            set
            {
                if( value.Length != (int) Header.LogoLength ) throw new ArgumentException( $"Logo must be exaclty {(int)Header.LogoLength} bytes long" );

                var src = value.ToArray();
                Mbc.Write(
                    src: src,
                    src_offset: 0,
                    dst_offset: (ushort)Header.LogoStart,
                    len: (int)Header.LogoLength );
            }
        }

        private string GetHeaderString( Header start, Header end )
        {
            StringBuilder sb = new();

            for( var i = start; i <= end; i++ )
            {
                char c = (char)Mbc[(ushort)i];
                if( c == 0 ) break;
                sb.Append( c );
            }

            return sb.ToString();
        }

        private void SetHeaderString(Header start, Header len, string str )
        {
            Mbc.Write(
               src: str.Select( c => (byte)c ).ToArray(),
               src_offset: 0,
               dst_offset: (ushort)start,
               len: (ushort)len );
        }

        public string Title
        {
            get => GetHeaderString( Header.TitleStart, Header.TitleEnd );
            set => SetHeaderString( Header.TitleStart, Header.TitleLength, value );
        }

        public string Manufacturer
        {
            get => GetHeaderString( Header.ManufacturerStart, Header.ManufacturerEnd );
            set => SetHeaderString( Header.ManufacturerStart, Header.ManufacturerLength, value );
        }

        public static byte ComputeHeaderChecksum( IEnumerable<byte> header, int start = (int)Header.TitleStart )
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
        /// <param name="header"></param>
        /// <param name="start"></param>
        /// <returns>Checksum lsb first, needs to be byte swapped!</returns>
        public static ushort ComputeRomChecksum( IEnumerable<byte> banks )
        {
            ushort checksum = 0;

			foreach( byte b in banks )
			{
				checksum += b;
			}

			checksum -= banks.ElementAt( (int)Header.RomChecksumStart );
            checksum -= banks.ElementAt( (int)Header.RomChecksumEnd);

            return checksum;
        }

        public int RomBanks
        {
            get => ( 2 << Mbc[(ushort)Header.RomBanks] );
            set => Mbc[(ushort)Header.RomBanks] = (byte)( value >> 2 );
        }

        public int RamBanks
        {
            get => Mbc.RamBanks;
        }

        public static implicit operator Section( Cartridge cart) { return cart.Mbc;  } 

        public Cartridge( )
        {
            Mbc = new Mbc( new byte[0x8000] );
        }

        public bool Load( byte[] cart )
        {
			Mbc = new Mbc( cart );

            // TODO: restore ram

            Debug.WriteLine( $"Loaded cartridge {Title} v{Version} [{cart.Count()}B] {Type} {RomBanks}|{RamBanks} Banks" );
            Debug.WriteLine( $"Header|Rom checksum {HeaderChecksum:X2}|{RomChecksum:X4} SGB support {SGBSupport}" );
            Debug.WriteLine( $"Manufactuer {Manufacturer} Destination {Japan}" );

            var hCheck = ComputeHeaderChecksum( Mbc.Rom() );
			var rCheck = ComputeRomChecksum( Mbc.Rom() );

            Debug.WriteLine( $"Computed Header|Rom checksum {hCheck:X2}|{rCheck:X4}" );

			return true;
        }
    }
}
