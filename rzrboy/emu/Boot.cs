using static rzr.AsmOperandTypes;

namespace rzr
{
	public static class Boot
	{
		private static Section CreateMinimal()
		{
			Section boot = new( 0, len: 0x150, "MinimalBoot" );
			SectionBuilder sb = new( boot );

			sb.Ld( SP, 0xFFFE ); // init stack: SP = 0xFFFE
			sb.Jp( 0x14C );
			sb.IP = 0x14C; // 0x150 - 4;
			sb.Ld( A, 1 );
			sb.Ldh( 0x50, A ); // mem[0xFF50]=1: disable booting, switch to rom

			// [0x0000] LD SP, 0xFFFE
			// [0x0003] JP 0x14C
			// [0x014C] LD A, 1
			// [0x014E] LD (0xFF50), A (disable booting)

			boot.ReadOnly = true;
			return boot;
		}

		public static Section Minimal { get; } = CreateMinimal();
	}
}
