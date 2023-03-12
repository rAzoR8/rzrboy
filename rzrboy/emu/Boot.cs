namespace rzr
{
	public static class Boot
	{
		public static Section Minimal
		{
			get 
			{
				AsmRecorder boot = new();
				boot.Jp( 0x150-5 ); // 3 byte jump

				for( int i = 3; i < 0x150-5; ++i )
					boot.Nop(); // fill with nops

				// 5 byte instructions
				boot.Ld( 0xFF50, 1 ); // disable booting, switch to rom
				
				return boot.Write( start: 0, len: 0x200 );
			}
		}
	}
}
