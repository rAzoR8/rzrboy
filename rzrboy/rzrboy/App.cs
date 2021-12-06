using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace rzrboy
{
	public class App : Application
	{
		//private emu.gb gb = new emu.gb( new byte[0x8000] /*"game.rom"*/);
		private rzr.Gb gb = new rzr.Gb( "C:\\Users\\razor\\Desktop\\roms\\game.gb" );

		public App()
		{
            MainPage = new MainPage( gb );
        }
	}
}
