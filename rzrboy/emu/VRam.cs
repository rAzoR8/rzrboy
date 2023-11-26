namespace rzr 
{
	public class VRam : BankedMemory
	{
		public string Name => "vram";
		public VRam() : base( start: 0x8000, bankSize: 0x2000, banks: 2, directMapped: true )
		{
		}
	}
}