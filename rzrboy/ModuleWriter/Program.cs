using rzr;

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
		CGBSupport = (byte)HeaderView.CGBFlag.CGBOnly;

		Rst0.Xor( A );
		Rst10.Ld( B, 0 );
		// ...
		Joypad.Ld( H, A );
	}

	protected override void WriteGameCode() 
	{
		ushort Label0 = Ld( A, 0x42 );
		Inc( A );
		Dec( B );
		Ld( C, A );
		Push( BC );
		Jp( isZ, Label0 );
		Cp( B );

		Jr( 2 );

		Jp( 0x1234 );
		Jp( Label0 );
		Jp( isC, 0x2000 );
		Jp( HL );
		Jr( isZ, (sbyte)( Label0 - PC ) );

		Ld( A, adrBC ); // LD A, (BC)
		Ld( A, DE.Adr ); // LD A, (DE)
		Ld( 0x6000.Adr(), A );

		Inc( B );
		Dec( SP );

		Add( A, B );
		Add( A, 6 );

		Adc( D );
		Adc( 3 );

		Sub( L );
		Sub( 155 );

		Sbc( E );
		Sbc( 255 );

		And( HL.Adr );
		And( C );

		Or( H );
		Or( L );

		Xor( B );
		Xor( E );

		Cp( A );
		Cp( D );

		Ret();
		Ret( isZ );
		Reti();

		Push( BC );
		Pop( AF );

		Call( 0x4000 );
		Call( isNC, 0x3000 );

		Di();
		Ei();
		Rlca();
		Rla();
		Daa();
		Scf();
		Rrca();
		Rra();
		Cpl();
		Ccf();

		Rst( 0 );
		//Rst( 3 ); // error

		Db( 0x00, 0x01, 0x02, 0x03 );
	}
}