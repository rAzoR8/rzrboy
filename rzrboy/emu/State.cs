namespace rzr
{
	public class State
	{
		public Mem mem { get; }
		public Reg reg { get; }
		public Pix pix { get; }
		public Snd snd { get; }
		public Mbc mbc => mem.mbc;
		
		public State()
		{
			mem = new();
			reg = new();
			pix = new();
			snd = new();
		}
		
		public void LoadBootRom( byte[] boot )
		{
			mem.boot = new Section( start: 0x0000, len: (ushort)boot.Length, "bootrom", access: SectionAccess.Read, data: boot, offset: 0 );
		}
		
		public void LoadRom(  byte[] cart )
		{
			var type = (CartridgeType)cart[(ushort)HeaderOffsets.Type];
			if( type == mbc.Header.Type )
			{
				mbc.LoadRom( cart );
			}
			else
			{
				mem.mbc = Cartridge.CreateMbc( type, cart );			
			}
		}
	}
}
