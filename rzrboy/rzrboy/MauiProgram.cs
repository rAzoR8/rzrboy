using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.LifecycleEvents;

namespace rzrboy
{
	public static class MauiProgram
	{
		public static MauiApp CreateMauiApp()
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts( fonts =>
				 {
					 fonts.AddFont( "iosevka-regular.ttf", Font.Regular );
					 fonts.AddFont( "iosevka-bold.ttf", Font.Bold );
					 fonts.AddFont( "iosevka-light.ttf", Font.Light );
					 fonts.AddFont( "iosevka-medium.ttf", Font.Medium );
					 fonts.AddFont( "iosevka-thin.ttf", Font.Thin );
				 } )
				.ConfigureLifecycleEvents( lifecycle =>
				{
#if __IOS__
					lifecycle
						.AddiOS(iOS => iOS
							.OpenUrl((app, url, options) =>
								Microsoft.Maui.Essentials.Platform.OpenUrl(app, url, options))
							.ContinueUserActivity((application, userActivity, completionHandler) =>
								Microsoft.Maui.Essentials.Platform.ContinueUserActivity(application, userActivity, completionHandler))
							.PerformActionForShortcutItem((application, shortcutItem, completionHandler) =>
								Microsoft.Maui.Essentials.Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler)));
#elif WINDOWS
					lifecycle
						.AddWindows(windows =>
						{
							windows
								.OnLaunched((app, e) =>
									Microsoft.Maui.Essentials.Platform.OnLaunched(e));
							windows
								.OnActivated((window, e) =>
									Microsoft.Maui.Essentials.Platform.OnActivated(window, e));
						});
#elif ANDROID
					//lifecycle.AddAndroid(d => {
					//	d.OnBackPressed(activity => {
					//		System.Diagnostics.Debug.WriteLine("Back button pressed!");
					//		return true;
					//	});
					//});
#endif
				} );

			builder.Services.AddSingleton<App>();

			builder.Services.AddTransient<MainPage>();
			//builder.Services.AddTransient<MainViewModel>();

			return builder.Build();
		}
	}
}