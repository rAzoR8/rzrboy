namespace rzr
{
	public class WRam : ISection
	{
		public string Name = "WRAM";
		public ushort StartAddr => 0xC000;
		public ushort Length => 8192; // 8KiB

		private byte[] m_banks = new byte[8*4096];
		private int m_index = 0;

		public byte this[ushort address] 
		{
			get => address < 0xD000 ? m_banks[address] : m_banks[0xD000 + m_index * 4096 + address % 4096];
			set {
				if( address < 0xD000 )
					m_banks[address] = value;
				else 
					m_banks[0xD000 + m_index * 4096 + address % 4096] = value;
			}
		}
	}
}
