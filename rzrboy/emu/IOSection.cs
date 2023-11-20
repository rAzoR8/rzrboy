namespace rzr
{
	public class IOSection : ISection
	{
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

		public ushort StartAddr => 0xFF00;
		public ushort Length => 128;

		private Mem m_onwer;

		public IOSection( Mem owner )
		{
			m_onwer = owner;
		}
	}
}
