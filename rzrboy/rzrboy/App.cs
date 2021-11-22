using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace rzrboy
{
	public class App : Application
	{
		public App()
		{
			MainPage = new MainPage();
		}
	}
}
