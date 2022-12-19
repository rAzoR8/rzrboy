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

		Rst0.Xor( Asm.A );
		Rst10.Xor( Asm.B );

		Joypad.Ld( Asm.H, Asm.A );
	}

	protected override void WriteGameCode() 
	{
		Ld( Asm.A, Asm.D8( 42 ) );
		Ld( A, adrBC ); // LD A, (BC)
	}
}