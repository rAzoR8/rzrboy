using System.Diagnostics;

namespace rzr
{
	public class IOSection : ISection
	{
		public ushort StartAddr => 0xFF00;
		public ushort Length => 128;

		public byte[] Data = new byte[128];
		public byte this[ushort address]
		{
			get => Data[address-StartAddr]; // for now just map to the data
			set
			{
				switch( address )
				{
					//case >= 0xFF00 and < 0xFF80: return io;         // FF00-FF80 128B
					//case >= 0xFF80 and < 0xFFFF: return hram;       // FF80-FFFF 127B
					case 0xFF70: Data[address - StartAddr] = m_onwer.wram.SwitchableBank = value; break;
					default:
						Data[address - StartAddr] = value; break;
				}
			}
		}

		public void Load( byte[] io) 
		{
			Debug.Assert( io.Length == 128 );
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
		public byte Joypad => this[0xFF00];
		public byte SerialData => this[0xFF01];
		public byte SerialControl => this[0xFF02];
		public byte Div => this[0xFF03];
		public byte TimerCounter => this[0xFF05]; // TIMA
		public byte TimerControl => this[0xFF06];
		public byte IF => this[0xFF07]; // Interrupt flag
		// TODO: sound channel NR10->NR52
		// TODO: Wave Ram FF30-FF3F
		public byte LcdControl => this[0xFF40];
		public byte LCDC => this[0xFF40];
		public byte LcdStatus => this[0xFF41];
		public byte STAT => this[0xFF41];
		public byte SCY => this[0xFF42]; // Viewport Y pos
		public byte SCX => this[0xFF43]; // Viewport X pos
		public byte LY => this[0xFF44]; // LCD Y coord
		public byte LYC => this[0xFF45]; // LY compare
		public byte DMA => this[0xFF46]; // OAM DMA src addr & start
		public byte BGP => this[0xFF47]; // BG palette data
		public byte OBP0 => this[0xFF48];
		public byte OBP1 => this[0xFF49];
		public byte WY => this[0xFF4A]; // Window Y pos
		public byte WX => this[0xFF4B]; // Window X pos
		public byte KEY1 => this[0xFF4D]; // Prepare speed switch
		public byte VRamBank => this[0xFF4F]; // VBK VRAM Bank
		// TODO: HDMA1->HMDA5
		public byte WRamBank => this[0xFF70]; // SVBK
	}
}
