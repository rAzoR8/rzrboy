﻿namespace PeliPoika
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
		private static byte[] TileData = {
			0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff,
			0x00, 0xff, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80,
			0x00, 0xff, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e,
			0x00, 0xff, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01,
			0x00, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0xff, 0x00, 0x7f, 0x00, 0x7f, 0x00, 0x7f, 0x00, 0x7f, 0x00, 0x7f, 0x00, 0x7f, 0x00, 0x7f,
			0x00, 0xff, 0x03, 0xfc, 0x00, 0xf8, 0x00, 0xf0, 0x00, 0xe0, 0x20, 0xc0, 0x00, 0xc0, 0x40, 0x80,
			0x00, 0xff, 0xc0, 0x3f, 0x00, 0x1f, 0x00, 0x0f, 0x00, 0x07, 0x04, 0x03, 0x00, 0x03, 0x02, 0x01,
			0x00, 0x80, 0x00, 0x80, 0x7f, 0x80, 0x00, 0x80, 0x00, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x00, 0x80,
			0x00, 0x7e, 0x2a, 0x7e, 0xd5, 0x7e, 0x2a, 0x7e, 0x54, 0x7e, 0xff, 0x00, 0xff, 0x00, 0x00, 0x00,
			0x00, 0x01, 0x00, 0x01, 0xff, 0x01, 0x00, 0x01, 0x01, 0x01, 0xfe, 0x01, 0xff, 0x01, 0x00, 0x01,
			0x00, 0x80, 0x80, 0x80, 0x7f, 0x80, 0x80, 0x80, 0x00, 0x80, 0xff, 0x80, 0x7f, 0x80, 0x80, 0x80,
			0x00, 0x7f, 0x2a, 0x7f, 0xd5, 0x7f, 0x2a, 0x7f, 0x55, 0x7f, 0xff, 0x00, 0xff, 0x00, 0x00, 0x00,
			0x00, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xfa, 0x07, 0xfd, 0x07, 0x02, 0x07,
			0x00, 0x7f, 0x2a, 0x7f, 0xd5, 0x7f, 0x2a, 0x7f, 0x55, 0x7f, 0xaa, 0x7f, 0xd5, 0x7f, 0x2a, 0x7f,
			0x00, 0xff, 0x80, 0xff, 0x00, 0xff, 0x80, 0xff, 0x00, 0xff, 0x80, 0xff, 0x00, 0xff, 0x80, 0xff,
			0x40, 0x80, 0x00, 0x80, 0x7f, 0x80, 0x00, 0x80, 0x00, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x00, 0x80,
			0x00, 0x3c, 0x02, 0x7e, 0x85, 0x7e, 0x0a, 0x7e, 0x14, 0x7e, 0xab, 0x7e, 0x95, 0x7e, 0x2a, 0x7e,
			0x02, 0x01, 0x00, 0x01, 0xff, 0x01, 0x00, 0x01, 0x01, 0x01, 0xfe, 0x01, 0xff, 0x01, 0x00, 0x01,
			0x00, 0xff, 0x80, 0xff, 0x50, 0xff, 0xa8, 0xff, 0x50, 0xff, 0xa8, 0xff, 0x54, 0xff, 0xa8, 0xff,
			0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80,
			0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e,
			0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01,
			0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xff, 0x80,
			0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xaa, 0x7f, 0xd5, 0x7f, 0xaa, 0x7f, 0xd5, 0x7f, 0xaa, 0x7f,
			0xf8, 0x07, 0xf8, 0x07, 0xf8, 0x07, 0x80, 0xff, 0x00, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xaa, 0xff,
			0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xff, 0x80,
			0xd5, 0x7f, 0xaa, 0x7f, 0xd5, 0x7f, 0xaa, 0x7f, 0xd5, 0x7f, 0xaa, 0x7f, 0xd5, 0x7f, 0xaa, 0x7f,
			0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xeb, 0x3c,
			0x54, 0xff, 0xaa, 0xff, 0x54, 0xff, 0xaa, 0xff, 0x54, 0xff, 0xaa, 0xff, 0x54, 0xff, 0xaa, 0xff,
			0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x00, 0xff,
			0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0x2a, 0xff,
			0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0x80, 0xff,
			0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0xaa, 0xff,
			0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0x2a, 0xff,
			0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0x80, 0xff,
			0x7f, 0x80, 0xff, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x7f, 0x80, 0x00, 0xff,
			0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0xfe, 0x01, 0x80, 0xff,
			0x3f, 0xc0, 0x3f, 0xc0, 0x3f, 0xc0, 0x1f, 0xe0, 0x1f, 0xe0, 0x0f, 0xf0, 0x03, 0xfc, 0x00, 0xff,
			0xfd, 0x03, 0xfc, 0x03, 0xfd, 0x03, 0xf8, 0x07, 0xf9, 0x07, 0xf0, 0x0f, 0xc1, 0x3f, 0x82, 0xff,
			0x55, 0xff, 0x2a, 0x7e, 0x54, 0x7e, 0x2a, 0x7e, 0x54, 0x7e, 0x2a, 0x7e, 0x54, 0x7e, 0x00, 0x7e,
			0x01, 0xff, 0x00, 0x01, 0x01, 0x01, 0x00, 0x01, 0x01, 0x01, 0x00, 0x01, 0x01, 0x01, 0x00, 0x01,
			0x54, 0xff, 0xae, 0xf8, 0x50, 0xf0, 0xa0, 0xe0, 0x60, 0xc0, 0x80, 0xc0, 0x40, 0x80, 0x40, 0x80,
			0x55, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x55, 0xff, 0x6a, 0x1f, 0x05, 0x0f, 0x02, 0x07, 0x05, 0x07, 0x02, 0x03, 0x03, 0x01, 0x02, 0x01,
			0x54, 0xff, 0x80, 0x80, 0x00, 0x80, 0x80, 0x80, 0x00, 0x80, 0x80, 0x80, 0x00, 0x80, 0x00, 0x80,
			0x55, 0xff, 0x2a, 0x1f, 0x0d, 0x07, 0x06, 0x03, 0x01, 0x03, 0x02, 0x01, 0x01, 0x01, 0x00, 0x01,
			0x55, 0xff, 0x2a, 0x7f, 0x55, 0x7f, 0x2a, 0x7f, 0x55, 0x7f, 0x2a, 0x7f, 0x55, 0x7f, 0x00, 0x7f,
			0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0x00, 0xff,
			0x15, 0xff, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x55, 0xff, 0x6a, 0x1f, 0x0d, 0x07, 0x06, 0x03, 0x01, 0x03, 0x02, 0x01, 0x03, 0x01, 0x00, 0x01,
			0x54, 0xff, 0xa8, 0xff, 0x54, 0xff, 0xa8, 0xff, 0x50, 0xff, 0xa0, 0xff, 0x40, 0xff, 0x00, 0xff,
			0x00, 0x7e, 0x2a, 0x7e, 0xd5, 0x7e, 0x2a, 0x7e, 0x54, 0x7e, 0xab, 0x76, 0xdd, 0x66, 0x22, 0x66,
			0x00, 0x7c, 0x2a, 0x7e, 0xd5, 0x7e, 0x2a, 0x7e, 0x54, 0x7c, 0xff, 0x00, 0xff, 0x00, 0x00, 0x00,
			0x00, 0x01, 0x00, 0x01, 0xff, 0x01, 0x02, 0x01, 0x07, 0x01, 0xfe, 0x03, 0xfd, 0x07, 0x0a, 0x0f,
			0x00, 0x7c, 0x2a, 0x7e, 0xd5, 0x7e, 0x2a, 0x7e, 0x54, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0x2a, 0x7e,
			0x00, 0xff, 0xa0, 0xff, 0x50, 0xff, 0xa8, 0xff, 0x54, 0xff, 0xa8, 0xff, 0x54, 0xff, 0xaa, 0xff,
			0xdd, 0x62, 0xbf, 0x42, 0xfd, 0x42, 0xbf, 0x40, 0xff, 0x00, 0xff, 0x00, 0xf7, 0x08, 0xef, 0x18,
			0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xab, 0x7c, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e,
			0xf9, 0x07, 0xfc, 0x03, 0xfd, 0x03, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01,
			0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7e, 0xd5, 0x7e, 0xab, 0x7c,
			0xf7, 0x18, 0xeb, 0x1c, 0xd7, 0x3c, 0xeb, 0x3c, 0xd5, 0x3e, 0xab, 0x7e, 0xd5, 0x7e, 0x2a, 0xff,
			0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xfe, 0x01, 0xff, 0x01, 0xa2, 0xff,
			0x7f, 0xc0, 0xbf, 0xc0, 0x7f, 0xc0, 0xbf, 0xe0, 0x5f, 0xe0, 0xaf, 0xf0, 0x57, 0xfc, 0xaa, 0xff,
			0xff, 0x01, 0xfc, 0x03, 0xfd, 0x03, 0xfc, 0x03, 0xf9, 0x07, 0xf0, 0x0f, 0xc1, 0x3f, 0x82, 0xff,
			0x55, 0xff, 0x2a, 0xff, 0x55, 0xff, 0x2a, 0xff, 0x55, 0xff, 0x2a, 0xff, 0x55, 0xff, 0x00, 0xff,
			0x45, 0xff, 0xa2, 0xff, 0x41, 0xff, 0x82, 0xff, 0x41, 0xff, 0x80, 0xff, 0x01, 0xff, 0x00, 0xff,
			0x54, 0xff, 0xaa, 0xff, 0x54, 0xff, 0xaa, 0xff, 0x54, 0xff, 0xaa, 0xff, 0x54, 0xff, 0x00, 0xff,
			0x15, 0xff, 0x2a, 0xff, 0x15, 0xff, 0x0a, 0xff, 0x15, 0xff, 0x0a, 0xff, 0x01, 0xff, 0x00, 0xff,
			0x01, 0xff, 0x80, 0xff, 0x01, 0xff, 0x80, 0xff, 0x01, 0xff, 0x80, 0xff, 0x01, 0xff, 0x00, 0xff
		};

		private static ushort TileMapStart = (ushort)( TileDataStart + TileData.Length );
		private static byte[] TileMap = new byte[32*32];
	//	{
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01, 0x08, 0x07,  0,0,0,0,0,0,0,0,0,0,0,0,
	//};

		protected override void WriteGameCode()
		{
			Array.Fill( TileMap, (byte)0xff );

			//Ld( DE, 9630 );
			//Dec( DE );
			//Push( DE );
			//Push( AF );
			//Pop( DE );

			// replace background tile
			//data.CopyTo( TileData, 0 );
			var tiles = Project.GetTiles( "test.tl", targetTileMap: TileMap, x: 8, y: 6 );
			tiles.CopyTo( TileData, 0 );

			//Fonts.Milla.ToTiles( "LUMIA" ).CopyTo(TileData, 0);

			// turn off audio
			ushort Entry = Xor( B ); // A = 0
			Ldh( 0x26, A ); // 0xFF26 rAUDENA 

			ushort WaitVBlank = Ldh( A, 0x44 );
			Cp( 140 );
			Jp( isC, WaitVBlank );

			// turn off LCD
			Xor( A );
			Ldh( 0x40, A ); // rLCDC LCD control

			this.memcopy( 0x9000, TileDataStart, (ushort)TileData.Length );
			//memcopy = this.Function<ushort, ushort, ushort>( testCopy );
			this.memcopy( 0x9800, TileMapStart, (ushort)TileData.Length );

			//Turn the LCD on
			Ld( A, 0b10000001 );
			Ldh( 0x40, A );

			// During the first( blank ) frame, initialize display registers

			var oldPalette = 0xe4.FromPalette();
			byte newPalette = BGColor.LightGray.Color1( BGColor.White ).Color2( BGColor.DarkGray ).Color3( BGColor.Black );
			Ld( A, newPalette );
			Ldh( 0x47, A ); // BGP palette
			Inc( A );

			//Jp( PC );// while true

			//var resetB = Ld( B, 0 );
			//Ld( HL, 0x9800 );
			//Ld( BC, (ushort)( TileMap.Length ) );
			
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

			Ldh( A, SCX );
			Dec( A );
			Ldh( SCX, A );

			Jp( restart );
			

			//Ld( A, adrHL );
			//Inc( A );
			//Ld( adrHLi, A );
			//Dec( BC );
			//Ld( A, B );
			//Or( C );
			//Jp( isZ, resetB );

			//Jp( start );

			//Ld( A, B );
			//Ldh( 0x47, A );
			//Inc( B );
			//Jp( start );

			Write( TileData, ip: TileDataStart );
			Write( TileMap, ip: TileMapStart );
		}
	}
}