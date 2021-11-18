namespace emu
{
    public static class binutil
    {
        public static short Combine(sbyte high, sbyte low) { return (short)((high << 8) | low); }
        public static void Split(short val, out sbyte high, out sbyte low) { high = (sbyte) ((val & 0xff00) >> 8); low = (sbyte)(val & 0xff); }

        public static ushort Combine(byte high, byte low) { return (ushort)((high << 8) | low); }
        public static void Split(ushort val, out byte high, out byte low) { high = (byte)((val & 0xff00) >> 8); low = (byte)(val & 0xff); }

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

    internal class registers
    {
        public const byte FlagMask8 = 0b1111_0000;
        public const ushort FlagMask16 = 0b11111111_11110000;

        public const byte ZFlagMask8 = 0b10000000; // ZERO
        public const byte ZFlagShift8 = 7;
        public const byte NFlagMask8 = 0b01000000; // BCD SUBTRACTION
        public const byte NFlagShift8 = 6;
        public const byte HFlagMask8 = 0b00100000; // BCD HALF CARRY
        public const byte HFlagShift8 = 5;
        public const byte CFlagMask8 = 0b00010000; // CARRY
        public const byte CFlagShift8 = 4;

        // REGISTERS
        public byte A, F;
        public byte B, C;
        public byte D, E;
        public byte H, L;
        public ushort SP;
        public ushort PC;

        public bool Zero { get => binutil.IsSet(F, ZFlagMask8); set { F = binutil.SetBit(value, 7, F); } }
        public bool Sub { get => binutil.IsSet(F, ZFlagMask8); set { F = binutil.SetBit(value, 6, F); } }
        public bool HalfCarry { get => binutil.IsSet(F, ZFlagMask8); set { F = binutil.SetBit(value, 5, F); } }
        public bool Carry { get => binutil.IsSet(F, ZFlagMask8); set { F = binutil.SetBit(value, 5, F); } }

        public ushort AF { get { return binutil.Combine(A, F); } set { binutil.Split((ushort)(value & FlagMask16), out A, out F); } }
        public ushort BC { get { return binutil.Combine(B, C); } set { binutil.Split(value, out B, out C); } }
        public ushort DE { get { return binutil.Combine(D, E); } set { binutil.Split(value, out D, out E); } }
        public ushort HL { get { return binutil.Combine(H, L); } set { binutil.Split(value, out H, out L); } }

        public override string ToString()
        {
            return $"AF={AF:X4}, BC={BC:X4}, DE={DE:X4}, HL={HL:X4}, SP={SP:X4}, PC={PC:X4}, Z={Zero}, N={Sub}, H={HalfCarry}, C={Carry}";
        }
    }
}
