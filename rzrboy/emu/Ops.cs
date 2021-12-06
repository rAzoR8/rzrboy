﻿namespace emu
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

            public static op nop = ( reg, mem ) => { };

            // read next byte from mem[pc++], 2 m-cycles
            public static IEnumerable<op> ldimm( Reg8 target )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.PC++;
                yield return ( reg, mem ) => reg[target] = mem[address];
            }

            private static op ldimm_helper( byte? val ) => ( Reg reg, ISection mem ) => { val = mem[reg.PC++]; };

            // read two bytes from instruction stream, write to 16bit reg: 3 m-cycles
            public static IEnumerable<op> ldimm( Reg16 target )
            {
                ushort val = 0;
                yield return ( reg, mem ) => val.SetLsb( mem[reg.PC++] );
                yield return ( reg, mem ) => val.SetMsb( mem[reg.PC++] );
                yield return ( reg, mem ) => reg[target] = val;
            }

            // reg to reg, 1 m-cycle
            public static op ldreg( Reg8 dst, Reg8 src ) => ( reg, mem ) => { reg[dst] = reg[src]; };
            // reg to reg, 2 m-cycles
            public static IEnumerable<op> ldreg( Reg16 dst, Reg16 src )
            {
                // simulate 16 bit register being written in two cycles
                yield return ( reg, mem ) => reg[dst].SetLsb( reg[src].GetLsb() );
                yield return ( reg, mem ) => reg[dst].SetMsb( reg[src].GetMsb() );
            }

            // remove:
            // address to byte ref / helper
            private static op ldadr_helper( byte? dst, Reg16 src_addr ) => ( reg, mem ) => { dst = mem[reg[src_addr]]; };

            // LD r, 1byte helper
            // private static op ldreg_helper( Reg8 dst, byte val ) => ( reg, mem ) => { reg[dst] = val; };

            // address to reg
            public static IEnumerable<op> ldadr( Reg8 dst, Reg16 src_addr )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[src_addr];
                yield return ( reg, mem ) => reg[dst] = mem[address];
            }

            // reg to address
            public static IEnumerable<op> ldadr( Reg16 dst_addr, Reg8 src )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg[dst_addr];
                yield return ( reg, mem ) => mem[address] = reg[src];
            }

            // LD (HL+), r
            public static IEnumerable<op> ldhlplus( Reg8 src )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL++;
                yield return ( reg, mem ) => mem[address] = reg[src];
            }

            // LD (HL-), r
            public static IEnumerable<op> ldhlminus( Reg8 src )
            {
                ushort address = 0;
                yield return ( reg, mem ) => address = reg.HL--;
                yield return ( reg, mem ) => mem[address] = reg[src];
            }

            // LD A, (0xFF00+C)
            public static IEnumerable<op> ldhac() 
            {
                ushort address = 0xFF00;
                yield return ( reg, mem ) => { address += reg.C; };
                yield return ( reg, mem ) => { reg.A = mem[address]; };
            }

            // LD (0xFF00+C), A
            public static IEnumerable<op> ldhca()
            {
                ushort address = 0xFF00;
                yield return ( reg, mem ) => { address += reg.C; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD A, (0xFF00+db8)
            public static IEnumerable<op> ldhaimm() 
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return ldimm_helper( lsb );
                yield return ( reg, mem ) => { address += lsb; };
                yield return ( reg, mem ) => { reg.A = mem[address]; };
            }

            // LD (0xFF00+db8), A
            public static IEnumerable<op> ldhimma()
            {
                byte lsb = 0; ushort address = 0xFF00;
                yield return ldimm_helper( lsb );                
                yield return ( reg, mem ) => { address += lsb; };
                yield return ( reg, mem ) => { mem[address] = reg.A; };
            }

            // LD (a16), SP
            public static IEnumerable<op> ldimm16_sp()
            {
                byte nlow = 0, nhigh = 0;
                yield return ldimm_helper( nlow );
                yield return ldimm_helper( nhigh );
                ushort nn = nhigh.Combine( nlow );
                yield return ( reg, mem ) => mem[nn] = reg.SP.GetLsb();
                yield return ( reg, mem ) => mem[++nn] = reg.SP.GetMsb();
            }

            private static op jp_helper( ushort addr ) => ( reg, mem ) => { reg.PC = addr; };

            // JP HL
            public static op jphl() => ( reg, mem ) => { reg.PC = reg.HL; };

            public delegate bool cond( Reg reg );
            public readonly static cond NZ = ( Reg reg ) => !reg.Zero;
            public readonly static cond Z = ( Reg reg ) => reg.Zero;
            public readonly static cond NC = ( Reg reg ) => !reg.Carry;
            public readonly static cond C = ( Reg reg ) => reg.Carry;

            // JP cc, a16
            public static IEnumerable<op> jpccimm16( cond cc )
            {
                byte nlow = 0, nhigh = 0;
                bool takeBranch = false;
                yield return ldimm_helper( nlow );
                yield return ( Reg reg, ISection mem ) => { nhigh = mem[reg.PC++]; takeBranch = cc( reg ); };
                if ( takeBranch )
                {
                    ushort nn = nhigh.Combine( nlow );
                    yield return jp_helper( nn );
                }
            }

            // JP a16
            public static IEnumerable<op> jpimm16()
            {
                byte nlow = 0, nhigh = 0;
                yield return ldimm_helper( nlow );
                yield return ldimm_helper( nhigh );
                ushort nn = nhigh.Combine( nlow );
                yield return jp_helper( nn );
            }

            private static op jr_helper( sbyte offset ) => ( reg, mem ) => { reg.PC = (ushort)( reg.PC + offset ); };

            public static IEnumerable<op> jrimm()
            {
                byte offset = 0;
                yield return ldimm_helper( offset );
                yield return jr_helper( (sbyte)offset );
            }

            public static IEnumerable<op> jrccimm( cond cc )
            {
                byte offset = 0; bool takeBranch = false;
                yield return ( Reg reg, ISection mem ) => { offset = mem[reg.PC++]; takeBranch = cc( reg ); };
                if ( takeBranch )
                {
                    yield return jr_helper( (sbyte)offset );
                }
            }

            // XOR A, (HL)', 2 cycles
            public static IEnumerable<op> xorhl()
            {
                byte val = 0;
                yield return ldadr_helper( val, Reg16.HL );
                yield return (reg, mem) => { reg.A ^= val; reg.SetFlags( reg.A == 0, false, false, false ); };
            }

            // XOR A, src
            public static op xor( Reg8 src ) => ( reg, mem ) => { reg.A ^= reg[src]; reg.SetFlags( reg.A == 0, false, false, false ); };

            // BIT i, b8
            private static op bit_helper( byte bit, byte val ) => ( reg, mem ) => 
            {
                reg.Zero = !val.IsBitSet( bit );
                reg.Sub = false;
                reg.HalfCarry = true;
            };

            // BIT i, r
            public static op bit( byte _bit, Reg8 src ) => ( reg, mem ) => bit_helper( bit: _bit, val: reg[src] );

            // BIT i, (HL)
            public static IEnumerable<op> bithl(byte bit)
            {
                byte val = 0;
                yield return ldadr_helper( val, Reg16.HL );
                yield return bit_helper( bit, val );
            }

            // INC r16: 16bit alu op => 2 cycles
            public static IEnumerable<op> inc( Reg16 dst )
            {
                yield return nop;
                yield return ( reg, mem ) => { reg[dst] += 1; };
            }

            // INC r8: 1 cycle
            public static op inc( Reg8 dst ) => ( reg, mem ) =>
            {
                byte res = reg[dst]++;
                reg.Zero = res == 0;
                reg.Sub = false;
                reg.HalfCarry = (res & 0b10000) == 0b10000;
            };

            // INC (HL): 3 cycles
            public static IEnumerable<op> inchl( )
            {
                byte val = 0;
                yield return ldadr_helper( val, Reg16.HL );
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

        private readonly static Builder nop = new Builder( Ops.nop, "NOP" );

        // INC r8
        private static Builder inc( Reg8 dst ) => Ops.inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC r16
        private static Builder inc( Reg16 dst ) => Ops.inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC (HL)
        private readonly static Builder inchl = new Builder( Ops.inchl(), "INC" ) + "(HL)";

        // BIT i, r8
        private static Builder bit( byte bit, Reg8 target ) => Ops.bit( bit, target ).Get( "BIT" ) + $"{bit}" + Ops.operand( target );
        // BIT i, (HL)
        private static Builder bithl( byte bit ) => Ops.bithl( bit ).Get( "BIT" ) + $"{bit}" + "(HL)";

        // XOR A, src
        private static Builder xor( Reg8 target ) => Ops.xor( target ).Get( "XOR" ) + "A" + Ops.operand(target);
        // XOR A, (HL)
        private static Builder xorhl() => Ops.xorhl().Get( "XOR" ) + "A" + "(HL)";

        // LD r8, db8
        private static Builder ldimm( Reg8 target ) => Ops.ldimm( target ).Get( "LD" ) + Ops.operand( target ) + Ops.operandDB8;
        // LD r16, db16
        private static Builder ldimm( Reg16 target ) => Ops.ldimm( target ).Get( "LD" ) + Ops.operand( target ) + Ops.operandDB16;

        // LD r8, r8'
        private static Builder ldreg(Reg8 dst, Reg8 src) => Ops.ldreg(dst, src).Get("LD") + Ops.operand(dst, src);
        // LD r16, r16'
        private static Builder ldreg(Reg16 dst, Reg16 src) => Ops.ldreg(dst, src).Get("LD") + Ops.operand(dst, src);

        // LD r, (r16)
        private static Builder ldadr(Reg8 dst, Reg16 src_addr) => Ops.ldadr(dst, src_addr).Get("LD") + Ops.operand(dst) + $"({src_addr})";
        // LD (r16), r
        private static Builder ldadr(Reg16 dst_addr, Reg8 src) => Ops.ldadr(dst_addr, src).Get("LD") + $"({dst_addr})" + Ops.operand(src);

        // LD A, (0xFF00+C)
        private static readonly Builder ldhac = new Builder( Ops.ldhac(), "LD" ) + "A" + "(0xFF00+C)";

        // LD (0xFF00+C), A
        private static readonly Builder ldhca = new Builder( Ops.ldhca(), "LD" ) + "(0xFF00+C)" + "A";

        // LD A, (0xFF00+db8)
        private static readonly Builder ldhaimm = new Builder( Ops.ldhaimm(), "LD" ) + "A" + Ops.operandDB8x( "0xFF00+" );

        // LD (0xFF00+db8), A
        private static readonly Builder ldhimma = new Builder( Ops.ldhimma(), "LD" ) +  Ops.operandDB8x( "0xFF00+" ) + "A";

        // LD (a16), SP
        private static Builder ldimm16_sp() => new Builder(Ops.ldimm16_sp(), "LD") + Ops.addrDB16 + "SP";

        // JP a16
        private static Builder jpimm16() => new Builder(Ops.jpimm16(), "JP") + Ops.operandDB16;

        // JP cc, a16
        private static Builder jpimm16cc(Ops.cond cc, string flag) => new Builder(Ops.jpccimm16(cc), "JP") + flag + Ops.addrDB16;

        // JR e8
        private static Builder jrimm() => new Builder( Ops.jrimm(), "JR" ) + Ops.operandE8;

        // JR cc, e8
        private static Builder jrimmcc( Ops.cond cc, string flag ) => new Builder( Ops.jrccimm( cc ), "JR" ) + flag + Ops.operandE8;
    }
}
