using System.Diagnostics;

namespace rzr
{
	public class IOSection : ISection, IState
	{
		public string Name => "io";
		public ushort StartAddr => 0xFF00; // 0xFF00-0xFF7F
		public ushort Length => 127; // 0x7F

		public byte[] Data = new byte[127];
		public byte this[ushort address]
		{
			get => address >= 0xFF00 && address < 0xFF7F ? Data [address-StartAddr] : throw new rzr.AddressNotMappedException( address ); // for now just map to the data
			set
			{
				switch( address )
				{
					case < 0xFF00 or > 0xFF7F:
						throw new rzr.AddressNotMappedException( address );
					case 0xFF40: // LCDC
						//# https://www.reddit.com/r/Gameboy/comments/a1c8h0/what_happens_when_a_gameboy_screen_is_disabled/
						//# 1. LY (current rendering line) resets to zero. A few games rely on this behavior, namely Mr. Do! When LY
						//# is reset to zero, no LYC check is done, so no STAT interrupt happens either.
						//# 2. The LCD clock is reset to zero as far as I can tell.
						//# 3. I believe the LCD enters Mode 0 (OAM Search)
						Data[0x40] = value;
						if(!value.IsBitSet(7)) // LCD / PPU off
						{
							Data[0x44] = 0; // LY = 0
							// TODO reset PPU clock / Dot = 0
							byte prevMode = (byte)(Data[0x41] & 0b0000_0011);
							Data[0x41] &= 0b1111_1100; // unset mode bits => reset to mode 0
						}
						break;
					case 0xFF4F: // vram select: Reading from this register will return the number of the currently loaded VRAM bank in bit 0, and all other bits will be set to 1.
						m_onwer.vram.SelectedBank = value & 0b1;
						Data[0x4F] = (byte)(0b11111110 | ( value & 0b1 ));
						break;
					case 0xFF70: // wram select
						m_onwer.wram.SelectedBank = value & 0b111;
						Data[0x70] = (byte)(value & 0b111); //TODO: needs 0->1 adjustment?
						break;
					default:
						Data[address - StartAddr] = value; break;
				}
			}
		}

		public void Load( byte[] io) 
		{
			Debug.Assert( io.Length == Length );
			// manually copy to trigger all side effects
			for ( int i = 0; i < io.Length; i++ ) 
			{
				this[(ushort)(0xFF00+i)] = io[i];
			}
		}

		public byte[] Save() => Data;

		private Mem m_onwer;

		public IOSection( Mem owner )
		{
			m_onwer = owner;
		}

		// TODO: move to extension functions or section wrapper
		// https://gbdev.io/pandocs/Memory_Map.html#io-ranges
		// https://gbdev.io/pandocs/Hardware_Reg_List.html
		public byte Joypad { get => this[0xFF00]; set => this[0xFF00] = value; }
		public byte SerialData { get => this[0xFF01]; set => this[0xFF01] = value; } // SB
		public byte SerialControl { get => this[0xFF02]; set => this[0xFF02] = value; } // SC
		// Unknownd IO reg at 0xFF03
		public byte Div { get => this[0xFF04]; set => this[0xFF04] = value; }
		public byte TimerCounter { get => this[0xFF05]; set => this[0xFF05] = value; } // TIMA
		public byte TimerModulo { get => this[0xFF06]; set => this[0xFF06] = value; } // TMA
		public byte TimerControl { get => this[0xFF07]; set => this[0xFF07] = value; } // TAC
		public byte IF { get => this[0xFF0F]; set => this[0xFF0F] = value; } // Interrupt flag
		// TODO: sound channel NR10->NR52
		// TODO: Wave Ram FF30-FF3F
		public byte LcdControl { get => this[0xFF40]; set => this[0xFF40] = value; }
		public byte LCDC { get => this[0xFF40]; set => this[0xFF40] = value; }
		public byte LcdStatus { get => this[0xFF41]; set => this[0xFF41] = value; }
		public byte STAT { get => this[0xFF41]; set => this[0xFF41] = value; }
		public byte SCY { get => this[0xFF42]; set => this[0xFF21] = value; } // Viewport Y pos
		public byte SCX { get => this[0xFF43]; set => this[0xFF43] = value; } // Viewport X pos
		public byte LY { get => this[0xFF44]; set => this[0xFF44] = value; } // LCD Y coord
		public byte LYC { get => this[0xFF45]; set => this[0xFF45] = value; } // LY compare
		public byte DMA { get => this[0xFF46]; set => this[0xFF46] = value; } // OAM DMA src addr & start
		public byte BGP { get => this[0xFF47]; set => this[0xFF47] = value; } // BG palette data
		public byte OBP0 { get => this[0xFF48]; set => this[0xFF48] = value; }
		public byte OBP1 { get => this[0xFF49]; set => this[0xFF49] = value; }
		public byte WY { get => this[0xFF4A]; set => this[0xFF4A] = value; } // Window Y pos
		public byte WX { get => this[0xFF4B]; set => this[0xFF4B] = value; } // Window X pos
		// Unknownd IO reg at 0xFF4C
		public byte KEY1 { get => this[0xFF4D]; set => this[0xFF4D] = value; } // Prepare speed switch
		// Unknownd IO reg at 0xFF4E
		public byte VRamBank { get => this[0xFF4F]; set => this[0xFF4F] = value; } // VBK VRAM Bank
		// TODO: HDMA1->HMDA5
		public byte WRamBank { get => this[0xFF70]; set => this[0xFF70] = value; } // SVBK
	}
}
