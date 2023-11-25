using System.Diagnostics;

namespace rzr
{
    public enum Reg8 : byte
    {
        A = 0, F, B, C, D, E, H, L
    }

    public enum Reg16 : byte
    {
       AF = Reg8.L + 1, BC, DE, HL, PC, SP
    }

    public enum RegX : byte
    {
        A = 0, F, B, C, D, E, H, L,
        AF, BC, DE, HL, PC, SP,
    }

    public static class RegExtensions
    {
        public static bool Is8( this Reg16 reg ) => (byte)reg <= (byte)Reg8.L;
        public static bool Is8( this RegX reg ) => (byte)reg <= (byte)Reg8.L;
        public static bool Is16( this RegX reg ) => (byte)reg >= (byte)Reg16.AF;

        public static Reg8 To8( this RegX reg ) => (Reg8)reg;
        public static Reg16 To16( this RegX reg ) => (Reg16)reg;
        public static Reg8 To8( this Reg16 reg ) => (Reg8)reg;
        public static Reg16 To16( this Reg8 reg ) => (Reg16)reg;
        public static RegX ToX( this Reg16 reg ) => (RegX)reg;
        public static RegX ToX( this Reg8 reg ) => (RegX)reg;

        public static Reg16 Fused( this Reg8 left, Reg8 right )
        {
            switch (left)
            {
                case Reg8.A when right == Reg8.F: return Reg16.AF;
				case Reg8.B when right == Reg8.C: return Reg16.BC;
				case Reg8.D when right == Reg8.E: return Reg16.DE;
				case Reg8.H when right == Reg8.L: return Reg16.HL;
                default: throw new ArgumentException( $"{left} can't be extended to 16 bit register with {right}" );
			}
		}

		public static RegView AsView(this IRegisters reg) => new RegView(reg);
    }

	public class RegView : IRegisters
	{
		private IRegisters m_reg;

		public RegView( IRegisters reg ) { m_reg = reg; }

		public byte A { get => m_reg.A; set => m_reg.A = value; }
		public byte F { get => m_reg.F; set => m_reg.F = value; }
		public byte B { get => m_reg.B; set => m_reg.B = value; }
		public byte C { get => m_reg.C; set => m_reg.C = value; }
		public byte D { get => m_reg.D; set => m_reg.D = value; }
		public byte E { get => m_reg.E; set => m_reg.E = value; }
		public byte H { get => m_reg.H; set => m_reg.H = value; }
		public byte L { get => m_reg.L; set => m_reg.L = value; }
		public ushort SP { get => m_reg.SP; set => m_reg.SP = value; }
		public ushort PC { get => m_reg.PC; set => m_reg.PC = value; }
		public IMEState IME { get => m_reg.IME; set => m_reg.IME = value; }
		public bool Halted { get => m_reg.Halted; set => m_reg.Halted = value; }

		public ushort AF { get { return (ushort)( ( A << 8 ) | F ); } set { A = (byte)( value >> 8 ); F = (byte)( value & 0xFF ); } } // TODO masking?
		public ushort BC { get { return (ushort)( ( B << 8 ) | C ); } set { B = (byte)( value >> 8 ); C = (byte)( value & 0xFF ); } }
		public ushort DE { get { return (ushort)( ( D << 8 ) | E ); } set { D = (byte)( value >> 8 ); E = (byte)( value & 0xFF ); } }
		public ushort HL { get { return (ushort)( ( H << 8 ) | L ); } set { H = (byte)( value >> 8 ); L = (byte)( value & 0xFF ); } }

		public bool Zero { get => F.IsBitSet( 7 ); set { F = Binutil.SetBit( F, 7, value ); } }
		public bool Sub { get => F.IsBitSet( 6 ); set { F = Binutil.SetBit( F, 6, value ); } }
		public bool HalfCarry { get => F.IsBitSet( 5 ); set { F = F = Binutil.SetBit( F, 5, value ); } }
		public bool Carry { get => F.IsBitSet( 4 ); set { F = Binutil.SetBit( F, 4, value ); } }

		public void SetFlags( bool Z, bool N, bool H, bool C )
		{
			Zero = Z;
			Sub = N;
			HalfCarry = H;
			Carry = C;
		}

		public Reg Clone() => new Reg
		{
			F = this.F,
			A = this.A,
			B = this.B,
			C = this.C,
			D = this.D,
			E = this.E,
			H = this.H,
			L = this.L,
			SP = this.SP,
			PC = this.PC,
			IME = this.IME,
			Halted = this.Halted
		};

		public byte this[Reg8 type]
		{
			get
			{
				switch( type )
				{
					case Reg8.A: return A;
					case Reg8.F: return F;
					case Reg8.B: return B;
					case Reg8.C: return C;
					case Reg8.D: return D;
					case Reg8.E: return E;
					case Reg8.H: return H;
					case Reg8.L: return L;
					default: throw new ArgumentOutOfRangeException( "type" );
				}
			}
			set
			{
				switch( type )
				{
					case Reg8.A: A = value; break;
					case Reg8.F: F = value; break;
					case Reg8.B: B = value; break;
					case Reg8.C: C = value; break;
					case Reg8.D: D = value; break;
					case Reg8.E: E = value; break;
					case Reg8.H: H = value; break;
					case Reg8.L: L = value; break;
					default: throw new ArgumentOutOfRangeException( "type" );
				}
			}
		}

		public ushort this[Reg16 type]
		{
			get
			{
				switch( type )
				{
					case Reg16.AF: return AF;
					case Reg16.BC: return BC;
					case Reg16.DE: return DE;
					case Reg16.HL: return HL;
					case Reg16.SP: return SP;
					case Reg16.PC: return PC;
					default: throw new ArgumentOutOfRangeException( "type" );
				}
			}
			set
			{
				switch( type )
				{
					case Reg16.AF: AF = value; break;
					case Reg16.BC: BC = value; break;
					case Reg16.DE: DE = value; break;
					case Reg16.HL: HL = value; break;
					case Reg16.SP: SP = value; break;
					case Reg16.PC: PC = value; break;
					default: throw new ArgumentOutOfRangeException( "type" );
				}
			}
		}

		public override string ToString()
		{
			return $"AF={AF:X4}, BC={BC:X4}, DE={DE:X4}, HL={HL:X4}, SP={SP:X4}, PC={PC:X4}, Z={Zero}, N={Sub}, H={HalfCarry}, C={Carry}";
		}
	}

    public class Reg : IRegisters, IState
    {
        private byte _flags;       
        
        public byte A { get; set; }
		public byte F { get => _flags; set => _flags = (byte)(value & FlagMask8); }

		public byte B { get; set; }
		public byte C { get; set; }
		public byte D { get; set; }
		public byte E { get; set; }
		public byte H { get; set; }
		public byte L { get; set; }

		public ushort SP { get; set; }
		public ushort PC { get; set; }

		public IMEState IME { get; set; } = IMEState.Disabled;
        public bool Halted { get; set; } = false;
        //public bool Stopped = false;

		public byte[] Save() 
		{
			byte[] regs = new byte[14];
			regs[0] = A; regs[1] = _flags;
			regs[2] = B; regs[3] = C;
			regs[4] = D; regs[5] = E;
			regs[6] = H; regs[7] = L;
			regs[8] = SP.GetLsb(); regs[9] = SP.GetMsb();
			regs[10] = PC.GetLsb(); regs[11] = PC.GetMsb();
			regs[12] = (byte)IME; regs[13] = (byte)(Halted ? 1 : 0);
			// TODO: save IE 0xFFFF here?
			return regs;
		}

		public void Load( byte[] regs ) 
		{
			Debug.Assert( regs.Length >= 14 );
			A = regs[0]; _flags = regs[1];
			B = regs[2]; C = regs[3];
			D = regs[4]; E = regs[5];
			H = regs[6]; L = regs[7];
			SP = regs[9].Combine( lsb: regs[8] );
			PC = regs[11].Combine( lsb: regs[10] );
			IME = (IMEState)regs[12]; Halted = regs[13] == 1;
		}

		//public static Reg DMG() { return new Reg() { AF = 0x01B0, BC = 0x0013, DE = 0x00D8, HL = 0x014D, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg MGB() { return new Reg() { AF = 0xFFB0, BC = 0x0013, DE = 0x00D8, HL = 0x014D, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg SGB() { return new Reg() { AF = 0x0100, BC = 0x0014, DE = 0x0000, HL = 0xC060, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg CBG() { return new Reg() { AF = 0x1180, BC = 0x0000, DE = 0x0008, HL = 0x007C, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg AGB() { return new Reg() { AF = 0x1100, BC = 0x0100, DE = 0x0008, HL = 0x007C, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg AGS() { return new Reg() { AF = 0x1100, BC = 0x0100, DE = 0x0008, HL = 0x007C, SP = 0xFFFE, PC = 0x0100 }; }

		//// GBC modes
		//public static Reg CBG_GBC() { return new Reg() { AF = 0x1180, BC = 0x0000, DE = 0xFF56, HL = 0x000D, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg AGB_GBC() { return new Reg() { AF = 0x1100, BC = 0x0100, DE = 0xFF56, HL = 0x000D, SP = 0xFFFE, PC = 0x0100 }; }
		//public static Reg AGS_GBC() { return AGB_GBC(); }

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
