namespace rzr
{
	public class WRam : ISection
	{
		public string Name = "WRAM";
		public ushort StartAddr => 0xC000;
		public ushort Length => 8192; // 8KiB

		public byte[][] Banks = { new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096] };
		private byte m_bank = 1;
		public byte SwitchableBank { get => m_bank; set { m_bank = (byte)(value > 0 ? value : 1); } }

		public byte this[ushort address] 
		{
			get
			{
				if( address < 0xD000 )
					return Banks[0][address - StartAddr];
				else
					return Banks[SwitchableBank][address - 0xD000];
			}
			set {
				if( address < 0xD000 )
					Banks[0][address - StartAddr] = value;
				else
					Banks[SwitchableBank][address - 0xD000] = value;
			}
		}
	}
}
