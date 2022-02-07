using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rzr
{
    public enum HeaderOffsets : ushort
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

    public class HeaderView
	{
        private IList<byte> m_data;

        public HeaderView( IList<byte> data )
        {
            m_data = data;
        }

        public bool Valid() 
        {
            Debug.WriteLine( $"Loaded cartridge {Title} v{Version} [{m_data.Count()}B] {Type} {RomBanks}|{RamBanks} Banks" );
            Debug.WriteLine( $"Header|Rom checksum {HeaderChecksum:X2}|{RomChecksum:X4} SGB support {SGBSupport}" );
            Debug.WriteLine( $"Manufactuer {Manufacturer} Destination {Japan}" );

            var hCheck = ComputeHeaderChecksum( m_data );
            var rCheck = ComputeRomChecksum( m_data );

            Debug.WriteLine( $"Computed Header|Rom checksum {hCheck:X2}|{rCheck:X4}" );

            return hCheck == HeaderChecksum && rCheck == RomChecksum;
        }

        public byte HeaderChecksum
        {
            get => m_data[(ushort)HeaderOffsets.HeaderChecksum];
            set => m_data[(ushort)HeaderOffsets.HeaderChecksum] = value;
        }
        public ushort RomChecksum
        {
            get => binutil.Combine( msb: m_data[(ushort)HeaderOffsets.RomChecksumStart], lsb: m_data[(ushort)HeaderOffsets.RomChecksumEnd] );
            set
            {
                m_data[(ushort)HeaderOffsets.RomChecksumStart] = value.GetMsb();
                m_data[(ushort)HeaderOffsets.RomChecksumEnd] = value.GetLsb();
            }
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
                var len = HeaderOffsets.LogoEnd + 1 - HeaderOffsets.LogoStart;
                return m_data.Skip( (int) HeaderOffsets.LogoStart ).Take( len );
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

            checksum -= banks.ElementAt( (int)HeaderOffsets.RomChecksumStart );
            checksum -= banks.ElementAt( (int)HeaderOffsets.RomChecksumEnd );

            return checksum;
        }

        public int RomBanks
        {
            get => ( 2 << m_data[(ushort)HeaderOffsets.RomBanks] );
            set => m_data[(ushort)HeaderOffsets.RomBanks] = (byte)( value >> 2 );
        }

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
    }
}
