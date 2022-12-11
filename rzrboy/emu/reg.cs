namespace rzr
{
    public enum Reg8 : int
    {
        A = 0, F, B, C, D, E, H, L
    }

    public enum Reg16 : int
    {
       AF = Reg8.L + 1, BC, DE, HL, PC, SP
    }

    public enum RegX : int
    {
        A = 0, F, B, C, D, E, H, L,
        AF, BC, DE, HL, PC, SP,
    }

    static class RegExtensions
    {
        public static bool Is8(this Reg16 reg) => (int)reg <= (int)Reg8.L;
        public static bool Is8(this RegX reg) => (int)reg <= (int)Reg8.L;
        public static bool Is16(this RegX reg) => (int)reg >= (int)Reg16.AF;

        public static Reg8 To8( this RegX reg ) => (Reg8)reg;
        public static Reg16 To16( this RegX reg ) => (Reg16)reg;
        public static Reg8 To8( this Reg16 reg ) => (Reg8)reg;
        public static Reg16 To16( this Reg8 reg ) => (Reg16)reg;
        public static RegX ToX( this Reg16 reg ) => (RegX)reg;
        public static RegX ToX( this Reg8 reg ) => (RegX)reg;
    }

    public enum IMEState
	{
        Disabled,
        Enabled,
        RequestEnabled
	}

    public class Reg
    {
        private byte _flags;       

        // REGISTERS
        public byte A; public byte F { get => _flags; set => _flags = (byte)(value & FlagMask8); }
        public byte B, C;
        public byte D, E;
        public byte H, L;
        public ushort SP;
        public ushort PC;

        public ushort AF { get { return A.Combine( F ); } set { binutil.Split( (ushort)( value & FlagMask16 ), out A, out _flags ); } }
        public ushort BC { get { return B.Combine( C ); } set { binutil.Split( value, out B, out C ); } }
        public ushort DE { get { return D.Combine( E ); } set { binutil.Split( value, out D, out E ); } }
        public ushort HL { get { return H.Combine( L ); } set { binutil.Split( value, out H, out L ); } }

        public bool Zero { get => _flags.IsBitSet(7); set { binutil.SetBit( ref _flags, 7, value ); } }
        public bool Sub { get => _flags.IsBitSet( 6 ); set { binutil.SetBit( ref _flags, 6, value ); } }
        public bool HalfCarry { get => _flags.IsBitSet( 5 ); set { binutil.SetBit( ref _flags, 5, value); } }
        public bool Carry { get => _flags.IsBitSet( 4 ); set { binutil.SetBit( ref _flags, 4, value); } }

        public IMEState IME = IMEState.Disabled;

        public bool Halted = false;
        //public bool Stopped = false;

        public void SetFlags( bool Z, bool N, bool H, bool C )
        {
            Zero = Z;
            Sub = N;
            HalfCarry = H;
            Carry = C;
        }

        public byte this[Reg8 type]
        {
            get
            {
                switch (type)
                {
                    case Reg8.A: return A;
                    case Reg8.F: return F;
                    case Reg8.B: return B;
                    case Reg8.C: return C;
                    case Reg8.D: return D;
                    case Reg8.E: return E;
                    case Reg8.H: return H;
                    case Reg8.L: return L;
                    default: throw new ArgumentOutOfRangeException("type");
                }
            }
            set
            {
                switch (type)
                {
                    case Reg8.A: A = value; break;
                    case Reg8.F: F = value; break;
                    case Reg8.B: B = value; break;
                    case Reg8.C: C = value; break;
                    case Reg8.D: D = value; break;
                    case Reg8.E: E = value; break;
                    case Reg8.H: H = value; break;
                    case Reg8.L: L = value; break;
                    default: throw new ArgumentOutOfRangeException("type");
                }
            }
        }

        public ushort this[Reg16 type]
        {
            get
            {
                switch (type)
                {
                    case Reg16.AF: return AF;
                    case Reg16.BC: return BC;
                    case Reg16.DE: return DE;
                    case Reg16.HL: return HL;
                    case Reg16.SP: return SP;
                    case Reg16.PC: return PC;
                    default: throw new ArgumentOutOfRangeException("type");
                }
            }
            set
            {
                switch (type)
                {
                    case Reg16.AF: AF = value; break;
                    case Reg16.BC: BC = value; break;
                    case Reg16.DE: DE = value; break;
                    case Reg16.HL: HL = value; break;
                    case Reg16.SP: SP = value; break;
                    case Reg16.PC: PC = value; break;
                    default: throw new ArgumentOutOfRangeException("type");
                }
            }
        }

        public override string ToString()
        {
            return $"AF={AF:X4}, BC={BC:X4}, DE={DE:X4}, HL={HL:X4}, SP={SP:X4}, PC={PC:X4}, Z={Zero}, N={Sub}, H={HalfCarry}, C={Carry}";
        }

        public static Reg DMG() { return new Reg() { AF = 0x01B0, BC = 0x0013, DE = 0x00D8, HL = 0x014D, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg MGB() { return new Reg() { AF = 0xFFB0, BC = 0x0013, DE = 0x00D8, HL = 0x014D, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg SGB() { return new Reg() { AF = 0x0100, BC = 0x0014, DE = 0x0000, HL = 0xC060, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg CBG() { return new Reg() { AF = 0x1180, BC = 0x0000, DE = 0x0008, HL = 0x007C, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg AGB() { return new Reg() { AF = 0x1100, BC = 0x0100, DE = 0x0008, HL = 0x007C, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg AGS() { return new Reg() { AF = 0x1100, BC = 0x0100, DE = 0x0008, HL = 0x007C, SP = 0xFFFE, PC = 0x0100 }; }
        
        // GBC modes
        public static Reg CBG_GBC() { return new Reg() { AF = 0x1180, BC = 0x0000, DE = 0xFF56, HL = 0x000D, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg AGB_GBC() { return new Reg() { AF = 0x1100, BC = 0x0100, DE = 0xFF56, HL = 0x000D, SP = 0xFFFE, PC = 0x0100 }; }
        public static Reg AGS_GBC() { return AGB_GBC(); }

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
    }
}
