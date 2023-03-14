using System.Diagnostics;

namespace rzr
{
    // MBC Memory Banking Controller
	public class Mbc : ISection
	{
        public const ushort RomBankSize = 0x4000; // 16KiB
        public const ushort RamBankSize = 0x2000; // 8KiB

        private List<byte> m_rom;
		private List<byte> m_ram;

		public int SelectedRomBank { get; protected set; } = 0;
		public int SelectedRamBank { get; protected set; } = 0;

		protected bool m_ramEnabled = false;
		public bool RamEnabled => m_ramEnabled && m_ram != null && m_ram.Count != 0 && Header.Type.HasRam();

		// ISection
		public string Name = "MBC";
		public ushort StartAddr => 0;
		public ushort Length => RomBankSize * 2 + RamBankSize;

		public byte[] Ram() => m_ram.ToArray();
		public byte[] Rom() => m_rom.ToArray();

		public Storage RomBank( int bankIndex, ushort sectionStart = 0 ) => new Storage( storage: m_rom, storageOffset: bankIndex * RomBankSize, startAddr: sectionStart, len: RomBankSize );
		public Storage RamBank( int bankIndex, ushort sectionStart = 0 ) => new Storage( storage: m_ram, storageOffset: bankIndex * RamBankSize, startAddr: sectionStart, len: RamBankSize );

		public HeaderView Header { get; }

		public Mbc()
        {
			m_rom = new List<byte>( Enumerable.Repeat<byte>( 0, RomBankSize * 2 ) );
			m_ram = new List<byte>( Enumerable.Repeat<byte>( 0, RamBankSize ) );
            Header = new HeaderView( m_rom );

            Header.RomBanks = 2;
            Header.RamBanks = 1;
		}

		public Mbc( byte[] rom, byte[]? ram = null )
		{
            m_rom = new( rom );
            Header = new( m_rom );

			Debug.Assert( m_rom.Count == RomBankSize * Header.RomBanks );

            if( ram != null )
            {
                Debug.Assert( ram.Length == RamBankSize * Header.RamBanks );
                m_ram = new( ram );
            }
            else
            {
                m_ram = new( Enumerable.Repeat<byte>( 0, RamBankSize * Header.RamBanks ) );
            }
        }

        public void LoadRam( byte[] ram )
        {
            Debug.Assert( ram.Length == RamBankSize * Header.RamBanks );
            m_ram.Clear();
            m_ram.AddRange( ram );
        }

        // Load rom of identical MBC type
        public void LoadRom( byte[] rom )
        {
            var type = (CartridgeType)rom[(ushort)HeaderOffsets.Type];
            Debug.Assert( type == Header.Type );
			Debug.Assert( rom.Length >= RomBankSize );
			//Debug.Assert( rom.Length == RomBankSize * Header.RomBanks );

			m_rom.Clear();
            m_rom.AddRange( rom );
        }

        public void ResizeRom( int bankCount )
        {
            var size = RomBankSize * bankCount;

            if( size > m_rom.Count )
            {
                m_rom.EnsureCapacity( size );
                m_rom.AddRange( Enumerable.Repeat<byte>( 0, ( bankCount - Header.RomBanks ) * RomBankSize ) );
			}
			else
			{
                m_rom.RemoveRange( size, m_rom.Count - size );
			}
            
            Header.RomBanks = bankCount;
        }

        public void ResizeRam( int bankCount )
        {
            bankCount = HeaderView.RamBankSizes.SkipWhile( b => b < bankCount ).First();

            var size = RamBankSize * bankCount;

            if( size > m_ram.Count )
            {
                m_ram.EnsureCapacity( size );
                m_ram.AddRange( Enumerable.Repeat<byte>( 0, ( bankCount - Header.RamBanks ) * RamBankSize ) );
            }
            else
            {
                m_ram.RemoveRange( size, m_ram.Count - size );
            }

            Header.RamBanks = bankCount;
        }

        public bool Accepts( ushort address )
		{
            return
                address < 0x8000 ||     // roms banks
                                        // 0x8000-0x9FFF vram
                ( address > 0x9FFF &&   // 0xA000 - 0xC000 eram
                address < 0xC000 );     
        }

        // mapped access for emulator, default impl
        public byte this[ushort address]
        {
            get
            {
                if( address < 0x8000 ) // rom
                {
					var bankAdr = ( SelectedRomBank * Header.RomBanks ) + address - StartAddr;
                    return m_rom[bankAdr];
                }
                else if( RamEnabled )
                {
                    var bankAdr = ( SelectedRamBank * Header.RamBanks ) + address - StartAddr;
                    return m_ram[bankAdr];
                }
                return 0xFF;
            }
            set
            {
                if( address < 0x8000 ) // rom
                {
                    var bankAdr = ( SelectedRomBank * Header.RomBanks ) + address - StartAddr;
                    throw new SectionWriteAccessViolationException( $"Trying to write to ROM at 0x{address:X4} BankAddr: 0x{bankAdr:X4}" );
                }
                else if( RamEnabled )
                {
                    var bankAdr = ( SelectedRamBank * Header.RamBanks ) + address - StartAddr;
                    m_ram[bankAdr] = value;
                }
                else
                {
                    var bankAdr = ( SelectedRamBank * Header.RamBanks ) + address - StartAddr;
                    throw new SectionWriteAccessViolationException( $"Trying to write to disabled RAM at 0x{address:X4} BankAddr: 0x{bankAdr:X4}" );
                }
            }
        }

        public bool FinalizeRom() 
        {
			Header.HeaderChecksum = HeaderView.ComputeHeaderChecksum( m_rom );
            Header.RomChecksum = HeaderView.ComputeRomChecksum( m_rom );
            return Header.Valid();
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

        // override Mbc behavior
        public new byte this[ushort address]
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

                SelectedRomBank = ( ( m_secondaryRomBank << 5 ) + m_primaryRomBank ) % Header.RomBanks;
                Debug.WriteLine( $"[{address:X4}:{value:X2}] Selected rom{SelectedRomBank} [{m_primaryRomBank}:{m_secondaryRomBank}]" );
            }
        }
    }
}
