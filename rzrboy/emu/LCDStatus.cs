namespace rzr
{
	public enum PPUMode : byte
	{
		HBlank = 0,
		VBlank = 1,
		OAMSearch = 2,
		Drawing = 3
	}
	
	// https://gbdev.io/pandocs/STAT.html#ff41--stat-lcd-status
	public class STAT
	{
		// 7 Unused
		// 6 LYC int select (Read/Write): If set, selects the LYC == LY condition for the STAT interrupt.
		// 5 Mode 2 int select (Read/Write): If set, selects the Mode 2 condition for the STAT interrupt.
		// 4 Mode 1 int select (Read/Write): If set, selects the Mode 1 condition for the STAT interrupt.
		// 3 Mode 0 int select (Read/Write): If set, selects the Mode 0 condition for the STAT interrupt.
		// 2 LYC == LY (Read-only): Set when LY contains the same value as LYC; it is constantly updated.
		// [0-1]PPU mode (Read-only): Indicates the PPU’s current status.

		public byte Value = 0;

		public PPUMode Mode { get => (PPUMode)( Value & 0b11 ); set => Value = (byte)( ( Value & 0b11111100 ) | (byte)value ); }
		public bool LYCisLY { get => Value.IsBitSet( 2 ); set => Value.SetBit( 2, value ); }
		public bool HBlankInterrupt { get => Value.IsBitSet( 3 ); set => Value.SetBit( 3, value ); } // Mode 0
		public bool VBlankInterrupt { get => Value.IsBitSet( 4 ); set => Value.SetBit( 4, value ); } // Mode 1
		public bool OamInterrupt { get => Value.IsBitSet( 5 ); set => Value.SetBit( 5, value ); } // Mode 2
		public bool LYCInterrupt { get => Value.IsBitSet( 6 ); set => Value.SetBit( 6, value ); } // LY coincidence interrupt
	}
}