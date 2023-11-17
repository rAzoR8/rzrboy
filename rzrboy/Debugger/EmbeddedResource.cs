using System.Reflection;

namespace dbg
{
	public class EmbeddedResource
	{
		private static Assembly assembly = typeof( EmbeddedResource ).Assembly;
		public static byte[] Load( string resourceName )
		{
			using( Stream s = assembly.GetManifestResourceStream( resourceName ) )
			{
				byte[] ret = new byte[s.Length];
				s.Read( ret, 0, (int)s.Length );
				return ret;
			}
		}
	}
}
