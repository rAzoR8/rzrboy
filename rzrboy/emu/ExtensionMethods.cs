namespace rzr
{
	public static class ExtensionMethods
	{
		public static IEnumerable<string> Names<T>( this IEnumerable<T> vals ) where T : System.Enum
		{
			return vals.Select( v => v.ToString() );
		}

		public static T[] EnumValues<T>() where T : struct, System.Enum
		{
			return Enum.GetValues<T>();
		}

		public static IEnumerable<T> EnumValues<T>( this T from ) where T : struct, System.Enum
		{
			return Enum.GetValues<T>().Where( v => v.CompareTo( from ) > -1 );
		}

		// (from, to) inclusive range from-to
		public static IEnumerable<T> EnumValues<T>( this T from, T to ) where T : struct, System.Enum
		{
			return Enum.GetValues<T>( ).Where( v => v.CompareTo( from ) > -1 && v.CompareTo( to ) <= 0 );
		}

		public static IEnumerable<T[]> Split<T>( this T[] arr, int size )
		{
			for( var i = 0; i < arr.Length / size + 1; i++ )
			{
				yield return arr.Skip( i * size ).Take( size ).ToArray();
			}
		}
	}
}
