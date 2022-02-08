using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using System.IO;
using Application = Microsoft.Maui.Controls.Application;

namespace rzrboy
{
	public class App : Application
	{
		private rzr.Boy gb = new();

		public App()
		{
			MainPage = new MainPage( gb );
        }
	}
}
