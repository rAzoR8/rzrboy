using ImGuiNET;

namespace dbg.ui
{
	public static class Fonts
	{
		public static string FontsFolder { get; set; } = SearchFontsFolder(); 

		public static ImFontPtr MonaspaceNeon { get; private set; }
		public static ImFontPtr AwesomeSolid { get; private set; }

		public static void Push( this ImFontPtr fontPtr ) => ImGui.PushFont( fontPtr );
		public static void Pop() => ImGui.PopFont();

		public static void Init() 
		{
			MonaspaceNeon = LoadEmbedded( "monaspace-neon.ttf", 10f );
			AwesomeSolid = LoadFile( "fa-solid-900.ttf", 10f );
		}

		private static string SearchFontsFolder(string? start = null) 
		{
			var cur = new DirectoryInfo( start ?? Directory.GetCurrentDirectory() );
			
			while( cur!= null )
			{
				var dirs = cur.GetDirectories( "Fonts" );
				if( dirs.Length > 0 ) { return dirs[0].FullName; }
				cur = cur.Parent;
			};			

			return cur?.FullName ?? string.Empty;
		}

		private static unsafe ImFontPtr LoadEmbedded( string fileName, float pixelSize ) 
		{
			//https://github.com/ocornut/imgui/issues/220
			byte[] buffer = EmbeddedResource.Load( fileName );
			fixed( byte* p = buffer )
			{
				ImFontConfig font_cfg;
				font_cfg.FontDataOwnedByAtlas = 0;
				IntPtr ptr = (IntPtr)p;
				return ImGui.GetIO().Fonts.AddFontFromMemoryTTF( ptr, buffer.Length, pixelSize, &font_cfg );
			}
		}

		private static ImFontPtr LoadFile( string fileName, float pixelSize ) 
		{
			string path = Path.Combine( FontsFolder, fileName );
			return ImGui.GetIO().Fonts.AddFontFromFileTTF( path, pixelSize );
		}
	}
}
