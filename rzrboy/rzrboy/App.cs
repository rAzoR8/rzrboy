using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace rzrboy
{
	public class App : Application
	{
		private rzr.Boy gb = new rzr.Boy( "C:\\Users\\razor\\Desktop\\roms\\game.gb", "C:\\Users\\razor\\Desktop\\roms\\boot.bin" );

		public App()
		{
            MainPage = new MainPage( gb );
        }
	}
}
