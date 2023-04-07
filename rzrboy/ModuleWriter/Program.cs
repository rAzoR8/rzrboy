﻿using PeliPoika;
using rzr;

ushort romChecksum = 0;
while(true)
{
	Game peliPoika = new();
	peliPoika.WriteAll();
	byte[] rom = peliPoika.Rom();

	bool changed = peliPoika.RomChecksum != romChecksum;
	romChecksum = peliPoika.RomChecksum;

	if( changed )
	{
		foreach( string instr in Isa.Disassemble( 0x150, peliPoika.PC, new Storage( rom ) ) )
		{
			Console.WriteLine( instr );
		}

		try
		{
			File.WriteAllBytes( $"{peliPoika.Title}.gb", rom ); // local
			Console.WriteLine( $"{( changed ? "Change" : "NO change" )} written {peliPoika.Title} v{peliPoika.Version} {rom.Length}B HeaderChk {peliPoika.HeaderChecksum:X2} RomChk {peliPoika.RomChecksum:X4}" );
			File.WriteAllBytes( $"D:\\Assets\\gbc\\common\\{peliPoika.Title}.gb", rom ); // pocket
			File.WriteAllBytes( $"D:\\Assets\\gb\\common\\{peliPoika.Title}.gb", rom ); // pocket
		}
		catch( System.Exception e )
		{
			Console.WriteLine( $">> {e.Message}" );
		}
	}
}