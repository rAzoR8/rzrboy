using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace rzrboy
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont( "iosevka-regular.ttf", Font.Regular );
					fonts.AddFont( "iosevka-bold.ttf", Font.Bold );
					fonts.AddFont( "iosevka-light.ttf", Font.Light );
					fonts.AddFont( "iosevka-medium.ttf", Font.Medium );
					fonts.AddFont( "iosevka-thin.ttf", Font.Thin );
				} );

			builder.Services.AddSingleton<App>();

			builder.Services.AddTransient<MainPage>();
			//builder.Services.AddTransient<MainViewModel>();

			return builder.Build();
		}
	}
}