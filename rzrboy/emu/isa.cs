using System.Diagnostics;
using System.Text;

namespace rzr
{
    public partial class Isa
    {
        private static readonly IBuilder[] m_instructions = new IBuilder[256];
        private static readonly IBuilder[] m_extInstructions = new IBuilder[256];

        public IBuilder this[byte opcode]
        { 
            get => m_instructions[opcode];
            private set
            {
                Debug.Assert( m_instructions[opcode] == null );
                m_instructions[opcode] = value;
            }
        }

        private class ExtInstruction : IInstruction // this is just a proxy to extension ISA
        {
            private IInstruction? cur = null;

            public IEnumerable<string> Disassemble( Ref<ushort> pc, ISection mem )
            {
                byte opcode = mem[pc.Value]; // fetch
                IBuilder builder = m_extInstructions[opcode];
                if ( builder != null )
                {
                    pc.Value++;
                    return builder.Build().Disassemble( pc, mem );
                }

                return Enumerable.Repeat( $"EXT 0x{opcode:X2} NOT IMPLEMENTED", 1) ;
            }

            public bool Eval( Reg reg, ISection mem )
            {
                if ( cur == null )
                {
                    byte opcode = mem[reg.PC++]; // fetch, 1 M-cycle
                    IBuilder builder = m_extInstructions[opcode];

                    if ( builder != null )
                    {
                        cur = builder.Build();
                        return true; // continue, there is more :)
                    }

                    return false;
                }
                else
                {
                    return cur.Eval( reg, mem );
                }
            }
        }

        private class ExtBuilder : IBuilder
        {
            public IInstruction Build() { return new ExtInstruction(); }
        }       

        private delegate IBuilder Build<Y, X>(Y y, X x);

        // returns next opcode for validation
        private static void Fill<Y, X>(IBuilder[] target, byte offsetX, byte stepY, Build<Y, X> builder, IEnumerable<Y> ys, IEnumerable<X> xs) 
        {
            foreach (Y y in ys)
            {
                foreach ((X x, int i) in xs.Indexed())
                {
                    Debug.Assert( target[offsetX + i] == null);
                    target[offsetX + i] = builder(y, x);
                }
                offsetX += stepY;
            }
        }
        private static void Fill<Y, X>( IBuilder[] target, byte offsetX, Build<Y, X> builder, Y y, IEnumerable<X> xs)
        {
            Fill(target, offsetX, stepY: 0, builder, new[] { y }, xs);
        }
        private static void Fill<Y, X>( IBuilder[] target, byte offsetX, byte stepY, Build<Y, X> builder, IEnumerable<Y> ys, X x)
        {
            Fill(target, offsetX, stepY, builder, ys, new[] { x });
        }

        public Isa() 
        {
            Reg8[] bcdehl = { Reg8.B, Reg8.C, Reg8.D, Reg8.E, Reg8.H, Reg8.L };

            this[0xCB] = new ExtBuilder();

            this[0x00] = Nop;

            // 16bit loads
            // LD (BC), .DB16
            this[0x01] = LdImm16(Reg16.BC);
            this[0x11] = LdImm16(Reg16.DE);
            this[0x21] = LdImm16(Reg16.HL);
            this[0x31] = LdImm16(Reg16.SP);

            // LD [B D H], .DB8
            this[0x06] = LdImm8(Reg8.B);
            this[0x16] = LdImm8(Reg8.D);
            this[0x26] = LdImm8(Reg8.H);

            // LD HL, .DB16
            this[0x36] = LdImm16(Reg16.HL);

            // 8bit loads
            // LD (BC DE), A
            this[0x02] = LdAddr(Reg16.BC, Reg8.A);
            this[0x12] = LdAddr(Reg16.DE, Reg8.A);
            // LD (HL+-), A
            this[0x22] = LdHlPlusA;
            this[0x32] = LdHlMinusA;

            // LD A, (BC DE)
            this[0x0A] = LdAddr( Reg8.A, Reg16.BC );
            this[0x1A] = LdAddr( Reg8.A, Reg16.DE );
            // LD A, (HL+-)
            this[0x2A] = LdAHlPlus;
            this[0x3A] = LdAHlMinus;

            // LD [C E L A], .DB8
            this[0x0E] = LdImm8( Reg8.C );
            this[0x1E] = LdImm8( Reg8.E );
            this[0x2E] = LdImm8( Reg8.L );
            this[0x3E] = LdImm8( Reg8.A );

            // LD (.DB16), SP
            this[0x8] = LdImm16Sp;

            // single byte reg moves
            // LD B, B | LD B, C ...
            // LD [B D H], [B C D E H L]
            Fill( m_instructions, offsetX: 0x40, stepY: 0x10,
                ( Reg8 dst, Reg8 src ) => LdReg8( dst, src ),
                ys: new[] { Reg8.B, Reg8.D, Reg8.H },
                xs: bcdehl );

            // LD [B D H], (HL)
            this[0x46] = LdAddr(Reg8.B, Reg16.HL);
            this[0x56] = LdAddr(Reg8.D, Reg16.HL);
            this[0x66] = LdAddr(Reg8.H, Reg16.HL);

            // LD [B D H], A
            this[0x47] = LdReg8(Reg8.B, Reg8.A);
            this[0x57] = LdReg8(Reg8.D, Reg8.A);
            this[0x67] = LdReg8(Reg8.H, Reg8.A);

            //this[0x76] = halt;

            // LD (HL), A ( the one right after HALT)
            this[0x77] = LdAddr(Reg16.HL, Reg8.A);

            // LD [C E L], [B C D E H L]
            Fill( m_instructions, offsetX: 0x48, stepY: 0x10,
                (Reg8 dst, Reg8 src) => LdReg8(dst, src),
                ys: new[] { Reg8.C, Reg8.E, Reg8.L },
                xs: bcdehl);

            // LD [C E L A], (HL)
            this[0x4E] = LdAddr(Reg8.C, Reg16.HL);
            this[0x5E] = LdAddr(Reg8.E, Reg16.HL);
            this[0x6E] = LdAddr(Reg8.L, Reg16.HL);
            this[0x7E] = LdAddr(Reg8.A, Reg16.HL);

            // LD [C E L A], A
            this[0x4F] = LdReg8(Reg8.C, Reg8.A);
            this[0x5F] = LdReg8(Reg8.E, Reg8.A);
            this[0x6F] = LdReg8(Reg8.L, Reg8.A);
            this[0x7F] = LdReg8(Reg8.A, Reg8.A);

            // LD (HL), [B C D E H L]
            Fill( m_instructions, offsetX: 0x70, stepY: 0,
                (Reg16 dst, Reg8 src) => LdAddr(dst, src),
                ys: new[] { Reg16.HL },
                xs: bcdehl);

            // LD A, [B C D E H L]
            Fill( m_instructions, offsetX: 0x78,
                (Reg8 dst, Reg8 src) => LdReg8(dst, src),
                y: Reg8.A,
                xs: bcdehl);

            // LD SP, HL
            this[0xF9] = LdReg16( Reg16.SP, Reg16.HL );

            // LD (0xFF00+db8), A
            this[0XE0] = LdhImmA;

            // LD A, (0xFF00+db8)
            this[0XF0] = LdhAImm;

            // LD (0xFF00+C), A
            this[0xE2] = LdhCa;

            // LD A, (0xFF00+C)
            this[0xF2] = LdhAc;

            // JP NZ, a16
            this[0xC2] = JpCcImm16( Ops.NZ, "NZ");
            this[0xD2] = JpCcImm16( Ops.NC, "NC");

            // JP a16
            this[0xC3] = JpImm16;

            // JP Z, a16
            this[0xCA] = JpCcImm16( Ops.Z, "Z");
            this[0xDA] = JpCcImm16( Ops.C, "C");

            // JR NZ, e8
            this[0x20] = JrCcImm( Ops.NZ, "NZ" );
            this[0x30] = JrCcImm( Ops.NC, "NC" );

            // JR e8
            this[0x18] = JrImm;

            // JR Z, e8
            this[0x28] = JrCcImm( Ops.Z, "Z" );
            this[0x38] = JrCcImm( Ops.C, "C" );

            // XOR A, r
            Fill( m_instructions, offsetX: 0xA8,
                ( Reg8 dst, Reg8 src ) => Xor( src ),
                y: Reg8.A,
                xs: bcdehl );

            // XOR A, (HL)
            this[0xAE] = XorHl;

            // XOR A, A
            this[0XAF] = Xor( Reg8.A );

            // BIT [0 2 4 6], [B C D E H L]
            Fill( m_extInstructions, offsetX: 0x40, stepY: 0x10, Bit,
                  new byte[]{ 0, 2, 4, 6 }, bcdehl );

            // BIT [0 2 4 6], (HL)
            Fill( m_extInstructions, offsetX: 0x46, stepY: 0x10,
                ( byte bit, Reg16 hl ) => BitHl( bit ),
                new byte[] { 0, 2, 4, 6 }, Reg16.HL );

            // BIT [0 2 4 6], A
            Fill( m_extInstructions, offsetX: 0x47, stepY: 0x10, Bit,
                  new byte[] { 0, 2, 4, 6 }, Reg8.A );

            // BIT [1 3 5 7], [B C D E H L]
            Fill( m_extInstructions, offsetX: 0x48, stepY: 0x10, Bit,
                  new byte[] { 1, 3, 5, 7 }, bcdehl );

            // BIT [1 3 5 7], (HL)
            Fill( m_extInstructions, offsetX: 0x4E, stepY: 0x10,
                ( byte bit, Reg16 hl ) => BitHl( bit ),
                new byte[] { 1, 3, 5, 7 }, Reg16.HL );

            // BIT [1 3 5 7], A
            Fill( m_extInstructions, offsetX: 0x4F, stepY: 0x10, Bit,
                  new byte[] { 1, 3, 5, 7 }, Reg8.A );

            this[0x03] = Inc( Reg16.BC );
            this[0x13] = Inc( Reg16.DE );
            this[0x23] = Inc( Reg16.HL );
            this[0x33] = Inc( Reg16.SP );

            this[0x04] = Inc( Reg8.B );
            this[0x14] = Inc( Reg8.D );
            this[0x24] = Inc( Reg8.H );
            this[0x34] = IncHl;

            this[0x0C] = Inc( Reg8.C );
            this[0x1C] = Inc( Reg8.E );
            this[0x2C] = Inc( Reg8.L );
            this[0x3C] = Inc( Reg8.A );

            // CALL cc, nn
            this[0xC5] = CallCc( Ops.NZ, "NZ" );
            this[0xD5] = CallCc( Ops.NC, "NC" );
            this[0xCC] = CallCc( Ops.Z, "Z" );
            this[0xDC] = CallCc( Ops.C, "C" );

            // CALL nn
            this[0xCD] = Call;

            DebugReport();
        }

        /// <summary>
        /// only advances PC if the instructions is implemented
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="bin"></param>
        /// <returns></returns>
        public string Disassemble( ref ushort pc, ISection bin )
        {
            byte opcode = bin[pc]; // fetch
            IBuilder builder = this[opcode];

            StringBuilder sb = new();
            sb.Append( $"[0x{pc:X4}:0x{opcode:X2}] " );

            if ( builder != null )
            {
                ++pc;
                sb.Append( builder.Build().ToString( ref pc, bin ) );
            }
            else
            {
                sb.Append( "NOT IMPLEMENTED" );
            }

            return sb.ToString();
        }

        public IEnumerable<string> Disassemble( ushort from_pc, ushort to_pc, ISection bin )
        {
            ushort prev = (ushort)( from_pc - 1 );
            while ( from_pc < to_pc && prev != from_pc )
            {
                prev = from_pc;
                yield return Disassemble( ref from_pc, bin );
            }
        }

        private void DebugReport() 
        {
            Mem mem = new();

            int count = 0;

            void Print( byte pc, byte ext )
            {
                mem[pc] = pc;
                mem[(ushort)( pc + 1 )] = ext;
                ushort dis_pc = pc;
                Debug.WriteLine( Disassemble( ref dis_pc, mem ) );

                if ( ( pc != 0xCB && m_instructions[pc] != null ) ||
                    ( pc == 0xCB && m_extInstructions[ext] != null ) )
                {
                    count++;
                }
            }

            for (ushort pc = 0; pc <= 255; pc++)
            {  
                if(pc != 0xCB)
                {
                    Print((byte)pc, 0);
                }
            }

            for ( ushort j = 0; j <= 255; j++ )
            {
                Print( 0xCB, (byte)j );
            }

            Debug.WriteLine($"{count} out of 511 Instructions implemented: {100.0f*count/500.0f}%");
        }
    }

    static class LoopExtensions
    {
        public static IEnumerable<(T item, int index)> Indexed<T>(this IEnumerable<T> self) =>  self.Select((item, index) => (item, index));
    }
}
