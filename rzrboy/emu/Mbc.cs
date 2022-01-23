using System.Diagnostics;

namespace rzr
{
	public class Mbc : Section
	{
        public const ushort RomBankSize = 0x4000; // 16KIB
        public const ushort RamBankSize = 0x2000; // 8KiB

        private byte[] m_rom;
		private byte[] m_ram;

		private int m_selectedRomBank = 0;
		private int m_selectedRamBank = 0;
		private bool m_ramEnabled = false;

		public Mbc( byte[] rom ) : base( start: 0, len: RomBankSize + 2 + RamBankSize, name: "MBC", alloc: false )
		{
			int romBanks = 2 << rom[(ushort)Cartridge.Header.RomBanks];

			m_rom = new byte[RomBankSize * romBanks];

			Debug.Assert( m_rom.Length != rom.Length );
			Array.Copy( sourceArray: rom, destinationArray: m_rom, m_rom.Length );

			m_ram = new byte[RamBankSize * RamBanks];
		}

		public override bool Contains( ushort address )
		{
            return
                address < 0x8000 || // roms
                ( address > 0x9FFF && // 0x8000-0x9FFF vram
                address < 0xC000 );
        }

		public override byte Read( ushort address )
        {
            return m_rom[address - StartAddr];
        }
        public override void Write( ushort address, byte value )
        {
            m_rom[address - StartAddr] = value;
        }

        // mapped access for emulator, default impl
        public override byte this[ushort address]
        {
            get => Read( address );
            set => Write( address, value );
        }


        enum BankingMode : int
		{
			SimpleRomBanking = 0,
			RamBanking = 1
		}

		private BankingMode m_bankingMode = default;

		private int m_primaryRomBank = 0;
		private int m_secondaryRomBank = 0;

        public int RomBanks
        {
            get => ( 2 << this[(ushort)Cartridge.Header.RomBanks] );
            set => this[(ushort)Cartridge.Header.RomBanks] = (byte)( value >> 2 );
        }

        public int RamBanks
        {
            get
            {
                byte banks = this[(ushort)Cartridge.Header.RamBanks];
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

        private void MBC1RegWrite( ushort address, byte value )
        {
            if( address >= 0x0000 && address <= 0x1FFF ) // enable ram
            {
                m_ramEnabled = ( value & 0b1111 ) == 0xA;
                return;
            }
            else if( address >= 0x2000 && address <= 0x3FFF ) // rom bank number lower 5 bits
            {
                m_primaryRomBank = (byte)( value & 0b11111 );
            }
            else if( address >= 0x4000 && address <= 0x5FFF ) // rom bank number upper 2 bits
            {
                m_secondaryRomBank = (byte)( value & 0b11 );
            }
            else if( address >= 0x6000 && address <= 0x7FFF ) // banking mode select
            {
                m_bankingMode = (BankingMode)( ( value & 0b1 ) );
            }

            m_selectedRomBank = ( ( m_secondaryRomBank << 5 ) + m_primaryRomBank ) % RomBanks;
            Debug.WriteLine( $"[{address:X4}:{value:X2}] Selected rom{m_selectedRomBank} [{m_primaryRomBank}:{m_secondaryRomBank}]" );
        }

    }
}
