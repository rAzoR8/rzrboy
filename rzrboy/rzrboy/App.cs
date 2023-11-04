using Application = Microsoft.Maui.Controls.Application;

namespace rzrboy
{
	public class App : Application
	{
		private rzr.Boy gb = new();

		public App()
		{
			var page = new MainPage( gb );
			MainPage = page;

			string[] args = System.Environment.GetCommandLineArgs();
			string gameName = "PeliPoika.Game";

			for ( int i = 1; i < args.Length; i++ )
			{
				if( args[i - 1] == "-debug" )
				{
					gameName = args[i];
					break;
				}
			}

			if( gameName != null )
			{
				var game = System.Type.GetType( gameName, throwOnError: false );

				if( game == null ) { game = typeof( PeliPoika.Game ); }

				if( game != null && game.IsClass && !game.IsAbstract && game.IsSubclassOf( typeof( rzr.ModuleBuilder ) ) )
				{
					page.Debug( game );
				}
			}
		}
	}
}
