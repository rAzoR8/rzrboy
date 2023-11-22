using System.Diagnostics;

namespace rzr
{
    // MBC Memory Banking Controller
	public class Mbc : ISection
	{
        public const ushort RomBankSize = 0x4000; // 16KiB
        public const ushort RamBankSize = 0x2000; // 8KiB

        private List<byte[]> m_rom = new();
		private List<byte[]> m_ram = new();

		public int SelectedRomBank { get; protected set; } = 0;
		public int SelectedRamBank { get; protected set; } = 0;

		protected bool m_ramEnabled = false;
		public bool RamEnabled => m_ramEnabled && m_ram != null && m_ram.Count != 0 && Header.Type.HasRam();

		public byte[] Ram() => m_ram.SelectMany( x => x ).ToArray();
		public byte[] Rom() => m_rom.SelectMany( x => x ).ToArray();

		public Section RomBank( int bankIndex, ushort sectionStart = 0 ) => new Section( start: sectionStart, len: RomBankSize, name: $"RomBank{bankIndex}", access: SectionAccess.Read, data: m_rom[bankIndex], offset: 0 );
		public Section RamBank( int bankIndex, ushort sectionStart = 0 ) => new Section( start: sectionStart, len: RamBankSize, name: $"RamBank{bankIndex}", access: SectionAccess.ReadWrite, data: m_ram[bankIndex], offset: 0 );

        public HeaderView Header { get; }

		// ISection
		public string Name = "MBC";
		public ushort StartAddr => 0;
		public ushort Length => RomBankSize * 2 + RamBankSize;
		public bool Accepts( ushort address )
		{
			return
				address < 0x8000 ||     // 2x 16KiB banks banks
										// 0x8000-0x9FFF vram ( gap )
				( address > 0x9FFF &&   // 0xA000 - 0xC000 eram
				address < 0xC000 );
		}

		public Mbc()
        {
            m_rom.Add( new byte[RomBankSize] ); // 0000->3FFF
			m_rom.Add( new byte[RomBankSize] ); // 4000->7FFF
			m_ram.Add( new byte[RamBankSize] ); // A000->BFFF

			Header = new HeaderView( m_rom[0] );

            Header.RomBanks = 2;
            Header.RamBanks = 1;
		}

		public Mbc( byte[] rom, byte[]? ram = null )
		{
            m_rom = new( rom.Split( RomBankSize ) );
            Header = new( m_rom[0] );

			Debug.Assert( rom.Length == RomBankSize * Header.RomBanks );
			Debug.Assert( m_rom.Count > 1 );

            if( ram != null )
            {
                Debug.Assert( ram.Length == RamBankSize * Header.RamBanks );
                m_ram = new( ram.Split( RamBankSize ) );
            }
            else
            {
				m_ram.Add( new byte[RamBankSize] ); // A000->BFFF
			}
        }

        public void LoadRam( byte[] ram )
        {
            Debug.Assert( ram.Length == RamBankSize * Header.RamBanks );
            m_ram.Clear();
            m_ram.AddRange( ram.Split( RamBankSize ) );
        }

        // Load rom of identical MBC type
        public void LoadRom( byte[] rom )
        {
            var type = (CartridgeType)rom[(ushort)HeaderOffsets.Type];
            Debug.Assert( type == Header.Type );
			Debug.Assert( rom.Length >= RomBankSize );
			//Debug.Assert( rom.Length == RomBankSize * Header.RomBanks );

            m_rom = new( rom.Split( RomBankSize ) );
        }

        public void ResizeRom( int bankCount )
        {
			if( bankCount > m_rom.Count )
			{
				for( int i = m_rom.Count; i < bankCount; ++i )
				{
					m_rom.Add( new byte[RomBankSize] );
				}
			}
			else if( bankCount < m_rom.Count )
			{
				m_rom.RemoveRange( bankCount - 1, m_rom.Count - bankCount );
			}

			Header.RomBanks = bankCount;
        }

        public void ResizeRam( int bankCount )
        {
            bankCount = HeaderView.RamBankSizes.SkipWhile( b => b < bankCount ).First();

			if( bankCount > m_ram.Count )
			{
				for( int i = m_ram.Count; i < bankCount; ++i )
				{
					m_ram.Add( new byte[RamBankSize] );
				}
			}
			else if( bankCount < m_ram.Count )
			{
				m_ram.RemoveRange( bankCount - 1, m_ram.Count - bankCount );
			}

			Header.RamBanks = bankCount;
        }

        // mapped access for emulator, default impl
        public byte this[ushort address]
        {
            get
            {
                if( address < 0x8000 ) // rom
                {
                    return m_rom[SelectedRomBank][address - StartAddr];
                }
                else if( RamEnabled )
                {
					return m_rom[SelectedRamBank][address - StartAddr];
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
                    m_ram[SelectedRamBank][address - StartAddr] = value;
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
			Header.HeaderChecksum = HeaderView.ComputeHeaderChecksum( m_rom[0] );
            Header.RomChecksum = HeaderView.ComputeRomChecksum( m_rom.SelectMany(b=>b) );
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
