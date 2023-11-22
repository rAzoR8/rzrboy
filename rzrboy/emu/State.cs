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

	public class State
	{
		public Mem mem { get; }
		public Reg reg { get; }
		public Pix pix { get; }
		public Snd snd { get; }
		public Mbc mbc => mem.mbc;

		// TODO: rename to CPU tick, or encapsulate in CpuState struct
		public ulong tick = 0;// current cycle/tick
		public byte curOpCode = 0; // opcode od the currenlty executed instruction
		public ushort curInstrPC = 0; // start ProgramCounter of the currently executed instruction
		public ushort prevInstrPC = 0; // start ProgramCounter of the previously executed instruction

		public byte curInstrCycle = 1; // number of Non-fetch cycles already spent on executing the current instruction
		public byte prevInstrCycles = 1; // number of non-fetch cycles spend on the previous instructions

		public IEnumerator<CpuOp>? curOp = null;

		public State()
		{
			mem = new();
			reg = new();
			pix = new();
			snd = new();
		}
		
		public byte[] SaveCpuState()
		{
			BinaryWriter bw = new();
			bw.Write( tick );
			bw.Write( curOpCode );
			bw.Write( curInstrPC );
			bw.Write( prevInstrPC );
			bw.Write( curInstrCycle );
			bw.Write( prevInstrCycles );

			bw.Write( mem.IE.Value );
			return bw.ToArray();
		}

		public void LoadCpuState( byte[] cpu )
		{
			BinaryReader br = new( cpu );
			br.Read( ref tick );
			br.Read( ref curOpCode );
			br.Read( ref curInstrPC );
			br.Read( ref prevInstrPC );
			br.Read( ref curInstrCycle );
			br.Read( ref prevInstrCycles );

			byte IE = 0;
			br.Read( ref IE );
			mem.IE.Value = IE;

			// catch up on the passed cycles on this instruction
			curOp = Cpu.Instructions[curOpCode].GetEnumerator();
			for( int i = 0; i < curInstrCycle; ++i )
				curOp.MoveNext();
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
