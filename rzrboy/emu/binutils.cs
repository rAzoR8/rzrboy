namespace emu
{
    public static class binutil
    {
        public static ushort Combine(byte high, byte low) { return (ushort)((high << 8) | low); }
        public static void Split(ushort val, out byte high, out byte low) { high = (byte)((val & 0xff00) >> 8); low = (byte)(val & 0xff); }

        public static byte lsb(ushort val) => (byte)(val & 0xff);
        public static byte msb(ushort val) => (byte)((val & 0xff00) >> 8);

        public static bool IsSet(byte value, byte flag) { return (value & flag) == flag; }
        public static byte SetBit(bool value, byte index, byte target)
        {
            if (value)
                target |= (byte)(1 << index);
            else
                target &= (byte)~(1 << index);
            return target;
        }
    }
}
