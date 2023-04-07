using System.Diagnostics;
using System.Text;

namespace PeliPoika
{
	public static class Tiles
	{
		public static class FileFormat
		{
			public const uint Magic = 0x6c695472;
			public const byte Version = 1;
			public enum Mode : byte
			{ 
				Y8 = 8,
				Y16 = 16
			}
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

		public static byte[] FromStream( System.IO.Stream stream ) 
		{
			using( var reader = new BinaryReader( stream, Encoding.UTF8, leaveOpen: true ) )
			{
				var magic = reader.ReadUInt32();
				var version = reader.ReadByte();
				var mode = reader.ReadByte();

				if( magic == FileFormat.Magic && version == FileFormat.Version
					&& ( mode == (byte)FileFormat.Mode.Y8 || mode == (byte)FileFormat.Mode.Y16 ) )
				{
					var width = reader.ReadByte();
					var height = reader.ReadByte();

					if( width != 0 && height != 0 )
					{
						var bytesPerTile = mode * 2;
						return reader.ReadBytes( width * height * bytesPerTile );
					}
				}
			}

			return new byte[0];
		}

		public static byte[] FromFile( string filename )
		{
			try
			{
				using( var stream = File.Open( filename, FileMode.Open ) )
				{
					return FromStream( stream );
				}
			}
			catch( System.Exception ){}

			return new byte[0];
		}
	}
}
