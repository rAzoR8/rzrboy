using System.Diagnostics;
using System.Text;

namespace PeliPoika
{
	public static class Tiles
	{
		public enum Mode : byte
		{
			Y8 = 8,
			Y16 = 16
		}

		public static class FileFormat
		{
			public const uint Magic = 0x6c695472;
			public const byte Version = 1;
		}

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

		public static byte[] FromStream( System.IO.Stream stream, out byte width, out byte height, out Mode mode ) 
		{
			using( var reader = new BinaryReader( stream, Encoding.UTF8, leaveOpen: true ) )
			{
				var magic = reader.ReadUInt32();
				var version = reader.ReadByte();
				var modeByte = reader.ReadByte();

				if( magic == FileFormat.Magic && version == FileFormat.Version
					&& ( modeByte == (byte)Mode.Y8 || modeByte == (byte)Mode.Y16 ) )
				{
					width = reader.ReadByte(); // num of tiles in x
					height = reader.ReadByte(); // num of tiles in y
					mode = (Mode)modeByte; // num of rows in tile: 8 or 16

					if( width != 0 && height != 0 )
					{
						var bytesPerTile = modeByte * 2;
						return reader.ReadBytes( width * height * bytesPerTile );
					}
				}
			}

			mode = default;
			width = height = 0;
			return new byte[0];
		}

		public static byte[] FromFile( string filename, out byte width, out byte height, out Mode mode )
		{
			try
			{
				using( var stream = File.Open( filename, FileMode.Open ) )
				{
					return FromStream( stream, out width, out height, out mode);
				}
			}
			catch( System.Exception ){}

			mode = default;
			width = height = 0;
			return new byte[0];
		}

		/// <summary>
		/// Creates a linear 1-to-1 tile map and writes it to the target buffer
		/// </summary>
		/// <param name="target">Target buffer, 32x32 bytes</param>
		/// <param name="xStart">x offset in the target buffer</param>
		/// <param name="yStart">y offset in the target buffer</param>
		/// <param name="width">number of tiles in x</param>
		/// <param name="height">number of tiles in y</param>
		public static void WriteTileMap( byte[] target, byte xStart, byte yStart, byte width, byte height )
		{
			byte tile = 0;
			for( int y = yStart; y < yStart + height; y++ )
			{
				for( int x = xStart; x < xStart + width; x++ )
				{
					target[y * 32 + x] = tile++;
				}
			}
		}

		// public static byte[] CompressTileData(byte[] tiles, Mode mode, byte width, byte height, byte[] targetTileMap )
		// {
		// 	// tile data -> map id
		// 	Dictionary<byte[], byte> tileMap = new();

			
		// 	for(byte y = 0; y<height; ++y)
		// 	{
		// 		for (byte x = 0; x < width; ++x)
		// 		{


		// 		}
		// 	}
		// }
	}
}
