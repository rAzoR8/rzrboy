using System.Diagnostics;

namespace rzr
{
	public static class Boot
	{
		private static Section CreateMinimal()
		{
			Section boot = new( 0, len: 0x200, "MinimalBoot" );
			ushort pc = 0;

			Asm.Ld( Asm.SP, Asm.D16( 0xFFFE ) ).Assemble( ref pc, boot ); // init stack: SP = 0xFFFE
			Asm.Jp( Asm.A16( 0x150 - 4 ) ).Assemble( ref pc, boot ); // 4 byte instructions:

			pc = 0x150 - 4;
			Asm.Ld( Asm.A, Asm.D8( 1 ) ).Assemble( ref pc, boot ); // A = 1
			Asm.Ld( Asm.Io8( 0x50 ), Asm.A ).Assemble( ref pc, boot ); // mem[0xFF50]=A: disable booting, switch to rom
			Debug.Assert( pc == 0x150 );

			boot.ReadOnly = true;
			return boot;
		}

		public static Section Minimal { get; } = CreateMinimal();
	}
}
