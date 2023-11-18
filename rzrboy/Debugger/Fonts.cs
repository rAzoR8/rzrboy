using System.Runtime.InteropServices;
using ImGuiNET;

namespace dbg.ui
{
	public static class Fonts
	{
		public static string FontsFolder { get; set; } = SearchFontsFolder(); 

		public static ImFontPtr MonaspaceNeon { get; private set; }
		public static ImFontPtr Icons { get; private set; }

		public static void Push( this ImFontPtr fontPtr ) => ImGui.PushFont( fontPtr );
		public static void Pop() => ImGui.PopFont();

		private static GCHandle Awesome6Range = GCHandle.Alloc(new ushort[]{ IconFonts.FontAwesome6.IconMin, IconFonts.FontAwesome6.IconMax, 0 }, GCHandleType.Pinned);

		public static void Init() 
		{
			unsafe
			{
				ImFontConfigPtr config;
				var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
				config = new ImFontConfigPtr( nativeConfig );
				config.SizePixels = 13f;
				config.OversampleH = config.OversampleV = 1;
				config.PixelSnapH = true;
				config.MergeMode = true;

				MonaspaceNeon = LoadEmbedded( "monaspace-neon.ttf", config );

				config.GlyphMinAdvanceX = 13.0f;
				config.SizePixels *= 2f/3f;
				Icons = LoadEmbedded( "fa-solid-900.ttf", config, glyphRange: Awesome6Range.AddrOfPinnedObject() );
			}
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

		private static unsafe ImFontPtr LoadEmbedded( string fileName, ImFontConfigPtr config, IntPtr? glyphRange = null) 
		{
			//https://github.com/ocornut/imgui/issues/220
			byte[] buffer = EmbeddedResource.Load( fileName );
			fixed( byte* p = buffer )
			{
				if (glyphRange != null)
					return ImGui.GetIO().Fonts.AddFontFromMemoryTTF((IntPtr)p, buffer.Length, config.SizePixels, config, glyphRange.Value);
				else
					return ImGui.GetIO().Fonts.AddFontFromMemoryTTF((IntPtr)p, buffer.Length, config.SizePixels, config);
			}
		}

		private unsafe static ImFontPtr LoadFile( string fileName, ImFontConfigPtr config, IntPtr? glyphRange = null ) 
		{
			string path = Path.Combine( FontsFolder, fileName );
			if(glyphRange!= null)
				return ImGui.GetIO().Fonts.AddFontFromFileTTF( path, config.SizePixels, config, glyphRange.Value );
			else
				return ImGui.GetIO().Fonts.AddFontFromFileTTF( path, config.SizePixels, config );
		}
	}
}
