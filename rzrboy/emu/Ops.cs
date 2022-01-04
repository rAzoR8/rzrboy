namespace rzr
{
    public partial class Isa
    {
        public static class Ops
        {
			public static Dis mnemonic( string str ) => ( ref ushort pc, ISection mem ) => str;
			public static Dis operand( RegX reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
			public static Dis operand( Reg8 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
			public static Dis operand( Reg16 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
			public static Dis addr( Reg16 reg ) => ( ref ushort pc, ISection mem ) => $"({reg})";
            public static Dis operand8OrAdd16( RegX reg ) => reg.Is8() ? operand(reg) : addr(reg.To16());

            public static readonly Dis operandE8 = ( ref ushort pc, ISection mem ) => $"{(sbyte)mem[pc++]}";
            public static readonly Dis operandDB8 = ( ref ushort pc, ISection mem ) => $"0x{mem[pc++]:X2}";
            public static Dis operandDB8x( string prefix ) => ( ref ushort pc, ISection mem ) => $"{prefix}{mem[pc++]:X2}";

            public static readonly Dis operandDB16 = ( ref ushort pc, ISection mem ) =>
            {
                string str = $"0x{mem[(ushort)( pc + 1 )]:X2}{mem[pc]:X2}";
                pc += 2;
                return str;
            };

            public readonly static Dis addrDB16 = ( ref ushort pc, ISection mem ) => $"({operandDB16( ref pc, mem )})";

            public static Op Nop = ( reg, mem ) => { };

            // read next byte from mem[pc++], 2 m-cycles
            private static IEnumerable<Op> LdImm8( Reg8 target )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.PC++;
                yield return ( reg, mem ) => reg[target] = mem[address];
            }

            // read two bytes from instruction stream, write to 16bit reg: 3 m-cycles
            private static IEnumerable<Op> LdImm16( Reg16 target )
            {
                ushort val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => val |= (ushort)( mem[reg.PC++] << 8 );
                yield return ( reg, mem ) => reg[target] = val;
            }

			public static IEnumerable<Op> LdImm( RegX target ) => target.Is8() ? LdImm8( target.To8() ) : LdImm16( target.To16() );

			// LD r8, r8' 1-cycle
			private static IEnumerable<Op> LdReg8( Reg8 dst, Reg8 src ) 
            { 
                yield return (reg, mem) => { reg[dst] = reg[src]; };
            }

            // LD r16, r16' 2-cycles
            private static IEnumerable<Op> LdReg16( Reg16 dst, Reg16 src )
            {
                // simulate 16 bit register being written in two cycles
                yield return ( reg, mem ) => reg[dst] = binutil.SetLsb( reg[dst], reg[src].GetLsb() );
                yield return ( reg, mem ) => reg[dst] = binutil.SetMsb( reg[dst], reg[src].GetMsb() );
            }

            // LD r8, (r16) 2-cycle
            private static IEnumerable<Op> LdAddr( Reg8 dst, Reg16 src_addr )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[src_addr];
                yield return ( reg, mem ) => reg[dst] = mem[address];
            }

            // LD (r16), r8 2-cycle
            private static IEnumerable<Op> LdAddr( Reg16 dst_addr, Reg8 src )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[dst_addr];
                yield return ( reg, mem ) => mem[address] = reg[src];
            }

            public static IEnumerable<Op> LdRegOrAddr(RegX dst, RegX src )
			{
				if( dst.Is8() && src.Is8() )    return LdReg8( dst.To8(), src.To8() );
				if( dst.Is16() && src.Is16() )  return LdReg16( dst.To16(), src.To16() );
                if( dst.Is8() && src.Is16() )   return LdAddr( dst.To8(), src.To16() );
                return LdAddr( dst.To16(), src.To8() );
            }

            // LD (HL+), A
            public static IEnumerable<Op> LdHlPlusA()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL++;
                yield return ( reg, mem ) => mem[address] = reg.A;
            }

            // LD (HL-), A
            public static IEnumerable<Op> LdHlMinusA()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL--;
                yield return ( reg, mem ) => mem[address] = reg.A;
            }

            // LD A, (HL+)
            public static IEnumerable<Op> LdAHlPlus()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL++;
                yield return ( reg, mem ) => reg.A = mem[address];
            }

            // LD A, (HL-)
            public static IEnumerable<Op> LdAHlMinus()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL--;
                yield return ( reg, mem ) => reg.A = mem[address];
            }

            // LD A, (0xFF00+C)
            public static IEnumerable<Op> LdhAc()
            {
                ushort address = 0xFF00;
                yield return ( reg, mem ) => { address += reg.C; };
                yield return ( reg, mem ) => { reg.A = mem[address]; };
            }

            // LD (0xFF00+C), A
            public static IEnumerable<Op> LdhCa()
            {
                ushort address = 0xFF00;
                yield return ( reg, mem ) => { address += reg.C; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD A, (0xFF00+db8)
            public static IEnumerable<Op> LdhAImm()
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return ( reg, mem ) => lsb = mem[reg.PC++];
                yield return ( reg, mem ) => address += lsb;
                yield return ( reg, mem ) => reg.A = mem[address];
            }

            // LD (0xFF00+db8), A
            public static IEnumerable<Op> LdhImmA()
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return ( reg, mem ) => lsb = mem[reg.PC++];
                yield return ( reg, mem ) => { address += lsb; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD (a16), SP
            public static IEnumerable<Op> LdImm16Sp()
            {
                ushort nn = 0;
                yield return ( reg, mem ) => nn = mem[reg.PC++];
                yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
                yield return ( reg, mem ) => mem[nn] = reg.SP.GetLsb();
                yield return ( reg, mem ) => mem[++nn] = reg.SP.GetMsb();
            }

			private static void AddHelper( Reg reg, byte rhs, byte carry = 0 )
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
			public static IEnumerable<Op> Add( RegX src, byte carry = 0 ) 
            {
                byte val = 0;
                if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
				yield return ( reg, mem ) =>
				{
					if( src.Is8() ) val = reg[src.To8()];
					AddHelper( reg, val, carry );
				};
			}

            // ADD A, db8 2-cycle
            public static IEnumerable<Op> AddImm8( byte carry = 0 )
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => AddHelper( reg, val, carry );
            }

            // ADD HL, r16 2 cycles
            public static IEnumerable<Op> AddHl( Reg16 src ) 
            {
                yield return Nop;
                yield return (Reg reg, ISection mem ) =>
                {
                    ushort l = reg.HL;
                    ushort r = reg[src];
                    reg.Sub = false;
                    reg.HalfCarry = ( l & 0x0FFF ) + ( r & 0x0FFF ) > 0x0FFF;
                    reg.Carry = l + r > 0xFFFF;
                    reg.HL += r;
                };
            }

            public static void SubHelper( Reg reg, byte rhs, byte carry = 0 ) 
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
			public static IEnumerable<Op> Sub( RegX src, byte carry = 0 )
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
            public static IEnumerable<Op> SubImm8( byte carry = 0 )
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => SubHelper( reg, val, carry );
            }

            // AND A, [r8, (HL)] 1-2 -cycle
            public static IEnumerable<Op> And( RegX src )
            {
                byte val = 0;
                if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
                yield return ( Reg reg, ISection mem ) =>
                {
					if( src.Is8() ) val = reg[src.To8()];
                    reg.SetFlags( Z: ( reg.A &= val ) == 0, N: false, H: true, C: false );
				};
			}

			// AND A, db8 2-cycle
			public static IEnumerable<Op> AndImm8()
			{
				byte val = 0;
				yield return ( reg, mem ) => val = mem[reg.PC++];
				yield return ( reg, mem ) => reg.SetFlags( Z: ( reg.A &= val ) == 0, N: false, H: true, C: false );
			}

			// Or A, [r8, (HL)] 1-2 -cycle
			public static IEnumerable<Op> Or( RegX src )
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
            public static IEnumerable<Op> OrImm8()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => reg.SetFlags( Z: ( reg.A |= val ) == 0, N: false, H: false, C: false );
            }

            public static void CpHelper(Reg reg, byte rhs ) 
            {
                var res = reg.A - rhs;
                reg.Zero = (byte)res == 0;
                reg.Sub = true;
                reg.HalfCarry = ( rhs & 0b1111 ) > ( reg.A & 0b1111 );
                reg.Carry = res < 0;
            }

            // CP A, [r8, (HL)] 1-2 -cycle
            public static IEnumerable<Op> Cp( RegX src )
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
            public static IEnumerable<Op> CpImm8()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => CpHelper( reg, val );
            }

            // JP HL 1 cycle
            public static readonly Op JpHl = ( reg, mem ) => { reg.PC = reg.HL; };

            public delegate bool Condition( Reg reg );
            public readonly static Condition NZ = ( Reg reg ) => !reg.Zero;
            public readonly static Condition Z = ( Reg reg ) => reg.Zero;
            public readonly static Condition NC = ( Reg reg ) => !reg.Carry;
            public readonly static Condition C = ( Reg reg ) => reg.Carry;

            // JP cc, a16 3/4 cycles
            public static IEnumerable<Op> JpImm16( Condition? cc = null )
            {
                ushort nn = 0; bool takeBranch = true;
                yield return ( reg, mem ) => nn = mem[reg.PC++];
                yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
                if ( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
                if ( takeBranch )
                {
                    yield return ( reg, mem ) => { reg.PC = nn; };
                }
            }

            // JR cc, e8 2/3 ycles
            public static IEnumerable<Op> JrImm( Condition? cc = null )
            {
                byte offset = 0; bool takeBranch = true;
                yield return ( reg, mem ) => offset = mem[reg.PC++];
                if ( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
                if ( takeBranch )
                {
                    yield return ( reg, mem ) => reg.PC = (ushort)( reg.PC + (sbyte)offset );
                }
            }

			// XOR A, [r8, (HL)]  1-2 cycles
			public static IEnumerable<Op> Xor( RegX src )
			{
				byte val = 0;
				if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
				yield return ( reg, mem ) => 
                { 
                    if( src.Is8() ) { val = reg[src.To8()]; } 
                    reg.SetFlags(( reg.A ^= val ) == 0, false, false, false );
                };
			}

            // XOR A, [r8, (HL)]  1-2 cycles
            public static IEnumerable<Op> XorImm8()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => reg.SetFlags( ( reg.A ^= val ) == 0, false, false, false );
            }

            // BIT i, [r8, (HL)] 1-2 -cycle
            public static IEnumerable<Op> Bit( byte bit, RegX src )
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

            public static IEnumerable<Op> Set( byte bit, RegX target )
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

            public static IEnumerable<Op> Res( byte bit, RegX target )
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
            public static IEnumerable<Op> Inc( Reg16 dst )
            {
                yield return Nop;
                yield return ( reg, mem ) => { reg[dst] += 1; };
            }

            private static byte Inc8Helper( byte val, Reg reg )
            {
                byte res = (byte)(val+1);
                reg.Zero = res == 0;
                reg.Sub = false;
                reg.HalfCarry = ( val & 0b1111 ) == 0b1111;
                return res;
            }

            // INC r8: 1 cycle
            public static Op Inc( Reg8 dst ) => ( reg, mem ) => reg[dst] = Inc8Helper( reg[dst], reg );

            // INC (HL): 3 cycles
            public static IEnumerable<Op> IncHl()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.HL];
                yield return ( reg, mem ) => val = Inc8Helper( val, reg );
                yield return ( reg, mem ) => { mem[reg.HL] = val; };
            }

            // DEC r16: 16bit alu op => 2 cycles
            public static IEnumerable<Op> Dec( Reg16 dst )
            {
                yield return Nop;
                yield return ( reg, mem ) => { reg[dst] -= 1; };
            }

            private static byte Dec8Helper( byte val, Reg reg )
            {
                byte res = (byte)( val - 1 );
                reg.Zero = res == 0;
                reg.Sub = true;
                reg.HalfCarry = ( res & 0b1111 ) == 0b0000;
                return res;
            }

            // DEC r8: 1 cycle
            public static Op Dec( Reg8 dst ) => ( reg, mem ) => reg[dst] = Dec8Helper( reg[dst], reg );

            // DEC (HL): 3 cycles
            public static IEnumerable<Op> DecHl()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.HL];
                yield return ( reg, mem ) => val = Dec8Helper( val, reg );
                yield return ( reg, mem ) => { mem[reg.HL] = val; };
            }

            // CALL cc, nn, 3-6 cycles
            public static IEnumerable<Op> Call( Condition? cc = null )
            {
                ushort nn = 0; bool takeBranch = true;
                yield return ( reg, mem ) => nn = mem[reg.PC++];
                yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
                yield return ( reg, mem ) => takeBranch = cc == null || cc( reg );
                if ( takeBranch )
                {
                    yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetMsb();
                    yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
                    yield return ( reg, mem ) => reg.PC = nn;
                }
            }

            // RET, 4 cycles Ret cc 2/5 cycles
            public static IEnumerable<Op> Ret( Condition? cc = null ) 
            {
                bool takeBranch = true;
                yield return (reg, mem) => takeBranch = cc == null || cc( reg );
                if ( takeBranch == false ) 
                {
                    yield return Nop;
                }
                else
                {
                    byte lsb = 0; byte msb = 0;
                    yield return ( reg, mem ) => lsb = mem[reg.SP++];
                    yield return ( reg, mem ) => msb = mem[reg.SP++];
                    yield return ( reg, mem ) => reg.PC = binutil.SetLsb( reg.PC, lsb );
                    yield return ( reg, mem ) => reg.PC = binutil.SetMsb( reg.PC, msb );
                }
            }

            // RETI 4 cycles
            public static IEnumerable<Op> Reti( )
            {
				byte lsb = 0; byte msb = 0;
				yield return ( reg, mem ) => lsb = mem[reg.SP++];
				yield return ( reg, mem ) => msb = mem[reg.SP++];
				yield return ( reg, mem ) => reg.PC = binutil.SetLsb( reg.PC, lsb );
                yield return ( reg, mem ) =>
                { 
                    reg.PC = binutil.SetMsb( reg.PC, msb );
                    reg.IME = IMEState.Enabled;
                };
			}

            // EI 1 + 1' cycles
            public static IEnumerable<Op> Ei() 
            {
                yield return ( reg, mem ) => reg.IME = IMEState.RequestEnabled;
            }

            // DI 1 cycle
            public static IEnumerable<Op> Di()
            {
                yield return ( reg, mem ) => reg.IME = IMEState.Disabled;
            }

            // PUSH r16 4-cycle
            public static IEnumerable<Op> Push( Reg16 src )
            {
                byte lsb = 0; byte msb = 0;
                yield return ( reg, mem ) => msb = reg[src].GetMsb();
                yield return ( reg, mem ) => lsb = reg[src].GetLsb();
                yield return ( reg, mem ) => mem[--reg.SP] = msb;
                yield return ( reg, mem ) => mem[--reg.SP] = lsb;
            }

            // POP r16 3-cycle
            public static IEnumerable<Op> Pop( Reg16 dst )
            {
                byte lsb = 0; byte msb = 0;
                yield return ( reg, mem ) => lsb = mem[reg.SP++];
                yield return ( reg, mem ) => msb = mem[reg.SP++];
                yield return ( reg, mem ) => reg[dst] = binutil.Combine( msb, lsb );
            }

            // RST n, 4 cycles
            public static IEnumerable<Op> Rst( byte vec )
            {
                yield return Nop;
                yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetMsb();
                yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
                yield return ( reg, mem ) => reg.PC = binutil.Combine(0x00, vec);
            }

            // CCF
            public static IEnumerable<Op> Ccf() 
            {
                yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: !reg.Carry);
            }

            // SCF
            public static IEnumerable<Op> Scf()
            {
                yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: true );
            }

            // SCF
            public static IEnumerable<Op> Cpl()
            {
                yield return ( reg, mem ) => { reg.A = reg.A.Flip(); reg.SetFlags( Z: reg.Zero, N: true, H: true, C: reg.Carry ); };
            }

            // DAA
            public static IEnumerable<Op> Daa()
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

            private delegate byte AluFunc( Reg reg, byte val );

            // RLC r, RRC r etc - 1 or 3 cycles (+1 fetch)
            private static IEnumerable<Op> MemAluMemHelper( RegX dst, AluFunc func )
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

            private static byte RlcHelper( Reg reg, byte val )
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
            public static IEnumerable<Op> Rlc( RegX dst ) => MemAluMemHelper( dst, RlcHelper );

            private static byte RrcHelper( Reg reg, byte val )
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
            public static IEnumerable<Op> Rrc( RegX dst ) => MemAluMemHelper( dst, RrcHelper );

            private static byte RlHelper( Reg reg, byte val )
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
            public static IEnumerable<Op> Rl( RegX dst ) => MemAluMemHelper( dst, RlHelper );

            private static byte RrHelper( Reg reg, byte val )
            {
                byte res = (byte)( val >> 1 );
                if( reg.Carry ) res |= (1 << 7);

                reg.Carry = val.IsBitSet( 0 );
                reg.Zero = res == 0;
                reg.Sub = false;
                reg.HalfCarry = false;
                return res;
            }

            // RR r - 1 or 3 cycles (+1 fetch)
            public static IEnumerable<Op> Rr( RegX dst ) => MemAluMemHelper( dst, RrHelper );

            private static byte SlaHelper( Reg reg, byte val )
            {
                byte res = (byte)( val << 1 );
                reg.Carry = val.IsBitSet( 7 );
                reg.Zero = res == 0;
                reg.Sub = false;
                reg.HalfCarry = false;
                return res;
            }

            // SLA r - 1 or 3 cycles (+1 fetch)
            public static IEnumerable<Op> Sla( RegX dst ) => MemAluMemHelper( dst, SlaHelper );

            private static byte SraHelper( Reg reg, byte val )
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
            public static IEnumerable<Op> Sra( RegX dst ) => MemAluMemHelper( dst, SraHelper );

            private static byte SwapHelper( Reg reg, byte val )
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
            public static IEnumerable<Op> Swap( RegX dst ) => MemAluMemHelper( dst, SwapHelper );

            private static byte SrlHelper( Reg reg, byte val )
            {
                byte res = (byte)( val >> 1 ); // shift right into carry, MSB is set to 0
                reg.Carry = val.IsBitSet( 0 );
                reg.Zero = res == 0;
                reg.Sub = false;
                reg.HalfCarry = false;
                return res;
            }

            // SRL r - 1 or 3 cycles (+1 fetch)
            public static IEnumerable<Op> Srl( RegX dst ) => MemAluMemHelper( dst, SrlHelper );
        }

        // NOP
        private static Instruction Nop = Ops.Nop.Get( "NOP" );

        // INC r8
        private static Instruction Inc( Reg8 dst ) => Ops.Inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC r16
        private static Instruction Inc( Reg16 dst ) => new Instruction (() => Ops.Inc( dst ), "INC" ) + Ops.operand( dst );
        // INC (HL)
        private readonly static Instruction IncHl = new Instruction( Ops.IncHl, "INC" ) + "(HL)";

        // INC r8
        private static Instruction Dec( Reg8 dst ) => Ops.Dec( dst ).Get( "Dec" ) + Ops.operand( dst );
        // INC r16
        private static Instruction Dec( Reg16 dst ) => new Instruction( () => Ops.Dec( dst ), "Dec" ) + Ops.operand( dst );
        // INC (HL)
        private readonly static Instruction DecHl = new Instruction( Ops.DecHl, "Dec" ) + "(HL)";

		// BIT i, [r8, (HL)]
		private static Instruction Bit( byte bit, RegX target ) => new Instruction( () => Ops.Bit( bit, target ), "BIT" ) + $"{bit}" + Ops.operand8OrAdd16( target );

        // SET i, [r8, (HL)]
        private static Instruction Set( byte bit, RegX target ) => new Instruction( () => Ops.Set( bit, target ), "SET" ) + $"{bit}" + Ops.operand8OrAdd16( target );

        // SET i, [r8, (HL)]
        private static Instruction Res( byte bit, RegX target ) => new Instruction( () => Ops.Res( bit, target ), "RES" ) + $"{bit}" + Ops.operand8OrAdd16( target );

        // XOR A, [r8, (HL)]
        private static Instruction Xor( RegX target ) => new Instruction(() => Ops.Xor( target ), "XOR" ) + "A" + Ops.operand( target );

        // XOR A, db8
        private static readonly Instruction XorImm8 = new Instruction( Ops.XorImm8, "XOR" ) + "A" + Ops.operandDB8;

        // LD r8, db8 LD r16, db16
        private static Instruction LdImm( RegX dst ) => new Instruction( () => Ops.LdImm( dst ), "LD" ) + Ops.operand( dst ) + ( dst.Is8() ? Ops.operandDB8 : Ops.operandDB16 );

        /// <summary>
        /// LD r8, r8' 
        /// LD r16, r16'
        /// LD r, (r16)
        /// LD (r16), r
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Instruction Ld( RegX dst, RegX src ) 
        {
			Instruction builder = new( () => Ops.LdRegOrAddr( dst, src ), "LD" );

            if( ( dst.Is8() && src.Is8() ) || ( dst.Is16() && src.Is16() ) )
            {
                return builder + Ops.operand( dst ) + Ops.operand( src );
            }
            else if( dst.Is16() && src.Is8() )
            {
                return builder + $"({dst})" + Ops.operand( src );
            }
            else 
            {
                return builder + Ops.operand( dst ) + $"({src})";
            }
        }

        // LD (HL+), A
        private static readonly Instruction LdHlPlusA = new Instruction( Ops.LdHlPlusA, "LD" ) + $"(HL+)" + "A";
        // LD (HL-), A
        private static readonly Instruction LdHlMinusA = new Instruction( Ops.LdHlMinusA, "LD" ) + $"(HL-)" + "A";

        // LD A, (HL+)
        private static readonly Instruction LdAHlPlus = new Instruction( Ops.LdAHlPlus, "LD" ) + "A" + $"(HL+)";
        // LD A, (HL-)
        private static readonly Instruction LdAHlMinus = new Instruction( Ops.LdAHlMinus, "LD" ) + "A" + $"(HL-)";

        // LD A, (0xFF00+C)
        private static readonly Instruction LdhAc = new Instruction( Ops.LdhAc, "LD" ) + "A" + "(0xFF00+C)";

        // LD (0xFF00+C), A
        private static readonly Instruction LdhCa = new Instruction( Ops.LdhCa, "LD" ) + "(0xFF00+C)" + "A";

        // LD A, (0xFF00+db8)
        private static readonly Instruction LdhAImm = new Instruction( Ops.LdhAImm, "LD" ) + "A" + Ops.operandDB8x( "0xFF00+" );

        // LD (0xFF00+db8), A
        private static readonly Instruction LdhImmA = new Instruction(  Ops.LdhImmA, "LD" ) + Ops.operandDB8x( "0xFF00+" ) + "A";

        // LD (a16), SP
        private static readonly Instruction LdImm16Sp = new Instruction( Ops.LdImm16Sp, "LD" ) + Ops.addrDB16 + "SP";

		// ADD A, [r8 (HL)]
		private static Instruction Add( RegX src ) => new Instruction( () => Ops.Add( src ), "ADD" ) + "A" + Ops.operand8OrAdd16( src );

        // ADD HL, r16
        private static Instruction AddHl( Reg16 src ) => new Instruction( () => Ops.AddHl( src ), "ADD" ) + "HL" + Ops.operand( src );

		// ADD A, db8
		private static readonly Instruction AddImm8 = new Instruction( () => Ops.AddImm8( carry: 0 ), "ADD" ) + "A" + Ops.operandDB8;

        // ADC A, db8
        private static readonly Instruction AdcImm8 = new Instruction( () => Ops.AddImm8( carry: 1 ), "ADC" ) + "A" + Ops.operandDB8;

        // ADD A, [r8 (HL)]
        private static Instruction Adc( RegX src ) => new Instruction( () => Ops.Add( src, carry: 1 ), "ADC" ) + "A" + Ops.operand8OrAdd16( src );

        
        // SUB A, [r8 (HL)]
        private static Instruction Sub( RegX src ) => new Instruction( () => Ops.Sub( src ), "SUB" ) + "A" + Ops.operand8OrAdd16( src );

        // SUB A, db8
        private static readonly Instruction SubImm8 = new Instruction( () => Ops.SubImm8( carry: 0 ), "SUB" ) + "A" + Ops.operandDB8;

        // SBC A, [r8 (HL)]
        private static Instruction Sbc( RegX src ) => new Instruction( () => Ops.Sub( src, carry: 1 ), "SBC" ) + "A" + Ops.operand8OrAdd16( src );

        // SBC A, db8
        private static readonly Instruction SbcImm8 = new Instruction( () => Ops.SubImm8( carry: 1), "SBC") + "A" + Ops.operandDB8;

        // AND A, [r8 (HL)]
        private static Instruction And( RegX src ) => new Instruction( () => Ops.And( src), "AND" ) + "A" + Ops.operand8OrAdd16( src );

        // AND A, db8
        private static readonly Instruction AndImm8 = new Instruction( Ops.AndImm8, "AND" ) + "A" + Ops.operandDB8;

        // OR A, [r8 (HL)]
        private static Instruction Or( RegX src ) => new Instruction( () => Ops.Or( src ), "OR" ) + "A" + Ops.operand8OrAdd16( src );

        // OR A, db8
        private static readonly Instruction OrImm8 = new Instruction( Ops.OrImm8, "OR" ) + "A" + Ops.operandDB8;

        // CP A, [r8 (HL)]
        private static Instruction Cp( RegX src ) => new Instruction( () => Ops.Cp( src ), "CP" ) + "A" + Ops.operand8OrAdd16( src );

        // CP A, db8
        private static readonly Instruction CpImm8 = new Instruction( Ops.CpImm8, "CP" ) + "A" + Ops.operandDB8;

        // JP HL
        private static readonly Instruction JpHl = Ops.JpHl.Get( "JP" ) + "HL";

        // JP a16
        private static readonly Instruction JpImm16 = new Instruction( () => Ops.JpImm16(), "JP" ) + Ops.operandDB16;

        // JP cc, a16
        private static Instruction JpCcImm16( Ops.Condition cc, string flag ) => new Instruction( () => Ops.JpImm16( cc ), "JP" ) + flag + Ops.operandDB16;

        // JR e8
        private static readonly Instruction JrImm = new Instruction( () => Ops.JrImm(), "JR" ) + Ops.operandE8;

        // JR cc, e8
        private static Instruction JrCcImm( Ops.Condition cc, string flag ) => new Instruction( () => Ops.JrImm( cc ), "JR" ) + flag + Ops.operandE8;

        // CALL nn
        private static readonly Instruction Call = new Instruction( () => Ops.Call(), "CALL" ) + Ops.operandDB16;

        // CALL cc, nn
        private static Instruction CallCc( Ops.Condition cc, string flag ) => new Instruction( () => Ops.Call(cc), "CALL" ) + flag +  Ops.operandDB16;

        // RETI
        private static readonly Instruction Reti = new Instruction( Ops.Reti, "RETI" );

        // EI
        private static readonly Instruction Ei = new Instruction( Ops.Ei, "EI" );

        // DI
        private static readonly Instruction Di = new Instruction( Ops.Di, "DI" );

        // RET
        private static readonly Instruction Ret = new Instruction( () => Ops.Ret(), "RET" );
  
        // RET cc
        private static Instruction RetCc( Ops.Condition cc, string flag ) => new Instruction( () => Ops.Ret( cc ), "RET" ) + flag;

        // PUSH r16
        private static Instruction Push( Reg16 src ) => new Instruction( () => Ops.Push( src ), "PUSH" ) + Ops.operand( src );

        // POP r16
        private static Instruction Pop( Reg16 dst ) => new Instruction( () => Ops.Pop( dst ), "POP" ) + Ops.operand( dst );

        // RST vec 0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38
        private static Instruction Rst( byte vec ) => new Instruction( () => Ops.Rst( vec ), "RST" ) + $"0x{vec:X2}";

        // CCF
        private static readonly Instruction Ccf = new Instruction( Ops.Ccf, "CCF" );
        // SCF
        private static readonly Instruction Scf = new Instruction( Ops.Scf, "SCF" );
        // SCF
        private static readonly Instruction Cpl = new Instruction( Ops.Cpl, "CPL" );
        // DAA
        private static readonly Instruction Daa = new Instruction( Ops.Daa, "DAA" );

        // RLC
        private static Instruction Rlc( RegX dst ) => new Instruction( () => Ops.Rlc( dst ), "RLC" ) + Ops.operand( dst );
        // RRC
        private static Instruction Rrc( RegX dst ) => new Instruction( () => Ops.Rrc( dst ), "RRC" ) + Ops.operand( dst );

        // RL
        private static Instruction Rl( RegX dst ) => new Instruction( () => Ops.Rl( dst ), "RL" ) + Ops.operand( dst );
        // RR
        private static Instruction Rr( RegX dst ) => new Instruction( () => Ops.Rr( dst ), "RR" ) + Ops.operand( dst );

        // SLA
        private static Instruction Sla( RegX dst ) => new Instruction( () => Ops.Sla( dst ), "SLA" ) + Ops.operand( dst );
        // SRA
        private static Instruction Sra( RegX dst ) => new Instruction( () => Ops.Sra( dst ), "SRA" ) + Ops.operand( dst );

        // SWAP
        private static Instruction Swap( RegX dst ) => new Instruction( () => Ops.Swap( dst ), "SWAP" ) + Ops.operand( dst );

        // SRL
        private static Instruction Srl( RegX dst ) => new Instruction( () => Ops.Srl( dst ), "SRL" ) + Ops.operand( dst );
    }
}
