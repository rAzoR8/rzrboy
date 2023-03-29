using System.Diagnostics;

namespace PeliPoika
{
	public static class Tiles
	{
		public const int BytesPerTile = 8 * 2;

		public static readonly byte[] Color0 = From4Bit( 00000000, 00000000, 00000000, 00000000, 00000000, 00000000, 00000000, 00000000 );
		public static readonly byte[] Color1 = From4Bit( 11111111, 11111111, 11111111, 11111111, 11111111, 11111111, 11111111, 11111111 );
		public static readonly byte[] Color2 = From4Bit( 22222222, 22222222, 22222222, 22222222, 22222222, 22222222, 22222222, 22222222 );
		public static readonly byte[] Color3 = From4Bit( 33333333, 33333333, 33333333, 33333333, 33333333, 33333333, 33333333, 33333333 );

		public static byte[] From4Bit( params uint[] rows )
		{
			byte[] result = new byte[ rows.Length * 2];

			for(uint r = 0; r < rows.Length; ++r)
			{
				uint val = rows[r];
				uint left = 0, right = 0;
				for( int i = 0; i < 8; i++ )
				{
					uint cur = val % 10;

					if( cur > 3 )
						throw new ArgumentOutOfRangeException( $"Invalid color {cur} at index {i} of {r}" );

					left |= ( cur & 0b01 ) << i;
					right |= ( ( cur & 0b10 ) >> 1 ) << i;

					val /= 10;
				}

				Debug.WriteLine( $"{Convert.ToString( left, toBase: 2 ).PadLeft( 8, '0' )} {Convert.ToString( right, toBase: 2 ).PadLeft( 8, '0' )}" );

				result[r*2] = (byte)left;
				result[r*2+1] = (byte)right;
			}

			return result;
		}

		public static class Fonts
		{
			// todo turn into readonly static field, not property once the font is finalized
			public static IReadOnlyDictionary<char, byte[]> Milla => new Dictionary<char, byte[]>()
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
		}

		public static byte[] ToTiles( this IReadOnlyDictionary<char, byte[]> font, string str )
		{
			byte[] result = new byte[str.Length*BytesPerTile];
			for( int i = 0; i < str.Length; ++i )
			{
				if( font.TryGetValue( str[i], out var tile ) )
					Array.Copy( tile, sourceIndex: 0, result, destinationIndex: i * BytesPerTile, BytesPerTile );
			}
			return result;
		}
	}
}
