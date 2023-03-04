namespace rzr
{
	public class WRam : ISection
	{
		public byte this[ushort address] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public string Name = "WRAM";
		public ushort StartAddr => 0xC000;
		public ushort Length => 8192; // 8KiB
	}
}
