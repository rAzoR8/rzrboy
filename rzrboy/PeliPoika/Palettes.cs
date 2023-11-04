namespace PeliPoika
{
	public static class Palettes
	{
		public enum BGColor : byte
		{
			White = 0,
			LightGray = 1,
			DarkGray = 2,
			Black = 3
		}

		public static byte Color1( this BGColor color0, BGColor color1 ) => (byte)( (byte)color0 | (int)color1 << 2 );
		public static byte Color2( this byte palette, BGColor color2 ) => (byte)( palette | (int)color2 << 4 );
		public static byte Color3( this byte palette, BGColor color3 ) => (byte)( palette | (int)color3 << 6 );

		public static BGColor[] FromPalette( this int palette ) => new BGColor[] { (BGColor)( palette & 0b11 ), (BGColor)( (palette>>2) & 0b11 ), (BGColor)( ( palette >> 4 ) & 0b11 ), (BGColor)( ( palette >> 6 ) & 0b11 ) };

		public static byte MakeBGPalette( BGColor color0, BGColor color1, BGColor color2, BGColor color3 ) => (byte)( (int)color0 | (int)color1 << 2 | (int)color2 << 4 | (int)color3 << 6 );
	}
}
