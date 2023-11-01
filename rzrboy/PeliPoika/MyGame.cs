namespace PeliPoika
{
	using static rzr.StandardFunctions;
	using static rzr.AsmBuilderExtensions;

	using static Palettes;
    using System.Diagnostics;
    using rzr;

    [AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property)]
	public class BankAttribute : System.Attribute
	{
		public ushort Index { get; }
		public BankAttribute(ushort index)
		{
			Index = index;
		}
	}

	public class BankedMem
	{
		public ushort Bank {get;}
		public byte[] Data {get;}		
		public ushort Address {get; private set;}

		public BankedMem(ushort bank, byte[] data )
		{
			Bank = bank;
			Data = data;
		}

		public void Write(ModuleBuilder mb)
		{
			// TODO: get next free address
			uint ip = Bank * (uint)Mbc.RomBankSize;
			mb.Write(Data, ip: ip);
			Address = AsmBuilder.IPtoPC(ip);
		}
		//public static implicit operator ushort( BankedMem adr ) => adr.adr;
	}

	public class Variable : IDisposable
	{
		public ushort Start {get;}
		public ushort Size {get;set;}

		public rzr.Address Adr => new(Start);

		private RamAllocator m_allocator;

		public Variable(ushort start, ushort size, RamAllocator owner)
		{
			Start = start;
			Size = size;
			m_allocator = owner;
		}

        public void Dispose()
        {
            m_allocator.Free(this);
        }
    }
    public class VariableComparer : IComparer<Variable>
    {
        public int Compare(Variable? x, Variable? y)
        {
			if(x == null && y == null) return 0;
			if(x != null && y == null) return 1;
			if(x == null && y != null) return -1;
            return (x.Size).CompareTo(y.Size);
        }
    }

    public class RamAllocator
	{
		public ushort Start {get;}
		public ushort End {get;}
		public ushort Cur {get; private set;}

		private List<Variable> m_free = new();

		public RamAllocator(ushort start, ushort end){ Start = Cur = start; End = end;}

		private int Search(ushort size)
		{
			int min = 0;
			int max = m_free.Count - 1;
			while (min <= max)
			{
				int mid = (min + max) / 2;
				if (size == m_free[mid].Size)
				{
					return mid;
				}
				else if (size < m_free[mid].Size)
				{
					max = mid - 1;
				}
				else
				{
					min = mid + 1;
				}
			}
			return min;
		}

		public Variable? Alloc(ushort size)
		{
			if(Cur + size < End)			
			{
				var variable = new Variable(Cur, size, this);
				Cur += size;
				return variable;
			}

			if(m_free.Count > 0)
			{
				int idx = Search(size);
				Variable v = m_free[idx];
				if(v.Size >= size)
				{
					m_free.RemoveAt(idx);
					if (v.Size > size) // split
					{
						ushort diff = (ushort)(v.Size - size);
						v.Size = size;
						Variable remainder = new Variable(start: (ushort)(v.Start + size), size: diff, this);
						idx = Search(diff);
						m_free.Insert(idx, remainder);
					}
					return v;
				}
			}

			return null;
		}

		public void Free(Variable var)
		{
			int idx = Search(var.Size);
			m_free.Insert(idx, var);
			// TODO: defrag / merge
		}
	}

	public class Game : rzr.FunctionBuilder
	{
		// TODO: whole game should be reconstructed on each write
		public Game()
		{
			Title = "PeliPoika";
			Version = 1;
			//CGBSupport = (byte)HeaderView.CGBFlag.CGBOnly;
			SGBSupport = false;

			Rst0.Xor( A );
			Rst10.Ld( B, 0 );
			// ...
			Joypad.Ld( H, A );
		}

		private Project Project { get; set; } = new Project();

		private static readonly byte BGPalette = BGColor.LightGray.Color1( BGColor.White ).Color2( BGColor.DarkGray ).Color3( BGColor.Black );
		private static (string name, byte x, byte y, byte palette)[] TileNames =
		{
			("8bit_taproom_20x18_lanczos3.tl8", 0, 0, BGPalette),
			("8bit_both_20x18.tl8", 0, 0, BGPalette.Flip()),
			("8bit_beerdog_16x16.tl8", 2, 2, BGPalette.Flip()),
			("8bit_glass_11x18_thumbnail_lanczos3.tl8", 4, 2, BGPalette),
			("8bit_16x16_triangle.tl8", 2, 2, BGPalette),
			("8bit_18x18_jasonface.tl8", 1, 1, BGPalette.Flip()), 
			("8bit_bw_20x18_gaussian.tl8", 0, 0, BGPalette), // first
		}; 


		private RamAllocator WRamAlloc = new RamAllocator(start: 0xC000, end: 0xD000); // in CGB end is 0xE000

		private Variable WRAM1B => WRamAlloc.Alloc(1);
		private Variable WRAM2B => WRamAlloc.Alloc(2);
		private Variable WRAM(ushort len) => WRamAlloc.Alloc(len);
		private Variable[] WRAM(ushort count, byte elemSize)
		{ 
			Variable[] ret = new Variable[count];
			for (ushort i = 0; i < count; i++)
			{
				ret[i] = WRAM(elemSize);
			}
			return ret;
		}

		public delegate void IfBlock( AsmRecorder self );
		void If(rzr.AsmOperandTypes.Condtype cond, IfBlock ifBlock, IfBlock elseBlock )
		{
			AsmRecorder self = new();
			self.IP = IP;

			// JP cond, if_block
			// [else_block]
			// Jp merge
			// [if_block]
			// merge

			var jpCond = Asm.Jp(cond.Type, Asm.A16(0)); // placeholder
			self.Consume(jpCond);
			elseBlock(self);
			var jpMerge = Asm.Jp( Asm.A16(0)); // placeholder
			self.Consume(jpMerge);
			ushort if_block = self.PC;
			ifBlock(self);
			ushort merge_block = self.PC;

			// fixup
			jpCond.R.d16 = if_block;
			jpMerge.L.d16 = merge_block;

			// assemble
			foreach (AsmInstr instr in self)
			{
				this.Consume(instr);
			}
		}

		protected override void WriteGameCode()
		{
			const byte rAUDENA = 0x26;
			const byte SCY = 0x42;
			const byte SCX = 0x43;
			const byte BGP = 0x47;
			// find a tile that is clear / 0 and fint the index to it
			const byte TileMapClearId = 0xff;
			const ushort TileDataStart = 0x300;

			// turn off audio
			ushort Entry = Xor( A ); // A = 0
			Ldh( rAUDENA, A ); // 0xFF26 rAUDENA 

			// 7 LCD & PPU enable: 0 = Off; 1 = On
			// 6 Window tile map area: 0 = 9800–9BFF; 1 = 9C00–9FFF
			// 5 Window enable: 0 = Off; 1 = On
			// 4 BG & Window tile data area: 0 = 8800–97FF; 1 = 8000–8FFF
			// 3 BG tile map area: 0 = 9800–9BFF; 1 = 9C00–9FFF
			// 2 OBJ size: 0 = 8×8; 1 = 8×16
			// 1 OBJ enable: 0 = Off; 1 = On
			// 0 BG & Window enable / priority [Different meaning in CGB Mode]: 0 = Off; 1 = On
			Ld( A, 0b1001_0001 );
			Ldh( 0x40, A );

			byte TileCount = (byte)TileNames.Length;
			// 1 Byte long variable in WRAM
			Variable adrCurTile = WRAM1B;

			ushort resetStack = PC;
			this.Ld(adr: adrCurTile.Adr, 0);

			List<(ushort offset, byte[] data, byte[] map)> tileSets = new();
			{
				ushort tileOffset = TileDataStart;
				foreach((string name, byte x, byte y, byte palette) in TileNames)
				{
					byte[] tileMap = new byte[32*32];
					Array.Fill( tileMap, TileMapClearId);

					var tileData = Project.GetTiles( name, out var width, out var height, out var mode);
					tileData = Tiles.CompressTileData(tiles: tileData, mode: mode, width: width, height: height, targetTileMap: tileMap, xOffset: x, yOffset: y);
					tileSets.Add((tileOffset, tileData, tileMap));

					Ld(BC, (ushort)(palette<<8));
					Push(BC);

					Ld(BC, tileOffset); // data offset
					Push(BC);

					Ld(BC, (ushort)tileData.Length); // data length
					Push(BC);

					tileOffset += (ushort)tileData.Length;
					Ld(BC, tileOffset); // map offset
					Push(BC);

					tileOffset += (ushort)tileMap.Length;

					if(tileOffset > 0xFFFF)
					{
						throw new System.IndexOutOfRangeException($"Not enough space tile {name} in this bank");
						break;
					}
				}
			}

			using(var a = WRamAlloc.Alloc(2))
			{

			}

			const ushort delay = 100;
			var restart = Ld( BC, delay );

			// Turn the LCD on:
			Ld( A, 0b1001_0001 );
			Ldh( 0x40, A );

			var vsync = Ldh( A, 0x44 );
			Cp( 148 );
			Jp( isC, vsync );

			Dec( BC );
			Ld( A, B );
			Or( C );
			Jp( isNZ, vsync );

			Ldh( A, SCY );
			Inc( A );
			Ldh( SCY, A );

			Cp(1);
			Jp(isNZ, restart);
			
			Ld(A, adrCurTile.Adr);
			Inc(A);
			Ld(adrCurTile.Adr, A);
			Cp((byte)(TileCount+1));
			this.Jnb(resetStack); // reset if varTileCount >= TileCount
			
			{	
				// turn off LCD
				Xor( A );
				Ldh( 0x40, A ); // rLCDC LCD control

				// pop in reverse order!
				Ld(HL, 0x9800 ); // dst
				
				//Pop(BC); // length
				Ld(BC, 32*32); // tile map length is constant
				Pop(DE); // src map data
				
				ushort copyMap = Ld( A, adrDE );
				Ld( adrHLi, A );
				Inc( DE );
				Dec( BC );
				Ld( A, B );
				Or( C );
				Jp( isNZ, copyMap );

				Ld(HL, 0x8000 ); // dst
				// pop in reverse order!
				Pop(BC); // length
				Pop(DE); // src tile data

				ushort copyData = Ld( A, adrDE );
				Ld( adrHLi, A );
				Inc( DE );
				Dec( BC );
				Ld( A, B );
				Or( C );
				Jp( isNZ, copyData );

				Pop(AF); // palette
				Ldh( BGP, A ); // BGP palette
			}
			
			Jp(restart);

			Debug.Assert(IP <= TileDataStart);

			uint eod =0;
			foreach(var (offset, data, map) in tileSets)
			{
				Write( data, ip: offset );
				Write( map, ip: offset + (uint)data.Length );
				eod = offset + (uint)data.Length;
			}
			Debug.WriteLine($"End of Data: {eod} ({Mbc.RomBankSize*2-eod} left)");
		}
	}
}