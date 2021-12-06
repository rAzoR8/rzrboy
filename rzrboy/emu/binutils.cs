namespace emu
{
    public static class binutil
    {
        public static ushort Combine( this byte high, byte low ) { return (ushort)( ( high << 8 ) | low ); }
        public static void Split( this ushort val, out byte high, out byte low ) { high = (byte)( ( val & 0xff00 ) >> 8 ); low = (byte)( val & 0xff ); }

        public static byte GetLsb( this ushort val ) => (byte)( val & 0xff );
        public static byte GetMsb( this ushort val ) => (byte)( ( val & 0xff00 ) >> 8 );

        public static void SetLsb( this ushort target, byte lsb ) { target = (ushort)( ( target & 0xFF00 ) | lsb ); }
        public static void SetMsb( this ushort target, byte msb ) { target = (ushort)( ( target & 0x00FF ) | ( msb << 8 ) ); }

        public static bool IsBitSet( this byte value, byte bit ) { return ( value & ( 1 << bit ) ) != 0; }
        public static byte SetBit( this byte target,  byte index, bool value )
        {
            if ( value )
                target |= (byte)( 1 << index );
            else
                target &= (byte)~( 1 << index );
            return target;
        }

        public static bool IsBitSet( this ushort value, byte bit ) { return ( value & ( 1 << bit ) ) != 0; }
        public static ushort SetBit( this ushort target, byte index, bool value )
        {
            if ( value )
                target |= (byte)( 1 << index );
            else
                target &= (byte)~( 1 << index );
            return target;
        }
    }
}
