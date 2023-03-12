namespace rzr
{
	public static class Boot
	{
		public static Section Minimal
		{
			get 
			{
				AsmRecorder boot = new();
				boot.Jp( 0x100-5 ); // 3byte jump to game

				for( int i = 3; i < 0x100-5; ++i )
					boot.Nop(); // fill with nops

				// 5byte
				var pc = boot.Ld( 0xFF50, 1 ); // disable booting
				
				return boot.Write( start: 0, len: 256 );
			}
		}
	}
}
