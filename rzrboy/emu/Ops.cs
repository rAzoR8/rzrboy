namespace rzr
{
    public partial class Isa
    {
        public static class Ops
        {
            public static dis mnemonic( string str ) => ( ref ushort pc, ISection mem ) => str;
            public static dis operand( Reg8 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
            public static dis operand( Reg16 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();

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
            public static IEnumerable<op> LdImm8( Reg8 target )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.PC++;
                yield return ( reg, mem ) => reg[target] = mem[address];
            }

            private static op LdImm8Helper( byte? val ) => ( Reg reg, ISection mem ) => { val = mem[reg.PC++]; };

            // read two bytes from instruction stream, write to 16bit reg: 3 m-cycles
            public static IEnumerable<op> LdImm16( Reg16 target )
            {
                ushort val = 0;
                yield return ( reg, mem ) => val.SetLsb( mem[reg.PC++] );
                yield return ( reg, mem ) => val.SetMsb( mem[reg.PC++] );
                yield return ( reg, mem ) => reg[target] = val;
            }

            // reg to reg, 1 m-cycle
            public static op LdReg8( Reg8 dst, Reg8 src ) => ( reg, mem ) => { reg[dst] = reg[src]; };
            // reg to reg, 2 m-cycles
            public static IEnumerable<op> LdReg16( Reg16 dst, Reg16 src )
            {
                // simulate 16 bit register being written in two cycles
                yield return ( reg, mem ) => reg[dst].SetLsb( reg[src].GetLsb() );
                yield return ( reg, mem ) => reg[dst].SetMsb( reg[src].GetMsb() );
            }

            // remove:
            // address to byte ref / helper
            private static op LdAddrHelper( byte? dst, Reg16 src_addr ) => ( reg, mem ) => { dst = mem[reg[src_addr]]; };

            // LD r, 1byte helper
            // private static op ldreg_helper( Reg8 dst, byte val ) => ( reg, mem ) => { reg[dst] = val; };

            // address to reg
            public static IEnumerable<op> LdAddr( Reg8 dst, Reg16 src_addr )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[src_addr];
                yield return ( reg, mem ) => reg[dst] = mem[address];
            }

            // reg to address
            public static IEnumerable<op> LdAddr( Reg16 dst_addr, Reg8 src )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[dst_addr];
                yield return ( reg, mem ) => mem[address] = reg[src];
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
                yield return LdImm8Helper( lsb );
                yield return ( reg, mem ) => { address += lsb; };
                yield return ( reg, mem ) => { reg.A = mem[address]; };
            }

            // LD (0xFF00+db8), A
            public static IEnumerable<op> LdhImmA()
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return LdImm8Helper( lsb );
                yield return ( reg, mem ) => { address += lsb; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD (a16), SP
            public static IEnumerable<op> LdImm16Sp()
            {
                byte nlow = 0, nhigh = 0;
                yield return LdImm8Helper( nlow );
                yield return LdImm8Helper( nhigh );
                ushort nn = nhigh.Combine( nlow );
                yield return ( reg, mem ) => mem[nn] = reg.SP.GetLsb();
                yield return ( reg, mem ) => mem[++nn] = reg.SP.GetMsb();
            }

            private static op JpHelper( ushort addr ) => ( reg, mem ) => { reg.PC = addr; };

            // JP HL
            public static op JpHl() => ( reg, mem ) => { reg.PC = reg.HL; };

            public delegate bool cond( Reg reg );
            public readonly static cond NZ = ( Reg reg ) => !reg.Zero;
            public readonly static cond Z = ( Reg reg ) => reg.Zero;
            public readonly static cond NC = ( Reg reg ) => !reg.Carry;
            public readonly static cond C = ( Reg reg ) => reg.Carry;

            // JP cc, a16
            public static IEnumerable<op> JpCcImm16( cond cc )
            {
                byte nlow = 0, nhigh = 0;
                bool takeBranch = false;
                yield return LdImm8Helper( nlow );
                yield return ( Reg reg, ISection mem ) => { nhigh = mem[reg.PC++]; takeBranch = cc( reg ); };
                if ( takeBranch )
                {
                    ushort nn = nhigh.Combine( nlow );
                    yield return JpHelper( nn );
                }
            }

            // JP a16
            public static IEnumerable<op> JpImm16()
            {
                byte nlow = 0, nhigh = 0;
                yield return LdImm8Helper( nlow );
                yield return LdImm8Helper( nhigh );
                ushort nn = nhigh.Combine( nlow );
                yield return JpHelper( nn );
            }

            private static op JrHelper( sbyte offset ) => ( reg, mem ) => { reg.PC = (ushort)( reg.PC + offset ); };

            public static IEnumerable<op> JrImm()
            {
                byte offset = 0;
                yield return LdImm8Helper( offset );
                yield return JrHelper( (sbyte)offset );
            }

            public static IEnumerable<op> JrCcImm( cond cc )
            {
                byte offset = 0; bool takeBranch = false;
                yield return ( Reg reg, ISection mem ) => { offset = mem[reg.PC++]; takeBranch = cc( reg ); };
                if ( takeBranch )
                {
                    yield return JrHelper( (sbyte)offset );
                }
            }

            // XOR A, (HL)', 2 cycles
            public static IEnumerable<op> XorHl()
            {
                byte val = 0;
                yield return LdAddrHelper( val, Reg16.HL );
                yield return ( reg, mem ) => { reg.A ^= val; reg.SetFlags( reg.A == 0, false, false, false ); };
            }

            // XOR A, src
            public static op Xor( Reg8 src ) => ( reg, mem ) => { reg.A ^= reg[src]; reg.SetFlags( reg.A == 0, false, false, false ); };

            // BIT i, b8
            private static op BitHelper( byte bit, byte val ) => ( reg, mem ) =>
            {
                reg.Zero = !val.IsBitSet( bit );
                reg.Sub = false;
                reg.HalfCarry = true;
            };

            // BIT i, r
            public static op Bit( byte _bit, Reg8 src ) => ( reg, mem ) => BitHelper( bit: _bit, val: reg[src] );

            // BIT i, (HL)
            public static IEnumerable<op> BitHl( byte bit )
            {
                byte val = 0;
                yield return LdAddrHelper( val, Reg16.HL );
                yield return BitHelper( bit, val );
            }

            // INC r16: 16bit alu op => 2 cycles
            public static IEnumerable<op> Inc( Reg16 dst )
            {
                yield return Nop;
                yield return ( reg, mem ) => { reg[dst] += 1; };
            }

            // INC r8: 1 cycle
            public static op Inc( Reg8 dst ) => ( reg, mem ) =>
            {
                byte res = reg[dst]++;
                reg.Zero = res == 0;
                reg.Sub = false;
                reg.HalfCarry = ( res & 0b10000 ) == 0b10000;
            };

            // INC (HL): 3 cycles
            public static IEnumerable<op> IncHl()
            {
                byte val = 0;
                yield return LdAddrHelper( val, Reg16.HL );
                yield return ( reg, mem ) =>
                {
                    byte res = val++;
                    reg.Zero = res == 0;
                    reg.Sub = false;
                    reg.HalfCarry = ( res & 0b10000 ) == 0b10000;
                };
                yield return ( reg, mem ) => { mem[reg.HL] = val; };
            }
        };

        private readonly static Builder Nop = Ops.Nop.Get( "NOP" );

        // INC r8
        private static Builder Inc( Reg8 dst ) => Ops.Inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC r16
        private static Builder Inc( Reg16 dst ) => Ops.Inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC (HL)
        private readonly static Builder IncHl = Ops.IncHl().Get( "INC" ) + "(HL)";

        // BIT i, r8
        private static Builder Bit( byte bit, Reg8 target ) => Ops.Bit( bit, target ).Get( "BIT" ) + $"{bit}" + Ops.operand( target );
        // BIT i, (HL)
        private static Builder BitHl( byte bit ) => Ops.BitHl( bit ).Get( "BIT" ) + $"{bit}" + "(HL)";

        // XOR A, src
        private static Builder Xor( Reg8 target ) => Ops.Xor( target ).Get( "XOR" ) + "A" + Ops.operand( target );
        // XOR A, (HL)
        private static readonly Builder XorHl = Ops.XorHl().Get( "XOR" ) + "A" + "(HL)";

        // LD r8, db8
        private static Builder LdImm8( Reg8 target ) => Ops.LdImm8( target ).Get( "LD" ) + Ops.operand( target ) + Ops.operandDB8;
        // LD r16, db16
        private static Builder LdImm16( Reg16 target ) => Ops.LdImm16( target ).Get( "LD" ) + Ops.operand( target ) + Ops.operandDB16;

        // LD r8, r8'
        private static Builder LdReg8( Reg8 dst, Reg8 src ) => Ops.LdReg8( dst, src ).Get( "LD" ) + Ops.operand( dst, src );
        // LD r16, r16'
        private static Builder LdReg16( Reg16 dst, Reg16 src ) => Ops.LdReg16( dst, src ).Get( "LD" ) + Ops.operand( dst, src );

        // LD r, (r16)
        private static Builder LdAddr( Reg8 dst, Reg16 src_addr ) => Ops.LdAddr( dst, src_addr ).Get( "LD" ) + Ops.operand( dst ) + $"({src_addr})";
        // LD (r16), r
        private static Builder LdAddr( Reg16 dst_addr, Reg8 src ) => Ops.LdAddr( dst_addr, src ).Get( "LD" ) + $"({dst_addr})" + Ops.operand( src );

        // LD (HL+), A
        private static readonly Builder LdHlPlusA = Ops.LdHlPlusA().Get( "LD" ) + $"(HL+)" + "A";
        // LD (HL-), A
        private static readonly Builder LdHlMinusA = Ops.LdHlMinusA().Get( "LD" ) + $"(HL-)" + "A";

        // LD A, (HL+)
        private static readonly Builder LdAHlPlus = Ops.LdAHlPlus().Get( "LD" ) + "A" + $"(HL+)";
        // LD A, (HL-)
        private static readonly Builder LdAHlMinus = Ops.LdAHlMinus().Get( "LD" ) + "A" + $"(HL-)";

        // LD A, (0xFF00+C)
        private static readonly Builder LdhAc = Ops.LdhAc().Get( "LD" ) + "A" + "(0xFF00+C)";

        // LD (0xFF00+C), A
        private static readonly Builder LdhCa = Ops.LdhCa().Get( "LD" ) + "(0xFF00+C)" + "A";

        // LD A, (0xFF00+db8)
        private static readonly Builder LdhAImm = Ops.LdhAImm().Get( "LD" ) + "A" + Ops.operandDB8x( "0xFF00+" );

        // LD (0xFF00+db8), A
        private static readonly Builder LdhImmA = Ops.LdhImmA().Get( "LD" ) + Ops.operandDB8x( "0xFF00+" ) + "A";

        // LD (a16), SP
        private static readonly Builder LdImm16Sp = Ops.LdImm16Sp().Get( "LD" ) + Ops.addrDB16 + "SP";

        // JP a16
        private static readonly Builder JpImm16 = Ops.JpImm16().Get( "JP" ) + Ops.operandDB16;

        // JP cc, a16
        private static Builder JpCcImm16( Ops.cond cc, string flag ) => Ops.JpCcImm16( cc ).Get( "JP" ) + flag + Ops.addrDB16;

        // JR e8
        private static readonly Builder JrImm = Ops.JrImm().Get( "JR" ) + Ops.operandE8;

        // JR cc, e8
        private static Builder JrCcImm( Ops.cond cc, string flag ) => Ops.JrCcImm( cc ).Get( "JR" ) + flag + Ops.operandE8;
    }
}
