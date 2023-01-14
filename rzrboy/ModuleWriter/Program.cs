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
		Rst10.Xor( B );

		Joypad.Ld( H, A );
	}

	protected override void WriteGameCode() 
	{
		Ld( A, 0x42 );
		Ld( A, adrBC ); // LD A, (BC)
		Ld( A, DE.Adr ); // LD A, (DE)

		Ld( 0x6000.Adr(), A );
		Ld( 0x6000, 0 );
		Ld( 0x2000, (byte)( Banks.Count & 0b11111 ) );
		Ld( 0x4000, (byte)( ( Banks.Count >> 5 ) & 0b11 ) );

		Inc( B );
		Dec( SP );

		Add( A, B );
		Add( A, 6 );

		Adc( D );
		Adc( 3 );

var lbl = Label;
		Sub( L );
		Sub( 155 );

		Sbc( E );
		Sbc( 255 );

		Jr( condNZ, (sbyte)( Label - lbl ) );
		Jr( 2 );

		Jp( 0x1234 );
		Jp( lbl );
		Jp( condC, 0x2000 );
		Jp( HL );

		And( HL.Adr );
		And( C );

		Or( H );
		Or( L );

		Xor( B );
		Xor( E );

		Cp( A );
		Cp( D );

		Ret();
		Ret( condZ );
		Reti();

		Push( BC );
		Pop( AF );

		Call( 0x4000 );
		Call( condNC, 0x3000 );

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