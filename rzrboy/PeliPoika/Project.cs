using System.Runtime.CompilerServices;

namespace PeliPoika
{
	public class Project
	{
		public static string SourceFilePath( [CallerFilePath] string sourceFilePath = "" ) => sourceFilePath;

		public string ProjectDir { get; set; } = System.IO.Path.GetDirectoryName( SourceFilePath() ) ?? System.IO.Directory.GetCurrentDirectory();
		public string AssetDir => System.IO.Path.Combine( ProjectDir, "Assets" );
		public string TilesDir => System.IO.Path.Combine( AssetDir, "Tiles" );

		public byte[] GetTiles( string path, out byte width, out byte height, out Tiles.Mode mode )
		{
			var tile = System.IO.Path.Combine( TilesDir, path );
			return Tiles.FromFile( tile, out width, out height, out mode );
		}

		public byte[] GetTiles( string path, byte[] targetTileMap, byte x, byte y ) 
		{
			var tileData = GetTiles(path, out byte widht, out byte height, out Tiles.Mode mode );

			Tiles.WriteTileMap( targetTileMap, xStart: x, yStart: y, width: widht, height: height );

			return tileData;
		}

	}
}
