namespace rzr
{
    public partial class Isa
    {
        public static class Ops
        {
			public static dis mnemonic( string str ) => ( ref ushort pc, ISection mem ) => str;
			public static dis operand( RegX reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
			public static dis operand( Reg8 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
			public static dis operand( Reg16 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
			public static dis addr( Reg16 reg ) => ( ref ushort pc, ISection mem ) => $"({reg})";
            public static dis operand8OrAdd16( RegX reg ) => reg.Is8() ? operand(reg) : addr(reg.To16());

            public static readonly dis operandE8 = ( ref ushort pc, ISection mem ) => $"{(sbyte)mem[pc++]}";
            public static readonly dis operandDB8 = ( ref ushort pc, ISection mem ) => $"0x{mem[pc++]:X2}";
            public static dis operandDB8x( string prefix ) => ( ref ushort pc, ISection mem ) => $"{prefix}{mem[pc++]:X2}";

            public static readonly dis operandDB16 = ( ref ushort pc, ISection mem ) =>
            {
                string str = $"0x{mem[(ushort)( pc + 1 )]:X2}{mem[pc]:X2}";
                pc += 2;
                return str;
            };

            public readonly static dis addrDB16 = ( ref ushort pc, ISection mem ) => $"({operandDB16( ref pc, mem )})";

            public static dis[] operand( Reg8 dst, Reg8 src ) => new dis[] { operand( dst ), operand( src ) };
            public static dis[] operand( Reg16 dst, Reg16 src ) => new dis[] { operand( dst ), operand( src ) };

            public static op Nop = ( reg, mem ) => { };

            // read next byte from mem[pc++], 2 m-cycles
            private static IEnumerable<op> LdImm8( Reg8 target )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.PC++;
                yield return ( reg, mem ) => reg[target] = mem[address];
            }

            // read two bytes from instruction stream, write to 16bit reg: 3 m-cycles
            private static IEnumerable<op> LdImm16( Reg16 target )
            {
                ushort val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => val |= (ushort)( mem[reg.PC++] << 8 );
                yield return ( reg, mem ) => reg[target] = val;
            }

			public static IEnumerable<op> LdImm( RegX target ) => target.Is8() ? LdImm8( target.To8() ) : LdImm16( target.To16() );

			// LD r8, r8' 1-cycle
			private static IEnumerable<op> LdReg8( Reg8 dst, Reg8 src ) 
            { 
                yield return (reg, mem) => { reg[dst] = reg[src]; };
            }

            // LD r16, r16' 2-cycles
            private static IEnumerable<op> LdReg16( Reg16 dst, Reg16 src )
            {
                // simulate 16 bit register being written in two cycles
                yield return ( reg, mem ) => reg[dst] = binutil.SetLsb( reg[dst], reg[src].GetLsb() );
                yield return ( reg, mem ) => reg[dst] = binutil.SetMsb( reg[dst], reg[src].GetMsb() );
            }

            // LD r8, (r16) 2-cycle
            private static IEnumerable<op> LdAddr( Reg8 dst, Reg16 src_addr )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[src_addr];
                yield return ( reg, mem ) => reg[dst] = mem[address];
            }

            // LD (r16), r8 2-cycle
            private static IEnumerable<op> LdAddr( Reg16 dst_addr, Reg8 src )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[dst_addr];
                yield return ( reg, mem ) => mem[address] = reg[src];
            }

            public static IEnumerable<op> LdRegOrAddr(RegX dst, RegX src )
			{
				if( dst.Is8() && src.Is8() )    return LdReg8( dst.To8(), src.To8() );
				if( dst.Is16() && src.Is16() )  return LdReg16( dst.To16(), src.To16() );
                if( dst.Is8() && src.Is16() )   return LdAddr( dst.To8(), src.To16() );
                return LdAddr( dst.To16(), src.To8() );
            }

            // LD (HL+), A
            public static IEnumerable<op> LdHlPlusA()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL++;
                yield return ( reg, mem ) => mem[address] = reg.A;
            }

            // LD (HL-), A
            public static IEnumerable<op> LdHlMinusA()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL--;
                yield return ( reg, mem ) => mem[address] = reg.A;
            }

            // LD A, (HL+)
            public static IEnumerable<op> LdAHlPlus()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL++;
                yield return ( reg, mem ) => reg.A = mem[address];
            }

            // LD A, (HL-)
            public static IEnumerable<op> LdAHlMinus()
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL--;
                yield return ( reg, mem ) => reg.A = mem[address];
            }

            // LD A, (0xFF00+C)
            public static IEnumerable<op> LdhAc()
            {
                ushort address = 0xFF00;
                yield return ( reg, mem ) => { address += reg.C; };
                yield return ( reg, mem ) => { reg.A = mem[address]; };
            }

            // LD (0xFF00+C), A
            public static IEnumerable<op> LdhCa()
            {
                ushort address = 0xFF00;
                yield return ( reg, mem ) => { address += reg.C; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD A, (0xFF00+db8)
            public static IEnumerable<op> LdhAImm()
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return ( reg, mem ) => lsb = mem[reg.PC++];
                yield return ( reg, mem ) => address += lsb;
                yield return ( reg, mem ) => reg.A = mem[address];
            }

            // LD (0xFF00+db8), A
            public static IEnumerable<op> LdhImmA()
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return ( reg, mem ) => lsb = mem[reg.PC++];
                yield return ( reg, mem ) => { address += lsb; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD (a16), SP
            public static IEnumerable<op> LdImm16Sp()
            {
                ushort nn = 0;
                yield return ( reg, mem ) => nn = mem[reg.PC++];
                yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
                yield return ( reg, mem ) => mem[nn] = reg.SP.GetLsb();
                yield return ( reg, mem ) => mem[++nn] = reg.SP.GetMsb();
            }

            private static op JpHelper( ushort addr ) => ( reg, mem ) => { reg.PC = addr; };

            // JP HL 1 cycle
            public static readonly op JpHl = ( reg, mem ) => { reg.PC = reg.HL; };

            public delegate bool Condition( Reg reg );
            public readonly static Condition NZ = ( Reg reg ) => !reg.Zero;
            public readonly static Condition Z = ( Reg reg ) => reg.Zero;
            public readonly static Condition NC = ( Reg reg ) => !reg.Carry;
            public readonly static Condition C = ( Reg reg ) => reg.Carry;

            // JP cc, a16 3/4 cycles
            public static IEnumerable<op> JpImm16( Condition? cc = null )
            {
                ushort nn = 0; bool takeBranch = true;
                yield return ( reg, mem ) => nn = mem[reg.PC++];
                yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
                if ( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
                if ( takeBranch )
                {
                    yield return JpHelper( nn );
                }
            }

            private static op JrHelper( sbyte offset ) => ( reg, mem ) => reg.PC = (ushort)( reg.PC + offset );

            // JR cc, e8 2/3 ycles
            public static IEnumerable<op> JrImm( Condition? cc = null )
            {
                byte offset = 0; bool takeBranch = true;
                yield return ( reg, mem ) => offset = mem[reg.PC++];
                if ( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
                if ( takeBranch )
                {
                    yield return JrHelper( (sbyte)offset );
                }
            }

			// XOR A, [r8, (HL)]  1-2 cycles
			public static IEnumerable<op> Xor( RegX src )
			{
				byte val = 0;
				if( src.Is16() ) yield return ( reg, mem ) => val = mem[reg.HL];
				yield return ( reg, mem ) => { if( src.Is8() ) { val = reg[src.To8()]; } reg.A ^= val; reg.SetFlags( reg.A == 0, false, false, false ); };
			}

            // BIT i, [r8, (HL)] 1-2 -cycle
            public static IEnumerable<op> Bit( byte bit, RegX src )
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

            // INC r16: 16bit alu op => 2 cycles
            public static IEnumerable<op> Inc( Reg16 dst )
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
            public static op Inc( Reg8 dst ) => ( reg, mem ) => reg[dst] = Inc8Helper( reg[dst], reg );

            // INC (HL): 3 cycles
            public static IEnumerable<op> IncHl()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.HL];
                yield return ( reg, mem ) => val = Inc8Helper( val, reg );
                yield return ( reg, mem ) => { mem[reg.HL] = val; };
            }

            // DEC r16: 16bit alu op => 2 cycles
            public static IEnumerable<op> Dec( Reg16 dst )
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
            public static op Dec( Reg8 dst ) => ( reg, mem ) => reg[dst] = Dec8Helper( reg[dst], reg );

            // DEC (HL): 3 cycles
            public static IEnumerable<op> DecHl()
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.HL];
                yield return ( reg, mem ) => val = Dec8Helper( val, reg );
                yield return ( reg, mem ) => { mem[reg.HL] = val; };
            }

            // CALL cc, nn, 3-6 cycles
            public static IEnumerable<op> Call( Condition? cc = null )
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
            public static IEnumerable<op> Ret( Condition? cc = null ) 
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

            // TODO: RETI

            // PUSH r16 4-cycle
            public static IEnumerable<op> Push( Reg16 src )
            {
                byte lsb = 0; byte msb = 0;
                yield return ( reg, mem ) => msb = reg[src].GetMsb();
                yield return ( reg, mem ) => lsb = reg[src].GetLsb();
                yield return ( reg, mem ) => mem[--reg.SP] = msb;
                yield return ( reg, mem ) => mem[--reg.SP] = lsb;
            }

            // POP r16 3-cycle
            public static IEnumerable<op> Pop( Reg16 dst )
            {
                byte lsb = 0; byte msb = 0;
                yield return ( reg, mem ) => lsb = mem[reg.SP++];
                yield return ( reg, mem ) => msb = mem[reg.SP++];
                yield return ( reg, mem ) => reg[dst] = binutil.Combine( msb, lsb );
            }

            // RST n, 4 cycles
            public static IEnumerable<op> Rst( byte vec )
            {
                yield return Nop;
                yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetMsb();
                yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
                yield return ( reg, mem ) => reg.PC = binutil.Combine(0x00, vec);
            }

            // CCF
            public static IEnumerable<op> Ccf() 
            {
                yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: !reg.Carry);
            }

            // SCF
            public static IEnumerable<op> Scf()
            {
                yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: true );
            }

            // SCF
            public static IEnumerable<op> Cpl()
            {
                yield return ( reg, mem ) => { reg.A = reg.A.Flip(); reg.SetFlags( Z: reg.Zero, N: true, H: true, C: reg.Carry ); };
            }

            // DAA
            public static IEnumerable<op> Daa()
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
            private static IEnumerable<op> BitOpWithFlagHelper( RegX dst, AluFunc func )
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
            public static IEnumerable<op> Rlc( RegX dst ) => BitOpWithFlagHelper( dst, RlcHelper );

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
            public static IEnumerable<op> Rrc( RegX dst ) => BitOpWithFlagHelper( dst, RrcHelper );

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
            public static IEnumerable<op> Rl( RegX dst ) => BitOpWithFlagHelper( dst, RlHelper );

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
            public static IEnumerable<op> Rr( RegX dst ) => BitOpWithFlagHelper( dst, RrHelper );
        }

        private static Builder Nop = Ops.Nop.Get( "NOP" );

        // INC r8
        private static Builder Inc( Reg8 dst ) => Ops.Inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC r16
        private static Builder Inc( Reg16 dst ) => new Builder (() => Ops.Inc( dst ), "INC" ) + Ops.operand( dst );
        // INC (HL)
        private readonly static Builder IncHl = new Builder( Ops.IncHl, "INC" ) + "(HL)";

        // INC r8
        private static Builder Dec( Reg8 dst ) => Ops.Dec( dst ).Get( "Dec" ) + Ops.operand( dst );
        // INC r16
        private static Builder Dec( Reg16 dst ) => new Builder( () => Ops.Dec( dst ), "Dec" ) + Ops.operand( dst );
        // INC (HL)
        private readonly static Builder DecHl = new Builder( Ops.DecHl, "Dec" ) + "(HL)";

		// BIT i, [r8, (HL)]
		private static Builder Bit( byte bit, RegX target ) => new Builder( () => Ops.Bit( bit, target ), "BIT" ) + $"{bit}" + Ops.operand8OrAdd16( target );

		// XOR A, [r8, (HL)]
		private static Builder Xor( RegX target ) => new Builder(() => Ops.Xor( target ), "XOR" ) + "A" + Ops.operand( target );

        // LD r8, db8 LD r16, db16
        private static Builder LdImm( RegX dst ) => new Builder( () => Ops.LdImm( dst ), "LD" ) + Ops.operand( dst ) + ( dst.Is8() ? Ops.operandDB8 : Ops.operandDB16 );

        /// <summary>
        /// LD r8, r8' 
        /// LD r16, r16'
        /// LD r, (r16)
        /// LD (r16), r
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static Builder Ld( RegX dst, RegX src ) 
        {
			Builder builder = new( () => Ops.LdRegOrAddr( dst, src ), "LD" );

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
        private static readonly Builder LdHlPlusA = new Builder( Ops.LdHlPlusA, "LD" ) + $"(HL+)" + "A";
        // LD (HL-), A
        private static readonly Builder LdHlMinusA = new Builder( Ops.LdHlMinusA, "LD" ) + $"(HL-)" + "A";

        // LD A, (HL+)
        private static readonly Builder LdAHlPlus = new Builder( Ops.LdAHlPlus, "LD" ) + "A" + $"(HL+)";
        // LD A, (HL-)
        private static readonly Builder LdAHlMinus = new Builder( Ops.LdAHlMinus, "LD" ) + "A" + $"(HL-)";

        // LD A, (0xFF00+C)
        private static readonly Builder LdhAc = new Builder( Ops.LdhAc, "LD" ) + "A" + "(0xFF00+C)";

        // LD (0xFF00+C), A
        private static readonly Builder LdhCa = new Builder( Ops.LdhCa, "LD" ) + "(0xFF00+C)" + "A";

        // LD A, (0xFF00+db8)
        private static readonly Builder LdhAImm = new Builder( Ops.LdhAImm, "LD" ) + "A" + Ops.operandDB8x( "0xFF00+" );

        // LD (0xFF00+db8), A
        private static readonly Builder LdhImmA = new Builder(  Ops.LdhImmA, "LD" ) + Ops.operandDB8x( "0xFF00+" ) + "A";

        // LD (a16), SP
        private static readonly Builder LdImm16Sp = new Builder( Ops.LdImm16Sp, "LD" ) + Ops.addrDB16 + "SP";

        // JP HL
        private static readonly Builder JpHl = Ops.JpHl.Get( "JP" ) + "HL";

        // JP a16
        private static readonly Builder JpImm16 = new Builder( () => Ops.JpImm16(), "JP" ) + Ops.operandDB16;

        // JP cc, a16
        private static Builder JpCcImm16( Ops.Condition cc, string flag ) => new Builder( () => Ops.JpImm16( cc ), "JP" ) + flag + Ops.operandDB16;

        // JR e8
        private static readonly Builder JrImm = new Builder( () => Ops.JrImm(), "JR" ) + Ops.operandE8;

        // JR cc, e8
        private static Builder JrCcImm( Ops.Condition cc, string flag ) => new Builder( () => Ops.JrImm( cc ), "JR" ) + flag + Ops.operandE8;

        // CALL nn
        private static readonly Builder Call = new Builder( () => Ops.Call(), "CALL" ) + Ops.operandDB16;

        // CALL cc, nn
        private static Builder CallCc( Ops.Condition cc, string flag ) => new Builder( () => Ops.Call(cc), "CALL" ) + flag +  Ops.operandDB16;

        // RET
        private static readonly Builder Ret = new Builder( () => Ops.Ret(), "RET" );

        // RET cc
        private static Builder RetCc( Ops.Condition cc, string flag ) => new Builder( () => Ops.Ret( cc ), "RET" ) + flag;

        // PUSH r16
        private static Builder Push( Reg16 src ) => new Builder( () => Ops.Push( src ), "PUSH" ) + Ops.operand( src );

        // POP r16
        private static Builder Pop( Reg16 dst ) => new Builder( () => Ops.Pop( dst ), "POP" ) + Ops.operand( dst );

        // RST vec 0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38
        private static Builder Rst( byte vec ) => new Builder( () => Ops.Rst( vec ), "RST" ) + $"0x{vec:X2}";

        // CCF
        private static readonly Builder Ccf = new Builder( Ops.Ccf, "CCF" );
        // SCF
        private static readonly Builder Scf = new Builder( Ops.Scf, "SCF" );
        // SCF
        private static readonly Builder Cpl = new Builder( Ops.Cpl, "CPL" );
        // DAA
        private static readonly Builder Daa = new Builder( Ops.Daa, "DAA" );

        // RLC
        private static Builder Rlc( RegX dst ) => new Builder( () => Ops.Rlc( dst ), "RLC" ) + Ops.operand( dst );
        // RRC
        private static Builder Rrc( RegX dst ) => new Builder( () => Ops.Rrc( dst ), "RRC" ) + Ops.operand( dst );

        // RL
        private static Builder Rl( RegX dst ) => new Builder( () => Ops.Rl( dst ), "RL" ) + Ops.operand( dst );
        // RR
        private static Builder Rr( RegX dst ) => new Builder( () => Ops.Rr( dst ), "RR" ) + Ops.operand( dst );
    }
}
