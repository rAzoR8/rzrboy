namespace rzr
{
	public class BinaryWriter : System.IO.BinaryWriter
	{
		public BinaryWriter() : base( new MemoryStream () ) { }
		public byte[] ToArray() { Flush(); return ( OutStream as MemoryStream ).ToArray(); }
	}

	public class BinaryReader : System.IO.BinaryReader
	{
		public BinaryReader(byte[] data) : base( new MemoryStream(data) ){}
		public void Read( ref bool value ) => value = ReadBoolean();
		public void Read( ref byte value ) => value = ReadByte();
		public void Read( ref sbyte value ) => value = ReadSByte();
		public void Read( ref short value ) => value = ReadInt16();
		public void Read( ref ushort value ) => value = ReadUInt16();
		public void Read( ref int value ) => value = ReadInt32();
		public void Read( ref uint value ) => value = ReadUInt32();
		public void Read( ref long value) => value = ReadInt64();
		public void Read( ref ulong value ) => value = ReadUInt64();
		public void Read (ref float value ) => value = ReadSingle();
		public void Read (ref double value ) => value = ReadDouble();
		public void Read (ref string value ) => value = ReadString();
	}

	public class State : IEmuState
	{
		public Mem m_mem = new();
		public Reg m_reg = new();
		public Cpu m_cpu;

		public ISection mem => m_mem;
		public IRegisters reg => m_reg;
		public ICpuState cpu => m_cpu;
		public Pix pix { get; }
		public Snd snd { get; }
		public Mbc mbc => m_mem.mbc;

		public IBankedMemory vram => m_mem.vram;
		public IBankedMemory rom => m_mem.mbc.Rom;
		public IBankedMemory eram => m_mem.mbc.Ram;
		public IBankedMemory wram => m_mem.wram;
		public IState oam => m_mem.oam;
		public IState io => m_mem.io;
		public IState hram => m_mem.hram;
		public IState bios => m_mem.boot;
		public IFramebuffer frame => pix.FrameBuffer;

		public State(Cpu cpu)
		{
			m_cpu = cpu;
			pix = new();
			snd = new();
		}		

		public void LoadBootRom( byte[] boot )
		{
			m_mem.boot = new Section( start: 0x0000, len: (ushort)boot.Length, "bootrom", access: SectionAccess.Read, data: boot, offset: 0 );
		}
		
		public byte[] SaveBootRom() { return m_mem.boot.Data.ToArray(); }

		public void LoadRom(  byte[] cart )
		{
			var type = (CartridgeType)cart[(ushort)HeaderOffsets.Type];
			if( type == mbc.Header.Type )
				mbc.LoadRom( cart );
			else
				m_mem.mbc = Cartridge.CreateMbc( type, cart );			
		}
	}
}
