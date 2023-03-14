using PeliPoika;
using rzr;

bool first = true;
while(true)
{
	if( !first )
	{
		Console.WriteLine( "publish?" );

		int key = Console.In.Read();
		if( key == 'q' || key == 'n' )
			break;
	}

	first = false;

	Game peliPoika = new();
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