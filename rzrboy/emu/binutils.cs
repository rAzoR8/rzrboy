namespace rzr
{
    public static class Binutil
    {
        public static (byte low, byte high) Nibbles( this byte val ) => ((byte)( val & 0xF ), (byte)( val >> 4 ));
        public static ushort Combine( this byte msb, byte lsb ) { return (ushort)( ( msb << 8 ) | lsb ); }
        public static void Split( this ushort val, out byte msb, out byte lsb ) { msb = (byte)( ( val & 0xff00 ) >> 8 ); lsb = (byte)( val & 0xff ); }

        public static byte GetLsb( this ushort val ) => (byte)( val & 0xff );
        public static byte GetMsb( this ushort val ) => (byte)( ( val & 0xff00 ) >> 8 );

        public static ushort SetLsb( this ushort target, byte lsb ) { return (ushort)( ( target & 0xFF00 ) | lsb ); }
        public static void SetLsb( ref ushort target, byte lsb ) { target = (ushort)( ( target & 0xFF00 ) | lsb ); }
        public static ushort SetMsb( this ushort target, byte msb ) { return (ushort)( ( target & 0x00FF ) | ( msb << 8 ) ); }
        public static void SetMsb( ref ushort target, byte msb ) { target = (ushort)( ( target & 0x00FF ) | ( msb << 8 ) ); }

        public static bool IsBitSet( this byte value, byte bit ) { return ( value & ( 1 << bit ) ) != 0; }
		public static byte GetBit( this byte value, byte bit ) { return (byte)(( value & ( 1 << bit ) ) >> bit); }

		public static byte SetBit( this byte target, byte index, bool value )
		{
			if( value )
				target |= (byte)( 1 << index );
			else
				target &= (byte)~( 1 << index );
			return target;
		}

		public static byte SetBit( ref byte target, byte index, bool value )
        {
            if ( value )
                target |= (byte)( 1 << index );
            else
                target &= (byte)~( 1 << index );
            return target;
        }

		public static bool IsBitSet( this ushort value, byte bit ) { return ( value & ( 1 << bit ) ) != 0; }
		public static ushort GetBit( this ushort value, byte bit ) { return (ushort)( ( value & ( 1 << bit ) ) >> bit ); }

		public static ushort SetBit( ref ushort target, byte index, bool value )
        {
            if ( value )
                target |= (byte)( 1 << index );
            else
                target &= (byte)~( 1 << index );
            return target;
        }

		public static ushort SetBit( this ushort target, byte index, bool value )
		{
			if( value )
				target |= (byte)( 1 << index );
			else
				target &= (byte)~( 1 << index );
			return target;
		}

		public static byte Flip( this byte val ) => (byte)( ~val );
        public static ushort Flip( this ushort val ) => (ushort)( ~val );
    }
}
