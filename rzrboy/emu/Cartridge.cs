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
        public const ushort RomBankSize = 0x4000; // 16KIB
        public const ushort RamBankSize = 0x2000; // 8KiB

        private const ushort BootRomReg = 0xFF50;
        private ISection IO;

        private bool Booting => IO[BootRomReg] == 0;

        private ProxySection m_romProxy;
        private ProxySection m_eramProxy;

        private List<RWSection> m_romBanks = new();
        private List<RWSection> m_ramBanks = new();

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
            TitleLength = TitleEnd + 1 - TitleStart
        }

        public enum DestinationCode : byte 
        {
            Japan = 0,
            NotJapan = 1
        }

        public CartridgeType Type { get => (CartridgeType)m_romBanks[0][(ushort)Header.Type]; set => m_romBanks[0][(ushort)Header.Type] = (byte)value; }

        public Span<byte> Logo
        {
            get
            {
                var len = Header.LogoEnd + 1 - Header.LogoStart;
                return new Span<byte>( m_romBanks[0].mem, (int)Header.LogoStart, (int)len );
            }
            set
            {
                if( value.Length != (int) Header.LogoLength ) throw new ArgumentException( $"Logo must be exaclty {(int)Header.LogoLength} bytes long" );

                var src = value.ToArray();
                m_romBanks[0].write(
                    src: src,
                    src_offset: 0,
                    dst_offset: (ushort)Header.LogoStart,
                    len: (int)Header.LogoLength );
            }
        }

        public string Title
        {
            get
            {
                StringBuilder sb = new();

                for ( var i = Header.TitleStart; i <= Header.TitleEnd; i++ )
                {
                    char c = (char)m_romBanks[0][(ushort)i];
                    if ( c == 0 ) break;
                    sb.Append(c);
                }

                return sb.ToString();
            }
            set {
                m_romBanks[0].write(
                    src: value.Select( c => (byte)c ).ToArray(), 
                    src_offset:0,
                    dst_offset: (ushort)Header.TitleStart,
                    len: Header.TitleStart - Header.TitleStart );
            }
        }

        public int RomBanks { get => ( 2 << m_romBanks[0][(ushort)Header.RomBanks] ); set { m_romBanks[0][(ushort)Header.RomBanks] = (byte)( value >> 2 ); } }
        public int RamBanks
        {
            get
            {
                byte banks = m_romBanks[0][(ushort)Header.RamBanks];
                switch ( banks )
                {
                    case 0: return 0;
                    case 1: return 0; // unused
                    case 2: return 1;
                    case 3: return 4;
                    case 4: return 16;
                    case 5: return 8;
                    default: throw new ArgumentOutOfRangeException("Unknown ram bank specifier");
                }
            }
        }

        public static byte HeaderChecksum( IEnumerable<byte> header, int start = (int)Header.TitleStart )
        {
            byte checksum = 0;
            for( int i = start; i < start + (int)Header.TitleLength; i++ )
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
        public static ushort GlobalChecksum( IEnumerable<byte> header, int start = 0 )
        {
            int len = start + (int)Header.RomChecksumStart;
            byte checksum = 0;
            for( int i = start; i < len; i++ )
            {
                checksum += header.ElementAt( i );
            }
            // skip global checksum, but accumulate everything else
            len = header.Count();
            for( int i = start + (int)Header.RomChecksumEnd + 1; i < len; ++i ) 
            {
                checksum += header.ElementAt( i );
            }
            return checksum;
        }

        private struct MBC
        {
            public ReadFunc romRead = ( ushort addr ) => 0xff;
            public WriteFunc mbcRegWrite = ( ushort addr, byte value ) => { };
            public ReadFunc ramRead = ( ushort addr ) => 0xff;
            public WriteFunc ramWrite = ( ushort addr, byte value ) => { };
        }

        private Dictionary<CartridgeType, MBC> mbcs = new();

        public Cartridge( ProxySection rom, ProxySection eram, ISection io )
        {
            m_romProxy = rom;
            m_eramProxy = eram;
            IO = io;

            mbcs.Add( CartridgeType.ROM_ONLY, new MBC() { romRead = MBC0ReadRomOnly } );
        }

        public bool Load( byte[] cart )
        {
            m_romBanks.Clear();
            m_ramBanks.Clear();
            m_selectedRamBank = 0;
            m_selectedRomBank = 0;
            m_ramEnabled = false;
            m_bankingMode = default;

            m_primaryRomBank = 0;
            m_selectedRomBank = 0;

            // copy cartridge data
            for( int i = 0; i < cart.Length; i += RomBankSize )
            {
                RSection bank = new( start: i < RomBankSize ? (ushort)0 : RomBankSize, RomBankSize, $"rom{m_romBanks.Count()}" );
                bank.write(cart, src_offset: i, dst_offset: 0, RomBankSize);
                m_romBanks.Add( bank );
            }

            for ( int i = 0; i < RamBanks; i++ )
            {
                m_ramBanks.Add( new RWSection( start: 0, RamBankSize, $"ram{m_ramBanks.Count()}" ) );
            }

            // TODO: restore ram

            // switch MBC:

            if ( mbcs.TryGetValue( Type, out var mbc ) )
            {
                m_romProxy.Source = new RWInterceptSection( mbc.romRead, mbc.mbcRegWrite, "rom", 0, RomBankSize * 2 );
                m_eramProxy.Source = new RWInterceptSection( mbc.ramRead, mbc.ramWrite, "eram", 0xA000, RamBankSize );
            }
            else
            {
                return false;
            }

            Debug.WriteLine( $"Loaded cartridge {Title} [{cart.Count()}B] {Type} {RomBanks}|{RamBanks} Banks" );

            return true;
        }

        private int m_selectedRomBank = 0;
        private int m_selectedRamBank = 0;
        private bool m_ramEnabled = false;

        enum BankingMode : int
        {
            SimpleRomBanking = 0, 
            RamBanking = 1
        }

        private BankingMode m_bankingMode = default;

        private int m_primaryRomBank = 0;
        private int m_secondaryRomBank = 0;

        private byte MBC0ReadRomOnly( ushort address )
        {
            if ( Booting )
            {
                if ( address < 0x100 ) return boot.DMG[address];
            }
            return m_romBanks[m_selectedRomBank][address];
        }

        private byte MBC1ReadRam( ushort address )
        {
            return m_ramBanks[m_selectedRamBank][address];
        }

        private void MBC1WriteRam( ushort address, byte value )
        {
            m_ramBanks[m_selectedRamBank][address] = value;
        }

        private void MBC1RegWrite( ushort address, byte value )
        {
            if ( address >= 0x0000 && address <= 0x1FFF ) // enable ram
            {
                m_ramEnabled = ( value & 0b1111 ) == 0xA;
                return;
            }
            else if ( address >= 0x2000 && address <= 0x3FFF ) // rom bank number lower 5 bits
            {
                m_primaryRomBank = (byte)( value & 0b11111 );
            }
            else if ( address >= 0x4000 && address <= 0x5FFF ) // rom bank number upper 2 bits
            {
                m_secondaryRomBank = (byte)( value & 0b11 );
            }
            else if ( address >= 0x6000 && address <= 0x7FFF ) // banking mode select
            {
                m_bankingMode = (BankingMode)( ( value & 0b1 ) );
            }

            m_selectedRomBank = ( ( m_secondaryRomBank << 5 ) + m_primaryRomBank ) % RomBanks;
            Debug.WriteLine( $"[{address:X4}:{value:X2}] Selected rom{m_selectedRomBank} [{m_primaryRomBank}:{m_secondaryRomBank}]" );
        }
    }
}
