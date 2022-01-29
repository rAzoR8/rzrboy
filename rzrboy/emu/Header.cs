using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rzr
{
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

    public class HeaderView
	{
        private byte[] m_data;

        public HeaderView( byte[] data )
        {
            m_data = data;

            Debug.WriteLine( $"Loaded cartridge {Title} v{Version} [{data.Count()}B] {Type} {RomBanks}|{RamBanks} Banks" );
            Debug.WriteLine( $"Header|Rom checksum {HeaderChecksum:X2}|{RomChecksum:X4} SGB support {SGBSupport}" );
            Debug.WriteLine( $"Manufactuer {Manufacturer} Destination {Japan}" );

            var hCheck = ComputeHeaderChecksum( data );
            var rCheck = ComputeRomChecksum( data );

            Debug.WriteLine( $"Computed Header|Rom checksum {hCheck:X2}|{rCheck:X4}" );
        }

        public byte HeaderChecksum
        {
            get => m_data[(ushort)Header.HeaderChecksum];
            set => m_data[(ushort)Header.HeaderChecksum] = value;
        }
        public ushort RomChecksum
        {
            get => binutil.Combine( msb: m_data[(ushort)Header.RomChecksumStart], lsb: m_data[(ushort)Header.RomChecksumEnd] );
            set
            {
                m_data[(ushort)Header.RomChecksumStart] = value.GetMsb();
                m_data[(ushort)Header.RomChecksumEnd] = value.GetLsb();
            }
        }

        public enum DestinationCode : byte
        {
            Japan = 0,
            NotJapan = 1
        }

        public bool Japan
        {
            get => (byte)DestinationCode.Japan == m_data[(ushort)Header.DestinationCode];
            set => m_data[(ushort)( Header.DestinationCode )] = (byte)( value ? DestinationCode.Japan : DestinationCode.NotJapan );
        }

        public byte Version { get => m_data[(ushort)Header.Version]; set => m_data[(ushort)Header.Version] = value; }

        public enum SGBFlag
        {
            None = 0,
            SGBSupport = 0x3
        }

        public bool SGBSupport
        {
            get => (byte)SGBFlag.SGBSupport == m_data[(ushort)Header.DestinationCode];
            set => m_data[(ushort)( Header.SGBFlag )] = (byte)( value ? SGBFlag.SGBSupport : SGBFlag.None );
        }

        public CartridgeType Type { get => (CartridgeType)m_data[(ushort)Header.Type]; set => m_data[(ushort)Header.Type] = (byte)value; }

        public Span<byte> Logo
        {
            get
            {
                var len = Header.LogoEnd + 1 - Header.LogoStart;
                return new Span<byte>( m_data, (int)Header.LogoStart, (int)len );
            }
            set
            {
                if( value.Length != (int)Header.LogoLength ) throw new ArgumentException( $"Logo must be exaclty {(int)Header.LogoLength} bytes long" );

                var src = value.ToArray();
                Array.Copy(
                    src,
                    sourceIndex: 0,
                    m_data,
                    destinationIndex: (int)Header.LogoStart,
                    (int)Header.LogoLength );
            }
        }

        private string GetHeaderString( Header start, Header end )
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

        private void SetHeaderString( Header start, Header len, string str )
        {
            Array.Copy(
               sourceArray: str.Select( c => (byte)c ).ToArray(),
               sourceIndex: 0,
               destinationArray: m_data,
               destinationIndex: (int)start,
               length: (int)len );
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
            checksum -= banks.ElementAt( (int)Header.RomChecksumEnd );

            return checksum;
        }

        public int RomBanks
        {
            get => ( 2 << m_data[(ushort)Header.RomBanks] );
            set => m_data[(ushort)Header.RomBanks] = (byte)( value >> 2 );
        }

        public int RamBanks
        {
            get
            {
                byte banks = m_data[(ushort)Header.RamBanks];
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
        }
    }
}
