namespace rzr
{
	public class State
	{
		public Mem mem { get; }
		public Reg reg { get; }
		public Pix pix { get; }
		public Snd snd { get; }
		public Mbc mbc => mem.mbc;

		public ulong tick { get; set; } // current cycle/tick
		public byte curOpCode { get; set; } = 0; // opcode od the currenlty executed instruction
		public ushort curInstrPC { get; set; } = 0; // start ProgramCounter of the currently executed instruction
		public ushort prevInstrPC { get; set; } = 0; // start ProgramCounter of the previously executed instruction

		public byte prevInstrCycles { get; set; } = 1; // number of non-fetch cycles spend on the previous instructions
		public byte curInstrCycle { get; set; } = 1; // number of Non-fetch cycles already spent on executing the current instruction

		public IEnumerator<CpuOp>? curOp = null;

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
