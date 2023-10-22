namespace PeliPoika
{
	using static rzr.StandardFunctions;
	using static Palettes;

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

		private static ushort TileDataStart = 0x200;
		private static byte[] TileMap = new byte[32*32];

		protected override void WriteGameCode()
		{
			// TODO: find a tile that is clear / 0 and fint the index to it
			const byte TileMapClearId = 0xff;
			Array.Fill( TileMap, TileMapClearId);

			// replace background tile
			//var tileData = Project.GetTiles( "8bit_16x16_triangle.tl8", out var width, out var height, out var mode);
			var tileData = Project.GetTiles( "8bit_16x16_triangle.tl8", targetTileMap: TileMap, x: 0, y: 0);
			//tileData = Tiles.CompressTileData(tiles: tileData, mode: mode, width: width, height: height, targetTileMap: TileMap );

			// turn off audio
			ushort Entry = Xor( B ); // A = 0
			Ldh( 0x26, A ); // 0xFF26 rAUDENA 

			ushort WaitVBlank = Ldh( A, 0x44 );
			Cp( 140 );
			Jp( isC, WaitVBlank );

			// turn off LCD
			Xor( A );
			Ldh( 0x40, A ); // rLCDC LCD control

			this.memcopy( 0x8000, TileDataStart, (ushort)tileData.Length );
			ushort TileMapStart = (ushort)(TileDataStart + tileData.Length) ;
			this.memcopy( 0x9800, TileMapStart, (ushort)TileMap.Length );

			//Turn the LCD on

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

			// During the first( blank ) frame, initialize display registers

			var oldPalette = 0xe4.FromPalette();
			byte newPalette = BGColor.LightGray.Color1( BGColor.White ).Color2( BGColor.DarkGray ).Color3( BGColor.Black );
			Ld( A, newPalette );
			Ldh( 0x47, A ); // BGP palette
			Ld( L, A );

			const ushort delay = 100;
			var restart = Ld( BC, delay );

			var vsync = Ldh( A, 0x44 );
			Cp( 144 );
			Jp( isC, vsync );

			Dec( BC );
			Ld( A, B );
			Or( C );
			Jp( isNZ, vsync );

			const byte SCY = 0x42;
			const byte SCX = 0x43;

			Ldh( A, SCY );
			Inc( A );
			Ldh( SCY, A );

			Jp(restart); // remove

			// Cp(255);
			// Jp(isNZ, restart);
			// Inc(L);
			// Ld( A, L );
			// Ldh( 0x47, A );
			// Jp(restart);

			Write( tileData, ip: TileDataStart );
			Write( TileMap, ip: TileMapStart );
		}
	}
}