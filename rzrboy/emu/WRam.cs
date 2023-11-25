using System.Diagnostics;

namespace rzr
{
	public class WRam : ISection, IBankedMemory
	{
		public string Name = "WRAM";
		public ushort StartAddr => 0xC000;
		public ushort Length => 8192; // 8KiB

		public readonly byte[][] Banks = { new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096], new byte[4096] };
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

		public byte[] Save(int numBanks = 8) => Banks.Take( numBanks ).SelectMany(x=>x).ToArray();

		public void Load( byte[] wram )
		{
			// TODO: re-write as exceptions
			Debug.Assert( wram.Length % 4096 == 0, "WRam must be multiple of 4096 banks size" );
			Debug.Assert( wram.Length <= 8 * 4096, "Too much Wram" );

			foreach ((byte[] bank, int i) in wram.Split(4096).Indexed())
			{
				Banks[i] = bank;
			}
		}
	}
}
