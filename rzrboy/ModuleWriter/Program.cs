﻿using rzr;

MyGame peliPoika = new();
peliPoika.WriteAll();
foreach( string instr in Isa.Disassemble( 0, peliPoika.PC, peliPoika.Banks[0], unknownOp: UnknownOpHandling.AsDb ) )
{
	Console.WriteLine( instr );
}
System.IO.File.WriteAllBytes( "peliPoika.gb", peliPoika.Rom() );

public class MyGame : rzr.MbcWriter
{
	public MyGame() {
		Title = "PeliPoika";
		Version = 1;
		Manufacturer = "Fabi";
		Type = CartridgeType.ROM_ONLY;
		CGBSupport = (byte)HeaderView.CGBFlag.CGBOnly;
		Japan = false;
		Logo = new byte[]{
			0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D , 0x00 , 0x0B , 0x03 , 0x73 , 0x00 , 0x83 , 0x00 , 0x0C , 0x00 , 0x0D,
			0x00, 0x08, 0x11, 0x1F, 0x88, 0x89 , 0x00 , 0x0E , 0xDC , 0xCC , 0x6E , 0xE6 , 0xDD , 0xDD , 0xD9 , 0x99,
			0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E , 0xEC , 0xCC , 0xDD , 0xDC , 0x99 , 0x9F , 0xBB , 0xB9 , 0x33 , 0x3E,
		};

		Rst0.Xor( Asm.A );
		Rst10.Xor( Asm.B );

		Joypad.Ld( Asm.H, Asm.A );
	}

	protected override void WriteGameCode() 
	{
		Ld( Asm.A, Asm.D8( 42 ) );
	}
}