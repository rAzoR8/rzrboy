using System.Diagnostics;

namespace rzr
{
    public class BootRom 
    {
        public readonly byte[] Rom;
        private (ushort start, ushort len)[] m_ranges;

        public delegate bool IsBooting();

        private IsBooting m_booting;

        public BootRom( IsBooting booting, byte[] data, params (ushort start, ushort len)[] ranges )
        {
            m_booting = booting;
            Rom = data;
            m_ranges = ranges;

            if( m_ranges == null || m_ranges.Count() == 0 )
            {
                if( data.Length == 0x100 ) // dmg
                {
                    m_ranges = new (ushort, ushort)[] { (0, 0x100) };
                }
                else if( data.Length == 0x800 ) // cgb
                {
                    m_ranges = new (ushort, ushort)[] { (0, 0x100), (0x200, 0x900) };
                }
                else // unknown,map everything and hope for the best
                {
                    m_ranges = new (ushort, ushort)[] { (0, (ushort)data.Length) };
                }
            }
        }

        public bool Accepts( ushort address ) 
        {
            if ( m_booting() == false ) return false;
			foreach( (ushort start, ushort len) in m_ranges )
			{
                if(address >= start && address < start + len)
                    return true;
			}
            return false;
        }
    }

	public class Mbc : Section
	{
        public const ushort RomBankSize = 0x4000; // 16KIB
        public const ushort RamBankSize = 0x2000; // 8KiB

        private byte[] m_rom;
		private byte[] m_ram;

		protected int m_selectedRomBank = 0;
		protected int m_selectedRamBank = 0;
		protected bool m_ramEnabled = false;

        public byte[] Ram() => m_ram;
        public byte[] Rom() => m_rom;

        public HeaderView Header { get; }
        public BootRom? BootRom { get; set; }

        public bool RamEnabled => m_ramEnabled && m_ram != null && m_ram.Length != 0 && Header.Type.HasRam();

		public Mbc()
		{
			m_rom = new byte[0x8000];
			m_ram = new byte[0x2000];
			Header = new HeaderView( m_rom );

            Header.RomBanks = 2;
            Header.RamBanks = 1;
		}

		public Mbc( byte[] rom, BootRom? boot = null )
            : base( start: 0, len: RomBankSize * 2 + RamBankSize, name: "MBC", alloc: false )
		{
            BootRom = boot;

            m_rom = new byte[rom.Length];
			Array.Copy( sourceArray: rom, destinationArray: m_rom, m_rom.Length );
            Header = new( m_rom );

			Debug.Assert( m_rom.Length == RomBankSize * Header.RomBanks );

			m_ram = new byte[RamBankSize * Header.RamBanks];
		}

        public override bool Contains( ushort address )
		{
            return
                address < 0x8000 ||     // roms banks
                                        // 0x8000-0x9FFF vram
                ( address > 0x9FFF &&   // 0xA000 - 0xC000 eram
                address < 0xC000 );     
        }

        // mapped access for emulator, default impl
        public override byte this[ushort address]
        {
            get
            {
                if( address < 0x8000 ) // rom
                {
					if( BootRom != null && BootRom.Accepts( address ) )
						return BootRom.Rom[address];

					var bankAdr = ( m_selectedRomBank * Header.RomBanks ) + address - StartAddr;
                    return m_rom[bankAdr];
                }
                else if( RamEnabled )
                {
                    var bankAdr = ( m_selectedRamBank * Header.RamBanks ) + address - StartAddr;
                    return m_ram[bankAdr];
                }
                return 0xFF;
            }
            set
            {
                if( address < 0x8000 ) // rom
                {
                    var bankAdr = ( m_selectedRomBank * Header.RomBanks ) + address - StartAddr;
                    throw new System.AccessViolationException( $"Trying to write to ROM at 0x{address:X4} BankAddr: 0x{bankAdr:X4}" );
                }
                else if( RamEnabled )
                {
                    var bankAdr = ( m_selectedRamBank * Header.RamBanks ) + address - StartAddr;
                    m_ram[bankAdr] = value;
                }
                //else
                //{
                //    var bankAdr = ( m_selectedRamBank * Header.RamBanks ) + address - StartAddr;
                //    throw new System.AccessViolationException( $"Trying to write to disabled RAM at 0x{address:X4} BankAddr: 0x{bankAdr:X4}" );
                //}
            }
        }
    }

	public class Mbc1 : Mbc
	{
        enum BankingMode : int
        {
            SimpleRomBanking = 0,
            RamBanking = 1
        }

        private BankingMode m_bankingMode = default;

        private int m_primaryRomBank = 0;
        private int m_secondaryRomBank = 0;

        public Mbc1( byte[] rom ) : base( rom )
		{
		}

        public override byte this[ushort address]
        {
            set
            {
                if( address >= 0x0000 && address <= 0x1FFF ) // enable ram
                {
                    m_ramEnabled = ( value & 0b1111 ) == 0xA;
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
                else if( address >= 0xA000 ) // ram
                {
                    base[address] = value;
                }

                m_selectedRomBank = ( ( m_secondaryRomBank << 5 ) + m_primaryRomBank ) % Header.RomBanks;
                Debug.WriteLine( $"[{address:X4}:{value:X2}] Selected rom{m_selectedRomBank} [{m_primaryRomBank}:{m_secondaryRomBank}]" );
            }
        }
    }
}
