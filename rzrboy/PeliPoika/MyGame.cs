namespace PeliPoika
{
	using static rzr.StandardFunctions;
	using static rzr.AsmBuilderExtensions;

	using static Palettes;
    using System.Diagnostics;
    using rzr;

    public class Game : rzr.FunctionBuilder
	{
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

		private static string[] TileNames = {"8bit_bw_20x18_gaussian.tl8", "8bit_16x16_lanczos3.tl8"}; 

		// TODO: track with RAII / free-map
		private ushort m_wram = 0xC000;
		private ushort WRAM1B => m_wram++;
		private ushort WRAM2B { get { ushort ret = m_wram; m_wram += 2; return ret; } }
		private ushort WRAM(ushort len) { ushort ret = m_wram; m_wram += len; return ret; }
		private ushort[] WRAM(ushort count, byte elemSize)
		{ 
			ushort[] ret = new ushort[count];
			for (ushort i = 0; i < count; i++)
			{
				ret[i] = m_wram;
				m_wram += elemSize;
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
			const ushort TileDataStart = 0x200;

			// turn off audio
			ushort Entry = Xor( A ); // A = 0
			Ldh( rAUDENA, A ); // 0xFF26 rAUDENA 

			// During the first( blank ) frame, initialize display registers
			byte newPalette = BGColor.LightGray.Color1( BGColor.White ).Color2( BGColor.DarkGray ).Color3( BGColor.Black );
			Ld( A, newPalette );
			Ldh( BGP, A ); // BGP palette
			Ld( L, A );

			Xor(A); // A = 0
			Ldh( SCY, A ); // SCY = 0

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
			Address adrCurTile = WRAM1B.Adr();

			ushort resetStack = PC;
			this.Ld(adr: adrCurTile, 0);

			List<(ushort offset, byte[] data, byte[] map)> tileSets = new();
			{
				ushort tileOffset = TileDataStart;
				foreach(string name in TileNames)
				{
					byte[] tileMap = new byte[32*32];
					Array.Fill( tileMap, TileMapClearId);

					var tileData = Project.GetTiles( name, out var width, out var height, out var mode);
					tileData = Tiles.CompressTileData(tiles: tileData, mode: mode, width: width, height: height, targetTileMap: tileMap );
					tileSets.Add((tileOffset, tileData, tileMap));

					Ld(BC, tileOffset); // data offset
					Push(BC);

					Ld(BC, (ushort)tileData.Length); // data length
					Push(BC);

					tileOffset += (ushort)tileData.Length;
					Ld(BC, tileOffset); // map offset
					Push(BC);

					Ld(BC, (ushort)tileMap.Length); // map length
					Push(BC);
					tileOffset += (ushort)tileMap.Length;
				}
			}

			//this.waitvsync();

			const ushort delay = 100;
			var restart = Ld( BC, delay );

			// Turn the LCD on:
			Ld( A, 0b1001_0001 );
			Ldh( 0x40, A );

			var vsync = Ldh( A, 0x44 );
			Cp( 144 );
			Jp( isC, vsync );

			Dec( BC );
			Ld( A, B );
			Or( C );
			Jp( isNZ, vsync );

			Ldh( A, SCY );
			Inc( A );
			Ldh( SCY, A );

			Cp(160);
			Jp(isNZ, restart);
			
			Ld(A, adrCurTile);
			Inc(A);
			Ld(adrCurTile, A);
			Cp((byte)(TileCount+1));
			this.Jnb(resetStack); // reset if varTileCount >= TileCount
			
			{	
				// turn off LCD
				Xor( A );
				Ldh( 0x40, A ); // rLCDC LCD control

				//Ld( DE, TileDataStart );
				//Ld( HL, 0x8000 );
				//Ld( BC, (ushort)TileData.Length );

				Ld(HL, 0x9800 ); // dst
				Pop(BC); // length
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
			}
			
			Jp(restart);

			Debug.Assert(IP < TileDataStart);

			foreach(var (offset, data, map) in tileSets)
			{
				Write( data, ip: offset );
				Write( map, ip: offset + (uint)data.Length );
			}
		}
	}
}