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

            private static op LdStack8Helper( Ref<byte> val ) => ( Reg reg, ISection mem ) => val.Value = mem[reg.SP++];

            // read two bytes from instruction stream, write to 16bit reg: 3 m-cycles
            public static IEnumerable<op> LdImm16( Reg16 target )
            {
                ushort val = 0;
                yield return ( reg, mem ) => val = mem[reg.PC++];
                yield return ( reg, mem ) => val |= (ushort)( mem[reg.PC++] << 8 );
                yield return ( reg, mem ) => reg[target] = val;
            }

            // LD r8, r8' 1-cycle
            public static op LdReg8( Reg8 dst, Reg8 src ) => ( reg, mem ) => { reg[dst] = reg[src]; };

            // LD r16, r16' 2-cycles
            public static IEnumerable<op> LdReg16( Reg16 dst, Reg16 src )
            {
                // simulate 16 bit register being written in two cycles
                yield return ( reg, mem ) => reg[dst] = binutil.SetLsb( reg[dst], reg[src].GetLsb() );
                yield return ( reg, mem ) => reg[dst] = binutil.SetMsb( reg[dst], reg[src].GetMsb() );
            }

            // LD r8, (r16) 2-cycle
            public static IEnumerable<op> LdAddr( Reg8 dst, Reg16 src_addr )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[src_addr];
                yield return ( reg, mem ) => reg[dst] = mem[address];
            }

            // LD (r16), r8 2-cycle
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

            public delegate bool Cond( Reg reg );
            public readonly static Cond NZ = ( Reg reg ) => !reg.Zero;
            public readonly static Cond Z = ( Reg reg ) => reg.Zero;
            public readonly static Cond NC = ( Reg reg ) => !reg.Carry;
            public readonly static Cond C = ( Reg reg ) => reg.Carry;

            // JP cc, a16 3/4 cycles
            public static IEnumerable<op> JpImm16( Cond? cc = null )
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
            public static IEnumerable<op> JrImm( Cond? cc = null )
            {
                byte offset = 0; bool takeBranch = true;
                yield return ( reg, mem ) => offset = mem[reg.PC++];
                if ( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
                if ( takeBranch )
                {
                    yield return JrHelper( (sbyte)offset );
                }
            }

            // XOR A, (HL)', 2 cycles
            public static IEnumerable<op> XorHl()
            {
                byte val = 0;
                yield return (reg, mem) => val = mem[reg.HL];
                yield return ( reg, mem ) => { reg.A ^= val; reg.SetFlags( reg.A == 0, false, false, false ); };
            }

            // XOR A, src 1-cycle
            public static op Xor( Reg8 src ) => ( reg, mem ) => { reg.A ^= reg[src]; reg.SetFlags( reg.A == 0, false, false, false ); };

            // BIT i, r 1-cycle
            public static op Bit( byte bit, Reg8 src ) => ( reg, mem ) =>
            {
                reg.Zero = !reg[src].IsBitSet( bit );
                reg.Sub = false;
                reg.HalfCarry = true;
            };

            // BIT i, (HL) 2-cycle
            public static IEnumerable<op> BitHl( byte bit )
            {
                byte val = 0;
                yield return ( reg, mem ) => val = mem[reg.HL];
                yield return ( reg, mem ) =>
                {
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
            public static IEnumerable<op> Call( Cond? cc = null ) 
            {
                ushort nn = 0; bool takeBranch = true;
                yield return ( reg, mem ) => nn = mem[reg.PC++];
                yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
                yield return (reg, mem) => takeBranch = cc == null || cc( reg );
                if ( takeBranch )
                {
                    yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetMsb();
                    yield return ( reg, mem ) => mem[--reg.SP] = reg.PC.GetLsb();
                    yield return ( reg, mem ) => reg.PC = nn;
                }
            }

            // Ret, 4 cycles Ret cc 2/5 cycles
            public static IEnumerable<op> Ret( Cond? cc = null ) 
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
        };

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


        // BIT i, r8
        private static Builder Bit( byte bit, Reg8 target ) => Ops.Bit( bit, target ).Get( "BIT" ) + $"{bit}" + Ops.operand( target );
        // BIT i, (HL)
        private static Builder BitHl( byte bit ) => new Builder( () => Ops.BitHl( bit ), "BIT" ) + $"{bit}" + "(HL)";

        // XOR A, src
        private static Builder Xor( Reg8 target ) => Ops.Xor( target ).Get( "XOR" ) + "A" + Ops.operand( target );
        // XOR A, (HL)
        private static readonly Builder XorHl = new Builder( Ops.XorHl, "XOR" ) + "A" + "(HL)";

        // LD r8, db8
        private static Builder LdImm8( Reg8 target ) => new Builder( () => Ops.LdImm8( target ), "LD" ) + Ops.operand( target ) + Ops.operandDB8;
        // LD r16, db16
        private static Builder LdImm16( Reg16 target ) => new Builder( () => Ops.LdImm16( target ), "LD" ) + Ops.operand( target ) + Ops.operandDB16;

        // LD r8, r8'
        private static Builder LdReg8( Reg8 dst, Reg8 src ) => Ops.LdReg8( dst, src ).Get( "LD" ) + Ops.operand( dst, src );
        // LD r16, r16'
        private static Builder LdReg16( Reg16 dst, Reg16 src ) => new Builder( () => Ops.LdReg16( dst, src ), "LD" ) + Ops.operand( dst, src );

        // LD r, (r16)
        private static Builder LdAddr( Reg8 dst, Reg16 src_addr ) => new Builder( () => Ops.LdAddr( dst, src_addr ), "LD" ) + Ops.operand( dst ) + $"({src_addr})";
        // LD (r16), r
        private static Builder LdAddr( Reg16 dst_addr, Reg8 src ) => new Builder( () => Ops.LdAddr( dst_addr, src ), "LD" ) + $"({dst_addr})" + Ops.operand( src );

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
        private static Builder JpCcImm16( Ops.Cond cc, string flag ) => new Builder( () => Ops.JpImm16( cc ), "JP" ) + flag + Ops.operandDB16;

        // JR e8
        private static readonly Builder JrImm = new Builder( () => Ops.JrImm(), "JR" ) + Ops.operandE8;

        // JR cc, e8
        private static Builder JrCcImm( Ops.Cond cc, string flag ) => new Builder( () => Ops.JrImm( cc ), "JR" ) + flag + Ops.operandE8;

        // CALL nn
        private static readonly Builder Call = new Builder( () => Ops.Call(), "CALL" ) + Ops.operandDB16;

        // CALL cc, nn
        private static Builder CallCc( Ops.Cond cc, string flag ) => new Builder( () => Ops.Call(cc), "CALL" ) + flag +  Ops.operandDB16;

        // RET
        private static readonly Builder Ret = new Builder( () => Ops.Ret(), "RET" );

        // RET cc
        private static Builder RetCc( Ops.Cond cc, string flag ) => new Builder( () => Ops.Ret( cc ), "RET" ) + flag;
    }
}
