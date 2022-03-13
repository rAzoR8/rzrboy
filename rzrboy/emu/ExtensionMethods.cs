namespace rzr
{
	public static class ExtensionMethods
	{
		public static IEnumerable<string> Names<T>( this IEnumerable<T> vals ) where T : System.Enum
		{
			return vals.Select( v => v.ToString() );
		}

		public static IEnumerable<T> EnumValues<T>() where T : System.Enum
		{
			return Enum.GetValues( typeof( T ) ).Cast<T>();
		}

		public static IEnumerable<T> EnumValues<T>( this T from ) where T : System.Enum
		{
			return Enum.GetValues( typeof( T ) ).Cast<T>().Where( v => v.CompareTo( from ) > -1 );
		}

		// (from, to) inclusive ragne from-to
		public static IEnumerable<T> EnumValues<T>( this T from, T to ) where T : System.Enum
		{
			return Enum.GetValues( typeof( T ) ).Cast<T>().Where( v => v.CompareTo( from ) > -1 && v.CompareTo( to ) <= 0 );
		}
	}
}
