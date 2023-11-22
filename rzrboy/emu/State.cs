using System.Diagnostics;

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
				mbc.LoadRom( cart );
			else
				mem.mbc = Cartridge.CreateMbc( type, cart );			
		}
		public byte[] SaveRom() => mbc.Rom();

		public void LoadRegs( byte[] regs ) => reg.Load( regs );
		public byte[] SaveRegs() => reg.Save();
		public void LoadERam( byte[] eram ) => mbc.LoadRam( eram );
		public byte[] SaveERam() => mbc.Ram();
		public void LoadWRam( byte[] wram ) => mem.wram.Load( wram );
		public byte[] SaveWRam() => mem.wram.Save();
		public void LoadVRam( byte[] vram ) => mem.vram.Load( vram ); // TODO: select bank
		public byte[] SaveVRam() => mem.vram.Save();
		public void LoadIO( byte[] io ) { mem.io.Load(io); }
		public byte[] SaveIO() => mem.io.Save();
		public void LoadHRam( byte[] hram ) => mem.hram.Load( hram );
		public byte[] SaveHRam() => mem.hram.Save();
		public void LoadOam( byte[] oam ) => mem.oam.Load( oam );
		public byte[] SaveOam() => mem.oam.Save();
	}
}
