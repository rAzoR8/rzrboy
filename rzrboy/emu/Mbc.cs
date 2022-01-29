﻿using System.Diagnostics;

namespace rzr
{
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

        public override IList<byte>? Storage => m_rom;

        protected HeaderView m_header;

        public Mbc( byte[] rom ) : base( start: 0, len: RomBankSize + 2 + RamBankSize, name: "MBC", alloc: false )
		{
            m_header = new( rom );

			m_rom = new byte[RomBankSize * m_header.RomBanks];

			Debug.Assert( m_rom.Length != rom.Length );
			Array.Copy( sourceArray: rom, destinationArray: m_rom, m_rom.Length );

			m_ram = new byte[RamBankSize * m_header.RamBanks];
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
                    var bankAdr = ( m_selectedRomBank * m_header.RomBanks ) + address - StartAddr;
                    return m_rom[bankAdr];
                }
                else // ram, TODO: m_ramEnabled
                {
                    var bankAdr = ( m_selectedRamBank * m_header.RamBanks ) + address - StartAddr;
                    return m_ram[bankAdr];
                }
            }
            set
            {
                if( address < 0x8000 ) // rom
                {
                    var bankAdr = ( m_selectedRomBank * m_header.RomBanks ) + address - StartAddr;
                    m_rom[bankAdr] = value;
                }
                else // ram, TODO: m_ramEnabled
                {
                    var bankAdr = ( m_selectedRamBank * m_header.RamBanks ) + address - StartAddr;
                    m_ram[bankAdr] = value;
                }
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

                m_selectedRomBank = ( ( m_secondaryRomBank << 5 ) + m_primaryRomBank ) % m_header.RomBanks;
                Debug.WriteLine( $"[{address:X4}:{value:X2}] Selected rom{m_selectedRomBank} [{m_primaryRomBank}:{m_secondaryRomBank}]" );
            }
        }
    }
}
