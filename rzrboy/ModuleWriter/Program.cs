﻿using rzr;
using System.Diagnostics;

MyGame peliPoika = new();

while(true)
{
	Console.WriteLine( "publish?" );
	int key = Console.In.Read();
	if( key == 'q' || key == 'n' )
		break;

	peliPoika.WriteAll();
	byte[] rom = peliPoika.Rom();
	foreach( string instr in Isa.Disassemble( 0x150, peliPoika.PC, new Storage( rom ) ) )
	{
		Console.WriteLine( instr );
	}

	try
	{
		File.WriteAllBytes( $"{peliPoika.Title}.gb", rom ); // local
		Console.WriteLine( $"Written {peliPoika.Title} v{peliPoika.Version} {rom.Length}B HeaderChk {peliPoika.HeaderChecksum:X2} RomChk {peliPoika.RomChecksum:X4}");
		File.WriteAllBytes( $"D:\\Assets\\gbc\\common\\{peliPoika.Title}.gb", rom ); // pocket
	}
	catch( System.Exception e )
	{
		Console.WriteLine( $">> {e.Message}" );
	}
}

public static class Tiles
{
	public static IEnumerable<byte> FromColor( this uint row ) => FromColors( row );
	public static IEnumerable<byte> FromColor( this int row ) => FromColors( (uint)row );

	public static IEnumerable<byte> FromColors( params uint[] rows )
	{
		foreach( uint row in rows )
		{
			uint val = row; uint l = 0, r = 0;
			for( int i = 0; i < 8; i++ )
			{
				uint cur = val % 4;
				l |= ( cur & 0b01 ) << i;
				r |= ( ( cur & 0b10 ) >> 1 ) << i;

				Debug.WriteLine( $"{cur} {Convert.ToString( l, toBase: 2 ).PadLeft( 8, '0' )} {Convert.ToString( r, toBase: 2 ).PadLeft( 8, '0' )}" );

				val /= 10;
			}

			yield return (byte)l;
			yield return (byte)r; // todo: flip L & R
		}
	}
}

public class MyGame : rzr.MbcWriter
{
	public MyGame() {
		Title = "PeliPoika";
		Version = 1;
		//CGBSupport = (byte)HeaderView.CGBFlag.CGBOnly;

		Rst0.Xor( A );
		Rst10.Ld( B, 0 );
		// ...
		Joypad.Ld( H, A );
	}

	private static ushort TileDataStart = 0x200;
	private static byte[] TileData = {
	0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff, 0x00, 0xff,
	0x00, 0xff, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80, 0x00, 0x80,
	0x00, 0xff, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e, 0x00, 0x7e,
	0x00, 0xff, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x01,
	0x00, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
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
	0x55, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	0x55, 0xff, 0x6a, 0x1f, 0x05, 0x0f, 0x02, 0x07, 0x05, 0x07, 0x02, 0x03, 0x03, 0x01, 0x02, 0x01,
	0x54, 0xff, 0x80, 0x80, 0x00, 0x80, 0x80, 0x80, 0x00, 0x80, 0x80, 0x80, 0x00, 0x80, 0x00, 0x80,
	0x55, 0xff, 0x2a, 0x1f, 0x0d, 0x07, 0x06, 0x03, 0x01, 0x03, 0x02, 0x01, 0x01, 0x01, 0x00, 0x01,
	0x55, 0xff, 0x2a, 0x7f, 0x55, 0x7f, 0x2a, 0x7f, 0x55, 0x7f, 0x2a, 0x7f, 0x55, 0x7f, 0x00, 0x7f,
	0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0xaa, 0xff, 0x55, 0xff, 0x00, 0xff,
	0x15, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
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

	private static ushort TileMapStart = (ushort)(TileDataStart + TileData.Length);
	private static byte[] TileMap =
	{
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x01, 0x02, 0x03, 0x01, 0x04, 0x03, 0x01, 0x05, 0x00, 0x01, 0x05, 0x00, 0x06, 0x04, 0x07, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0b, 0x0e, 0x0f, 0x08, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x0f, 0x14, 0x1b, 0x0f, 0x14, 0x1c, 0x16, 0x1d, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x22, 0x25, 0x1e, 0x22, 0x25, 0x26, 0x22, 0x27, 0x1d, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x01, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2b, 0x2e, 0x2d, 0x2f, 0x30, 0x2d, 0x31, 0x32, 0x33, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x08, 0x34, 0x0a, 0x0b, 0x11, 0x0a, 0x0b, 0x35, 0x36, 0x0b, 0x0e, 0x0f, 0x08, 0x37, 0x0a, 0x38, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x14, 0x39, 0x16, 0x17, 0x1c, 0x16, 0x17, 0x3a, 0x3b, 0x17, 0x1b, 0x0f, 0x14, 0x3c, 0x16, 0x1d, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x1e, 0x3d, 0x3e, 0x3f, 0x22, 0x27, 0x21, 0x1f, 0x20, 0x21, 0x22, 0x25, 0x1e, 0x22, 0x40, 0x1d, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x41, 0x42, 0x43, 0x44, 0x30, 0x33, 0x41, 0x45, 0x43, 0x41, 0x30, 0x43, 0x41, 0x30, 0x33, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,  0,0,0,0,0,0,0,0,0,0,0,0,
	};

	protected override void WriteGameCode() 
	{
		// turn off audio
		ushort Entry = Xor( A ); // A = 0
		Ldh( 0x26, A ); // 0xFF26 rAUDENA 

ushort WaitVBlank = Ldh( A, 0x44 );
		Cp( 144 );
		Jp( isC, WaitVBlank );

		// turn off LCD
		Xor( A );
		Ldh( 0x40, A ); // rLCDC LCD control

		Ld( DE, TileDataStart );
		Ld( HL, 0x9000 );
		Ld( BC, (ushort)TileData.Length );

ushort CopyTiles = Ld( A, adrDE );
		Ld( adrHLi, A );
		Inc( DE );
		Dec( BC );
		Ld( A, B );
		Or( C );
		Jp(isNZ, CopyTiles );

		Ld( DE, TileMapStart );
		Ld( HL, 0x9800 ); // 0x9800
		Ld( BC, (ushort)(TileMap.Length) );

ushort CopyTilemap = Ld( A, adrDE );
		Ld( adrHLi, A );
		Inc( DE );
		Dec( BC );
		Ld( A, B );
		Or( C );
		Jp( isNZ, CopyTilemap );

		//Turn the LCD on
		Ld( A, 0b10000001 );
		Ldh( 0x40, A );

		// During the first( blank ) frame, initialize display registers

		Ld( A, 0xe4 );
		Ldh( 0x47, A ); // BGP palette
		Inc( A );

		Jp( PC );// while true

var resetB = Ld( B, 0 );
var start = Ldh( A, 0x44 );
		Cp( 144 );
		Jp( isC, start );

		Ld( A, B );
		Ldh( 0x47, A );
		Inc( B );
		Jp( start );

		Write( TileData, ip: TileDataStart );
		Write( TileMap, ip: TileMapStart );
	}
}