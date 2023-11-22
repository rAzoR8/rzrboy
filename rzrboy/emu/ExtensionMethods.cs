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
			if( arr.Length <= size )
			{
				yield return arr;
			}
			else
			{
				int cur = 0;
				while( cur < arr.Length )
				{
					yield return arr.Skip( cur ).Take( size ).ToArray();
					cur += size;
					int remainder = arr.Length - cur;
					size = remainder >= size ? size : remainder; 
				}
			}
		}
	}
}
