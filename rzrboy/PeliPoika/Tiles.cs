using System.Diagnostics;

namespace PeliPoika
{
	public static class Tiles
	{
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
	}
}
