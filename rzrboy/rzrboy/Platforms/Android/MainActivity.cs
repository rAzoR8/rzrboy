﻿using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Microsoft.Maui;
using Microsoft.Maui.Essentials;

namespace rzrboy
{
	[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
	public class MainActivity : MauiAppCompatActivity
	{
		protected override void OnCreate( Bundle savedInstanceState )
		{
			base.OnCreate( savedInstanceState );

			Platform.Init( this, savedInstanceState );
		}

		public override void OnRequestPermissionsResult( int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults )
		{
			Platform.OnRequestPermissionsResult( requestCode, permissions, grantResults );
			base.OnRequestPermissionsResult( requestCode, permissions, grantResults );
		}
	}
}