namespace rzr
{
	public static class CpuOps
	{
		/// <summary>
		/// OPERATIONS
		/// </summary>

		public static readonly CpuOp Nop = ( reg, mem ) => { };

		public static readonly CpuOp Halt = ( reg, mem ) =>
		{
			byte IF = mem[0xFF0F];
			byte IE = mem[0xFFFF];

			bool haltBug = ( IF & IE & 0x1F ) != 0 && reg.IME != IMEState.Enabled;
			if( haltBug )
			{
				reg.PC--;
			}

			reg.Halted = true;
		};

		public static IEnumerable<CpuOp> Stop()
		{
			byte IE = 0; byte IF = 0; bool IME = false;

			yield return ( reg, mem ) =>
			{
				reg.Halted = true;
				IME = reg.IME == IMEState.Enabled;
				IF = mem[0xFF0F];
				IE = mem[0xFFFF];
			};

			// TODO:
			// https://gbdev.io/pandocs/Reducing_Power_Consumption.html#using-the-stop-instruction
			// ASSERT:
			// On a DMG, disabling the LCD before invoking STOP leaves the LCD enabled, drawing a horizontal black line on the screen and very likely damaging the hardware.
		}

		// read next byte from mem[pc++], 2 m-cycles
		private static IEnumerable<CpuOp> LdImm8( Reg8 target )
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg.PC++;
			yield return ( reg, mem ) => reg[target] = mem[address];
		}

		// read two bytes from instruction stream, write to 16bit reg: 3 m-cycles
		private static IEnumerable<CpuOp> LdImm16( Reg16 target )
		{
			ushort val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => val |= (ushort)( mem[reg.PC++] << 8 );
			yield return ( reg, mem ) => reg[target] = val;
		}

		public static IEnumerable<CpuOp> LdImm( RegX target ) => target.Is8() ? LdImm8( target.To8() ) : LdImm16( target.To16() );

		// LD r8, r8' 1-cycle
		private static IEnumerable<CpuOp> LdReg8( Reg8 dst, Reg8 src )
		{
			yield return ( reg, mem ) => { reg[dst] = reg[src]; };
		}

		// LD r16, r16' 2-cycles
		private static IEnumerable<CpuOp> LdReg16( Reg16 dst, Reg16 src )
		{
			// simulate 16 bit register being written in two cycles
			yield return ( reg, mem ) => reg[dst] = Binutil.SetLsb( reg[dst], reg[src].GetLsb() );
			yield return ( reg, mem ) => reg[dst] = Binutil.SetMsb( reg[dst], reg[src].GetMsb() );
		}

		// LD r8, (r16) 2-cycle
		private static IEnumerable<CpuOp> LdAddr( Reg8 dst, Reg16 src_addr )
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg[src_addr];
			yield return ( reg, mem ) => reg[dst] = mem[address];
		}

		// LD (r16), r8 2-cycle
		private static IEnumerable<CpuOp> LdAddr( Reg16 dst_addr, Reg8 src )
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg[dst_addr];
			yield return ( reg, mem ) => mem[address] = reg[src];
		}

		public static IEnumerable<CpuOp> LdRegOrAddr( RegX dst, RegX src )
		{
			if( dst.Is8() && src.Is8() ) return LdReg8( dst.To8(), src.To8() );
			if( dst.Is16() && src.Is16() ) return LdReg16( dst.To16(), src.To16() );
			if( dst.Is8() && src.Is16() ) return LdAddr( dst.To8(), src.To16() );
			return LdAddr( dst.To16(), src.To8() );
		}

		// LD (HL+), A
		public static IEnumerable<CpuOp> LdHlPlusA()
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg.HL++;
			yield return ( reg, mem ) => mem[address] = reg.A;
		}

		// LD (HL-), A
		public static IEnumerable<CpuOp> LdHlMinusA()
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg.HL--;
			yield return ( reg, mem ) => mem[address] = reg.A;
		}

		// LD A, (HL+)
		public static IEnumerable<CpuOp> LdAHlPlus()
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg.HL++;
			yield return ( reg, mem ) => reg.A = mem[address];
		}

		// LD A, (HL-)
		public static IEnumerable<CpuOp> LdAHlMinus()
		{
			ushort address = 0;
			yield return ( reg, mem ) => address = reg.HL--;
			yield return ( reg, mem ) => reg.A = mem[address];
		}

		// LD A, (0xFF00+C)
		public static IEnumerable<CpuOp> LdhAc()
		{
			ushort address = 0xFF00;
			yield return ( reg, mem ) => { address += reg.C; };
			yield return ( reg, mem ) => { reg.A = mem[address]; };
		}

		// LD (0xFF00+C), A
		public static IEnumerable<CpuOp> LdhCa()
		{
			ushort address = 0xFF00;
			yield return ( reg, mem ) => { address += reg.C; };
			yield return ( reg, mem ) => { mem[address] = reg.A; };
		}

		// LD A, (0xFF00+db8)
		public static IEnumerable<CpuOp> LdhAImm()
		{
			byte lsb = 0; ushort address = 0xFF00;
			yield return ( reg, mem ) => lsb = mem[reg.PC++];
			yield return ( reg, mem ) => address += lsb;
			yield return ( reg, mem ) => reg.A = mem[address];
		}

		// LD (0xFF00+db8), A
		public static IEnumerable<CpuOp> LdhImmA()
		{
			byte lsb = 0; ushort address = 0xFF00;
			yield return ( reg, mem ) => lsb = mem[reg.PC++];
			yield return ( reg, mem ) => { address += lsb; };
			yield return ( reg, mem ) => { mem[address] = reg.A; };
		}

		// LD (a16), SP
		public static IEnumerable<CpuOp> LdImm16Sp()
		{
			ushort nn = 0;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			yield return ( reg, mem ) => mem[nn] = reg.SP.GetLsb();
			yield return ( reg, mem ) => mem[++nn] = reg.SP.GetMsb();
		}

		// LD HL,SP + r8 - 3 cycles
		public static IEnumerable<CpuOp> LdHlSpR8()
		{
			byte rhs = 0; ushort res = 0;
			yield return ( reg, mem ) => rhs = mem[reg.PC++];
			yield return ( reg, mem ) => res = SignedAddHelper( reg, reg.SP, (sbyte)rhs );
			yield return ( reg, mem ) => reg.HL = res;
		}

		// LD (a16), A - 4 cycles
		public static IEnumerable<CpuOp> LdImmAddrA( )
		{
			ushort nn = 0;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			yield return Nop;
			yield return ( reg, mem ) => mem[nn] = reg.A;
		}

		// LD A, (a16) - 4 cycles
		public static IEnumerable<CpuOp> LdAImmAddr()
		{
			ushort nn = 0;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			yield return Nop;
			yield return ( reg, mem ) => reg.A = mem[nn];
		}

		private static void AddHelper( RegView reg, byte rhs, byte carry = 0 )
		{
			carry = (byte)( carry != 0 && reg.Carry ? 1 : 0 );

			ushort acc = reg.A;
			reg.Sub = false;
			reg.HalfCarry = ( rhs & 0b1111 ) + ( acc & 0b1111 ) + carry > 0b1111;
			acc += rhs;
			acc += carry;
			reg.Carry = acc > 0xFF;
			reg.Zero = ( reg.A = (byte)acc ) == 0;
		}

		// ADD|ADC A, [r8, (HL)] 1-2 cycles
		public static IEnumerable<CpuOp> Add( RegX src, byte carry = 0 )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( src.Is8() ) val = reg[src.To8()];
				AddHelper( reg, val, carry );
			};
		}

		private static ushort SignedAddHelper( RegView reg, ushort lhs, sbyte rhs )
		{
			var res = lhs + rhs;

			reg.Sub = false;
			reg.Zero = false; // Accumulator not used
			reg.Carry = ( res & 0b1_0000_0000 ) != 0;
			reg.HalfCarry = ( res & 0b1_0000 ) != 0;

			return (ushort)( res );
		} 

		// ADD SP, R8 - 4 cycles
		public static IEnumerable<CpuOp> AddSpR8()
		{
			byte rhs = 0; ushort res = 0;
			yield return ( reg, mem ) => rhs = mem[reg.PC++];
			yield return ( reg, mem ) => res = SignedAddHelper( reg, reg.SP, (sbyte)rhs );
			yield return Nop; // no idea why this is a 4 cycle op
			yield return ( reg, mem ) => reg.SP = res;
		}

		// ADD A, db8 2-cycle
		public static IEnumerable<CpuOp> AddImm8( byte carry = 0 )
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => AddHelper( reg, val, carry );
		}

		// ADD HL, r16 2 cycles
		public static IEnumerable<CpuOp> AddHl( Reg16 src )
		{
			yield return Nop;
			yield return ( RegView reg, ISection mem ) =>
			{
				ushort l = reg.HL;
				ushort r = reg[src];
				reg.Sub = false;
				reg.HalfCarry = ( l & 0x0FFF ) + ( r & 0x0FFF ) > 0x0FFF;
				reg.Carry = l + r > 0xFFFF;
				reg.HL += r;
			};
		}

		public static void SubHelper( RegView reg, byte rhs, byte carry = 0 )
		{
			carry = (byte)( carry != 0 && reg.Carry ? 1 : 0 );

			ushort acc = reg.A;
			reg.Sub = true;
			if( carry == 0 ) reg.HalfCarry = ( rhs & 0b1111 ) > ( acc & 0b1111 );
			reg.Carry = rhs > acc;
			acc -= rhs;
			acc -= carry;
			if( carry != 0 ) reg.HalfCarry = ( ( reg.A ^ rhs ^ ( acc & 0xFF ) ) & ( 1 << 4 ) ) != 0;
			reg.Zero = ( reg.A = (byte)acc ) == 0;
		}

		// SUB|SBC A, [r8, (HL)] 1-2 cycles
		public static IEnumerable<CpuOp> Sub( RegX src, byte carry = 0 )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( src.Is8() ) val = reg[src.To8()];
				SubHelper( reg, val, carry );
			};
		}

		// SUB|SBC A, db8 2-cycle
		public static IEnumerable<CpuOp> SubImm8( byte carry = 0 )
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => SubHelper( reg, val, carry );
		}

		// AND A, [r8, (HL)] 1-2 -cycle
		public static IEnumerable<CpuOp> And( RegX src )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( RegView reg, ISection mem ) =>
			{
				if( src.Is8() ) val = reg[src.To8()];
				reg.SetFlags( Z: ( reg.A &= val ) == 0, N: false, H: true, C: false );
			};
		}

		// AND A, db8 2-cycle
		public static IEnumerable<CpuOp> AndImm8()
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => reg.SetFlags( Z: ( reg.A &= val ) == 0, N: false, H: true, C: false );
		}

		// Or A, [r8, (HL)] 1-2 -cycle
		public static IEnumerable<CpuOp> Or( RegX src )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( src.Is8() ) val = reg[src.To8()];
				reg.SetFlags( Z: ( reg.A |= val ) == 0, N: false, H: false, C: false );
			};
		}

		// Or A, db8 2-cycle
		public static IEnumerable<CpuOp> OrImm8()
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => reg.SetFlags( Z: ( reg.A |= val ) == 0, N: false, H: false, C: false );
		}

		public static void CpHelper( RegView reg, byte rhs )
		{
			var res = reg.A - rhs;
			reg.Zero = (byte)res == 0;
			reg.Sub = true;
			reg.HalfCarry = ( rhs & 0b1111 ) > ( reg.A & 0b1111 );
			reg.Carry = res < 0;
		}

		// CP A, [r8, (HL)] 1-2 -cycle
		public static IEnumerable<CpuOp> Cp( RegX src )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( src.Is8() ) val = reg[src.To8()];
				CpHelper( reg, val );
			};
		}

		// Or A, db8 2-cycle
		public static IEnumerable<CpuOp> CpImm8()
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => CpHelper( reg, val );
		}

		// JP HL 1 cycle
		public static readonly CpuOp JpHl = ( reg, mem ) => { reg.PC = reg.HL; };

		public delegate bool Condition( RegView reg );
		public readonly static Condition NZ = ( RegView reg ) => !reg.Zero;
		public readonly static Condition Z = ( RegView reg ) => reg.Zero;
		public readonly static Condition NC = ( RegView reg ) => !reg.Carry;
		public readonly static Condition C = ( RegView reg ) => reg.Carry;

		// JP cc, a16 3/4 cycles
		public static IEnumerable<CpuOp> JpImm16( Condition? cc = null )
		{
			ushort nn = 0; bool takeBranch = true;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			if( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
			if( takeBranch )
			{
				yield return ( reg, mem ) => { reg.PC = nn; };
			}
		}

		// JR cc, e8 2/3 ycles
		public static IEnumerable<CpuOp> JrImm( Condition? cc = null )
		{
			byte offset = 0; bool takeBranch = true;
			yield return ( reg, mem ) => offset = mem[reg.PC++];
			if( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
			if( takeBranch )
			{
				yield return ( reg, mem ) => reg.PC = (ushort)( reg.PC + (sbyte)offset );
			}
		}

		// XOR A, [r8, (HL)]  1-2 cycles
		public static IEnumerable<CpuOp> Xor( RegX src )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( src.Is8() ) { val = reg[src.To8()]; }
				reg.SetFlags( ( reg.A ^= val ) == 0, false, false, false );
			};
		}

		// XOR A, [r8, (HL)]  1-2 cycles
		public static IEnumerable<CpuOp> XorImm8()
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.PC++];
			yield return ( reg, mem ) => reg.SetFlags( ( reg.A ^= val ) == 0, false, false, false );
		}

		// BIT i, [r8, (HL)] 1-2 -cycle
		public static IEnumerable<CpuOp> Bit( byte bit, RegX src )
		{
			byte val = 0;
			if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( src.Is8() ) val = reg[src.To8()];
				reg.Zero = !val.IsBitSet( bit );
				reg.Sub = false;
				reg.HalfCarry = true;
			};
		}

		public static IEnumerable<CpuOp> Set( byte bit, RegX target )
		{
			byte val = 0;
			if( target.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( target.Is8() ) val = reg[target.To8()];
				val |= (byte)( 1 << bit );
			};
			if( target.Is16() ) yield return ( reg, mem ) => mem[reg.HL] = val;
		}

		public static IEnumerable<CpuOp> Res( byte bit, RegX target )
		{
			byte val = 0;
			if( target.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) =>
			{
				if( target.Is8() ) val = reg[target.To8()];
				val &= (byte)~( 1 << bit );
			};
			if( target.Is16() ) yield return ( reg, mem ) => mem[reg.HL] = val;
		}

		// INC r16: 16bit alu op => 2 cycles
		public static IEnumerable<CpuOp> Inc( Reg16 dst )
		{
			yield return Nop;
			yield return ( reg, mem ) => { reg[dst] += 1; };
		}

		private static byte Inc8Helper( byte val, RegView reg )
		{
			byte res = (byte)( val + 1 );
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = ( val & 0b1111 ) == 0b1111;
			return res;
		}

		// INC r8: 1 cycle
		public static CpuOp Inc( Reg8 dst ) => ( reg, mem ) => reg[dst] = Inc8Helper( reg[dst], reg );

		// INC (HL): 3 cycles
		public static IEnumerable<CpuOp> IncHl()
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) => val = Inc8Helper( val, reg );
			yield return ( reg, mem ) => { mem[reg.HL] = val; };
		}

		// DEC r16: 16bit alu op => 2 cycles
		public static IEnumerable<CpuOp> Dec( Reg16 dst )
		{
			yield return Nop;
			yield return ( reg, mem ) => { reg[dst] -= 1; };
		}

		private static byte Dec8Helper( byte val, RegView reg )
		{
			byte res = (byte)( val - 1 );
			reg.Zero = res == 0;
			reg.Sub = true;
			reg.HalfCarry = ( res & 0b1111 ) == 0b0000;
			return res;
		}

		// DEC r8: 1 cycle
		public static CpuOp Dec( Reg8 dst ) => ( reg, mem ) => reg[dst] = Dec8Helper( reg[dst], reg );

		// DEC (HL): 3 cycles
		public static IEnumerable<CpuOp> DecHl()
		{
			byte val = 0;
			yield return ( reg, mem ) => val = mem[reg.HL];
			yield return ( reg, mem ) => val = Dec8Helper( val, reg );
			yield return ( reg, mem ) => { mem[reg.HL] = val; };
		}

		// CALL cc, nn, 3-6 cycles
		public static IEnumerable<CpuOp> Call( Condition? cc = null )
		{
			ushort nn = 0; bool takeBranch = true;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			yield return ( reg, mem ) => takeBranch = cc == null || cc( reg );
			if( takeBranch )
			{
				yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetMsb();
				yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
				yield return ( reg, mem ) => reg.PC = nn;
			}
		}

		// RET, 4 cycles Ret cc 2/5 cycles
		public static IEnumerable<CpuOp> Ret( Condition? cc = null )
		{
			bool takeBranch = true;
			yield return ( reg, mem ) => takeBranch = cc == null || cc( reg );
			if( takeBranch == false )
			{
				yield return Nop;
			}
			else
			{
				byte lsb = 0; byte msb = 0;
				yield return ( reg, mem ) => lsb = mem[reg.SP++];
				yield return ( reg, mem ) => msb = mem[reg.SP++];
				yield return ( reg, mem ) => reg.PC = Binutil.SetLsb( reg.PC, lsb );
				yield return ( reg, mem ) => reg.PC = Binutil.SetMsb( reg.PC, msb );
			}
		}

		// RETI 4 cycles
		public static IEnumerable<CpuOp> Reti()
		{
			byte lsb = 0; byte msb = 0;
			yield return ( reg, mem ) => lsb = mem[reg.SP++];
			yield return ( reg, mem ) => msb = mem[reg.SP++];
			yield return ( reg, mem ) => reg.PC = Binutil.SetLsb( reg.PC, lsb );
			yield return ( reg, mem ) =>
			{
				reg.PC = Binutil.SetMsb( reg.PC, msb );
				reg.IME = IMEState.Enabled;
			};
		}

		// EI 1 + 1' cycles
		public static IEnumerable<CpuOp> Ei()
		{
			yield return ( reg, mem ) => reg.IME = IMEState.RequestEnabled;
		}

		// DI 1 cycle
		public static IEnumerable<CpuOp> Di()
		{
			yield return ( reg, mem ) => reg.IME = IMEState.Disabled;
		}

		// PUSH r16 4-cycle
		public static IEnumerable<CpuOp> Push( Reg16 src )
		{
			byte lsb = 0; byte msb = 0;
			yield return ( reg, mem ) => msb = reg[src].GetMsb();
			yield return ( reg, mem ) => lsb = reg[src].GetLsb();
			yield return ( reg, mem ) => mem[--reg.SP] = msb;
			yield return ( reg, mem ) => mem[--reg.SP] = lsb;
		}

		// POP r16 3-cycle
		public static IEnumerable<CpuOp> Pop( Reg16 dst )
		{
			byte lsb = 0; byte msb = 0;
			yield return ( reg, mem ) => lsb = mem[reg.SP++];
			yield return ( reg, mem ) => msb = mem[reg.SP++];
			yield return ( reg, mem ) => reg[dst] = Binutil.Combine( msb, lsb );
		}

		// RST n, 4 cycles
		public static IEnumerable<CpuOp> Rst( byte vec )
		{
			yield return Nop;
			yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetMsb();
			yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
			yield return ( reg, mem ) => reg.PC = Binutil.Combine( 0x00, vec );
		}

		// CCF
		public static IEnumerable<CpuOp> Ccf()
		{
			yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: !reg.Carry );
		}

		// SCF
		public static IEnumerable<CpuOp> Scf()
		{
			yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: true );
		}

		// SCF
		public static IEnumerable<CpuOp> Cpl()
		{
			yield return ( reg, mem ) => { reg.A = reg.A.Flip(); reg.SetFlags( Z: reg.Zero, N: true, H: true, C: reg.Carry ); };
		}

		// DAA
		public static IEnumerable<CpuOp> Daa()
		{
			yield return ( reg, mem ) =>
			{
				ushort res = reg.A;

				if( reg.Sub )
				{
					if( reg.HalfCarry ) res = (byte)( res - 0x06 );
					if( reg.Carry ) res = (byte)( res - 0x60 );
				}
				else
				{
					if( reg.HalfCarry || ( res & 0b0000_1111 ) > 9 ) res += 0x06;
					if( reg.Carry || res > 0b1001_1111 ) res += 0x60;
				}

				reg.HalfCarry = false;
				reg.Carry = res > 0xFF ? true : reg.Carry;
				reg.A = (byte)res;
				reg.Zero = reg.A == 0;
			};
		}

		private delegate byte AluFunc( RegView reg, byte val );

		// RLC r, RRC r etc - 1 or 3 cycles (+1 fetch)
		private static IEnumerable<CpuOp> MemAluMemHelper( RegX dst, AluFunc func )
		{
			if( dst.Is8() )
			{
				yield return ( reg, mem ) => reg[dst.To8()] = func( reg, reg[dst.To8()] );
			}
			else
			{
				byte val = 0;
				yield return ( reg, mem ) => val = mem[reg[dst.To16()]];
				yield return ( reg, mem ) => val = func( reg, val );
				yield return ( reg, mem ) => mem[reg[dst.To16()]] = val;
			}
		}

		private static byte RlcHelper( RegView reg, byte val )
		{
			reg.Carry = val.IsBitSet( 7 );
			val <<= 1;
			if( reg.Carry ) val |= 1;

			reg.Zero = val == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return val;
		}

		// RLC r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Rlc( RegX dst ) => MemAluMemHelper( dst, RlcHelper );

		private static byte RrcHelper( RegView reg, byte val )
		{
			reg.Carry = val.IsBitSet( 0 );
			val >>= 1;
			if( reg.Carry ) val |= ( 1 << 7 );

			reg.Zero = val == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return val;
		}

		// RRC r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Rrc( RegX dst ) => MemAluMemHelper( dst, RrcHelper );

		private static byte RlHelper( RegView reg, byte val )
		{
			byte res = (byte)( val << 1 );
			if( reg.Carry ) res |= 1;

			reg.Carry = val.IsBitSet( 7 );
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return res;
		}

		// RL r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Rl( RegX dst ) => MemAluMemHelper( dst, RlHelper );

		private static byte RrHelper( RegView reg, byte val )
		{
			byte res = (byte)( val >> 1 );
			if( reg.Carry ) res |= ( 1 << 7 );

			reg.Carry = val.IsBitSet( 0 );
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return res;
		}

		// RR r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Rr( RegX dst ) => MemAluMemHelper( dst, RrHelper );

		private static byte SlaHelper( RegView reg, byte val )
		{
			byte res = (byte)( val << 1 );
			reg.Carry = val.IsBitSet( 7 );
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return res;
		}

		// SLA r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Sla( RegX dst ) => MemAluMemHelper( dst, SlaHelper );

		private static byte SraHelper( RegView reg, byte val )
		{
			// shift right into carry, MSB stays the same
			byte res = (byte)( ( val >> 1 ) | ( val & ( 1 << 7 ) ) );
			reg.Carry = val.IsBitSet( 0 );
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return res;
		}

		// SRA r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Sra( RegX dst ) => MemAluMemHelper( dst, SraHelper );

		private static byte SwapHelper( RegView reg, byte val )
		{
			byte low = (byte)( val & 0b0000_1111 );
			byte high = (byte)( val & 0b1111_0000 );
			byte res = (byte)( ( low << 4 ) | ( high >> 4 ) );
			reg.Carry = false;
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return res;
		}

		// SWAP r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Swap( RegX dst ) => MemAluMemHelper( dst, SwapHelper );

		private static byte SrlHelper( RegView reg, byte val )
		{
			byte res = (byte)( val >> 1 ); // shift right into carry, MSB is set to 0
			reg.Carry = val.IsBitSet( 0 );
			reg.Zero = res == 0;
			reg.Sub = false;
			reg.HalfCarry = false;
			return res;
		}

		// SRL r - 1 or 3 cycles (+1 fetch)
		public static IEnumerable<CpuOp> Srl( RegX dst ) => MemAluMemHelper( dst, SrlHelper );
	}
}
