using static PeliPoika.Tiles;

namespace PeliPoika
{
	using Font = IReadOnlyDictionary<char, byte[]>;

	public static class Fonts
	{
		// todo turn into readonly static field, not property once the font is finalized
		public static Font Milla => new Dictionary<char, byte[]>()
			{
				{ 'A', From4Bit(
					03333300,
					32000330,
					32000330,
					33333330,
					32000330,
					32000330,
					32000330,
					00000000)
				},
				{ 'I', From4Bit(
					33333300,
					00230000,
					00230000,
					00230000,
					00230000,
					00230000,
					33333300,
					00000000)
				},
				{ 'L', From4Bit(
					32000000,
					32000000,
					32000000,
					32000000,
					32000000,
					32222220,
					33333330,
					00000000)
				},
				{ 'M', From4Bit(
					32000230,
					33202330,
					33030330,
					33020330,
					33000330,
					33000330,
					33000330,
					00000000)
				},
				{ 'U', From4Bit(
					32000330,
					32000330,
					32000330,
					32000330,
					32000330,
					32000330,
					33333330,
					00000000)
				},
			};

		public static byte[] ToTiles( this Font font, string str )
		{
			const int BytesPerTile = 8 * 2;
			byte[] result = new byte[str.Length * BytesPerTile];
			for( int i = 0; i < str.Length; ++i )
			{
				if( font.TryGetValue( str[i], out var tile ) )
					Array.Copy( tile, sourceIndex: 0, result, destinationIndex: i * BytesPerTile, BytesPerTile );
			}
			return result;
		}
	}
}
