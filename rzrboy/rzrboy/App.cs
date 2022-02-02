using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using System.IO;
using Application = Microsoft.Maui.Controls.Application;

namespace rzrboy
{
	public class App : Application
	{
		private const string CartPath = "C:\\Users\\razor\\Desktop\\roms\\game.gb";
		private const string BootRomPath = "C:\\Users\\razor\\Desktop\\roms\\boot.bin";
		private rzr.Boy gb;

		public App()
		{
			byte[] data;
			byte[]? boot = null;
			try
			{
				data = File.ReadAllBytes( CartPath );

				if( BootRomPath != null )
				{
					boot = File.ReadAllBytes( BootRomPath );
				}
			}
			catch( System.Exception )
			{
				data = new byte[0x8000];
			}

			gb = new rzr.Boy( data, boot );

			MainPage = new MainPage( gb );
        }
	}
}
