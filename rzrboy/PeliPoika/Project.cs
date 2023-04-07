using System.Runtime.CompilerServices;

namespace PeliPoika
{
	public class Project
	{
		public static string SourceFilePath( [CallerFilePath] string sourceFilePath = "" ) => sourceFilePath;

		public string ProjectDir { get; set; } = System.IO.Path.GetDirectoryName( SourceFilePath() ) ?? System.IO.Directory.GetCurrentDirectory();
		public string AssetDir => System.IO.Path.Combine( ProjectDir, "Assets" );
		public string TilesDir => System.IO.Path.Combine( AssetDir, "Tiles" );

		public byte[] GetTiles( string path )
		{
			var tile = System.IO.Path.Combine( TilesDir, path );
			return Tiles.FromFile( tile );
		}
	}
}
