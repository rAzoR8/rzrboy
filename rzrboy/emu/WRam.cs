namespace rzr
{
	public class WRam : BankedMemory
	{
		public string Name => "WRAM";

		public WRam( ) : base( start: 0xC000, bankSize: 4096, banks: 8, directMapped: false ) { }
	}
}
