using PeliPoika;
using rzr;

ushort romChecksum = 0;
while(true)
{
	Game peliPoika = new();
	byte[] rom = peliPoika.WriteAll();

	bool changed = peliPoika.RomChecksum != romChecksum;
	romChecksum = peliPoika.RomChecksum;

	if( changed )
	{
		foreach( string instr in Isa.Disassemble( 0x150, peliPoika.PC, new Section( start: 0, len: Mbc.RomBankSize, name: "disrom", data: rom, access: SectionAccess.Read ) ) )
		{
			Console.WriteLine( instr );
		}

		try
		{
			var localTarget = Path.GetFullPath( $"{peliPoika.Title}.gb" );
			File.WriteAllBytes( localTarget, rom ); // local
			Console.WriteLine( $"{( changed ? "Change" : "NO change" )} written {peliPoika.Title} v{peliPoika.Version} {rom.Length}B HeaderChk {peliPoika.HeaderChecksum:X2} RomChk {peliPoika.RomChecksum:X4}" );
			Console.WriteLine($"\t{localTarget}");
			File.WriteAllBytes( $"D:\\Assets\\gb\\common\\{peliPoika.Title}.gb", rom ); // pocket
		}
		catch( System.Exception e )
		{
			Console.WriteLine( $">> {e.Message}" );
		}
	}
}