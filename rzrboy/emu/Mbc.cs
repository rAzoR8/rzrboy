using System.Diagnostics;

namespace rzr
{
	public class BankedMemory : IBankedMemory
	{
		private List<byte[]> m_banks = new();

		public ushort BankSize { get; }
		public int Banks => (ushort)m_banks.Count;
		public int SelectedBank { get; set; } = 0;

		public IList<byte> this[int bank] => m_banks[bank];

		public BankedMemory( ushort bankSize, ushort numBanks )
		{
			BankSize = bankSize;
			for ( int i = 0; i < numBanks; i++ ) 
			{
				m_banks.Add(new byte[BankSize]);
			}
		}

		public void Resize(ushort bankCount)
		{
			if( bankCount > m_banks.Count )
			{
				for( int i = m_banks.Count; i < bankCount; ++i )
				{
					m_banks.Add( new byte[BankSize] );
				}
			}
			else if( bankCount < m_banks.Count )
			{
				m_banks.RemoveRange( bankCount - 1, m_banks.Count - bankCount );
			}
		}

		public void Load( byte[] data )
		{
			Debug.Assert( data.Length % BankSize == 0);
			m_banks.Clear();
			m_banks.AddRange( data.Split( BankSize ) );			
		}

		public byte[] Save() => m_banks.SelectMany(x => x ).ToArray();
	}

	// MBC Memory Banking Controller
	public class Mbc : ISection
	{
        public const ushort RomBankSize = 0x4000; // 16KiB
        public const ushort RamBankSize = 0x2000; // 8KiB

		public BankedMemory Rom { get; } = new( bankSize: RomBankSize, 2 );
		public BankedMemory Ram { get; } = new( bankSize: RamBankSize, 0 );

		protected bool m_ramEnabled = false;
		public bool RamEnabled => m_ramEnabled && Ram.Banks != 0 && Header.Type.HasRam();

        public HeaderView Header { get; }

		// ISection
		public string Name = "MBC";
		public ushort StartAddr => 0;
		public ushort Length => RomBankSize * 2 + RamBankSize;
		public bool Accepts( ushort address )
		{
            // 0000->3FFF
			// 4000->7FFF
			// A000->BFFF
			return
				address < 0x8000 ||     // 2x 16KiB banks banks
										// 0x8000-0x9FFF vram ( gap )
				( address > 0x9FFF &&   // 0xA000 - 0xC000 eram
				address < 0xC000 );
		}

		public Mbc()
		{
			Header = new HeaderView( Rom[0] );

            Header.RomBanks = 2;
            Header.RamBanks = 1;
		}

		public Mbc( byte[] rom, byte[]? ram = null )
		{
            Rom.Load( rom );
            Header = new( Rom[0] );

			Debug.Assert( rom.Length == RomBankSize * Header.RomBanks );
			Debug.Assert( Rom.Banks > 1 );

            if( ram != null )
            {
                Debug.Assert( ram.Length == RamBankSize * Header.RamBanks );
                Rom.Load( ram );
            }
        }

        public void LoadRam( byte[] ram )
        {
            Debug.Assert( ram.Length == RamBankSize * Header.RamBanks );
			Ram.Load( ram );
        }

        // Load rom of identical MBC type
        public void LoadRom( byte[] rom )
        {
            var type = (CartridgeType)rom[(ushort)HeaderOffsets.Type];
            Debug.Assert( type == Header.Type );
			Debug.Assert( rom.Length >= RomBankSize );
			//Debug.Assert( rom.Length == RomBankSize * Header.RomBanks );

            Rom.Load( rom );
        }

        public void ResizeRom( ushort bankCount )
        {
			Rom.Resize( bankCount );
			Header.RomBanks = bankCount;
        }

        public void ResizeRam( ushort bankCount )
        {
            bankCount = HeaderView.RamBankSizes.SkipWhile( b => b < bankCount ).First();

			Ram.Resize( bankCount );
			Header.RamBanks = bankCount;
        }

        // mapped access for emulator, default impl
        public byte this[ushort address]
        {
            get
            {
                if( address < 0x8000 ) // rom
                {
                    return Rom[Rom.SelectedBank][address - StartAddr];
                }
                else if( RamEnabled )
                {
					return Ram[Ram.SelectedBank][address - StartAddr];
				}
				return 0xFF;
            }
            set
            {
                if( address < 0x8000 ) // rom
                {
                    var bankAdr = ( Rom.SelectedBank * Header.RomBanks ) + address - StartAddr;
                    throw new SectionWriteAccessViolationException( $"Trying to write to ROM at 0x{address:X4} BankAddr: 0x{bankAdr:X4}" );
                }
                else if( RamEnabled )
                {
                    Ram[Ram.SelectedBank][address - StartAddr] = value;
                }
                else
                {
                    var bankAdr = ( Ram.SelectedBank * Header.RamBanks ) + address - StartAddr;
                    throw new SectionWriteAccessViolationException( $"Trying to write to disabled RAM at 0x{address:X4} BankAddr: 0x{bankAdr:X4}" );
                }
            }
        }

        public bool FinalizeRom() 
        {
			Header.HeaderChecksum = HeaderView.ComputeHeaderChecksum( Rom[0] );
            Header.RomChecksum = HeaderView.ComputeRomChecksum( Rom.Save() );
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

        private ushort m_primaryRomBank = 0;
        private ushort m_secondaryRomBank = 0;

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

                Rom.SelectedBank = ( ( m_secondaryRomBank << 5 ) + m_primaryRomBank ) % Header.RomBanks;
                Debug.WriteLine( $"[{address:X4}:{value:X2}] Selected rom{Rom.SelectedBank} [{m_primaryRomBank}:{m_secondaryRomBank}]" );
            }
        }
    }
}
