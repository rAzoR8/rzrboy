using System.Collections;
using System.Diagnostics;
using System.Text;

namespace rzr
{
    public class Isa : IEnumerable<ExecInstr>
    {
        private static readonly ExecInstr[] m_instructions = new ExecInstr[256];
        private static readonly ExecInstr[] m_extInstructions = new ExecInstr[256];

        public ExecInstr this[byte opcode]
        { 
            get => m_instructions[opcode];
            private set
            {
                Debug.Assert( m_instructions[opcode] == null );
                m_instructions[opcode] = value;
            }
        }

        private class ExtInstr : ExecInstr
        {
            public ExtInstr( ) : base( ExtOps ) {}

            private static IEnumerable<Op> ExtOps()
            {
                byte opcode = 0;
                yield return ( reg, mem ) => opcode = mem[reg.PC++]; // fetch, 1 M-cycle

                ExecInstr ext = m_extInstructions[opcode];
                foreach ( Op op in ext.Make() )
                {
                    yield return op;
                }
            }
        }

        private delegate ExecInstr BuildFunc<Y, X>(Y y, X x);
        private delegate ExecInstr BuildFunc<X>( X x );

        public Isa() 
        {
            RegX[] bcdehl = { RegX.B, RegX.C, RegX.D, RegX.E, RegX.H, RegX.L };
            RegX[] bcdehlHLa = { RegX.B, RegX.C, RegX.D, RegX.E, RegX.H, RegX.L, RegX.HL, RegX.A };
            RegX[] cela = { RegX.C, RegX.E, RegX.L, RegX.A };
            RegX[] bdhHL = {RegX.B, RegX.D, RegX.H, RegX.HL };
            RegX[] BCDEHLSP = {RegX.BC, RegX.DE, RegX.HL, RegX.SP };

            this[0xCB] = new ExtInstr();

            this[0xD3] = Invalid;
            this[0xE3] = Invalid;
            this[0xE4] = Invalid;
            this[0xF4] = Invalid;
            this[0xDB] = Invalid;
            this[0xDD] = Invalid;
            this[0xEB] = Invalid;
            this[0xEC] = Invalid;
            this[0xED] = Invalid;
            this[0xFC] = Invalid;
            this[0xFD] = Invalid;

            this[0x00] = Nop;
            this[0x10] = Stop;
            this[0x76] = Halt;

            // LD [BC DE HL SP], .DB16
			FillY( m_instructions, 0x01, LdImm, BCDEHLSP );

            // LD (BC DE), A
            this[0x02] = Ld( RegX.BC, RegX.A );
            this[0x12] = Ld( RegX.DE, RegX.A );
            // LD (HL+-), A
            this[0x22] = LdHlPlusA;
            this[0x32] = LdHlMinusA;

			// LD [B D H HL], .DB8
			this[0x06] = LdImm(RegX.B);
            this[0x16] = LdImm(RegX.D);
            this[0x26] = LdImm(RegX.H);            
            this[0x36] = LdImm(RegX.HL); // LD HL, .DB16

            // LD (.DB16), SP
            this[0x8] = LdImm16Sp;

            // ADD HL, r16
            this[0x09] = AddHl( Reg16.BC );
            this[0x19] = AddHl( Reg16.DE );
            this[0x29] = AddHl( Reg16.HL );
            this[0x39] = AddHl( Reg16.SP );

            // ADD HL, SP + r8
            this[0xE8] = AddSpR8;

            // LD A, (BC DE)
            this[0x0A] = Ld( RegX.A, RegX.BC );
            this[0x1A] = Ld( RegX.A, RegX.DE );
            // LD A, (HL+-)
            this[0x2A] = LdAHlPlus;
            this[0x3A] = LdAHlMinus;

            // LD [C E L A], .DB8
            FillY( m_instructions, 0x0E, LdImm, cela );

            // LD [B D H HL], [B C D E H L)
			Fill( m_instructions, offsetX: 0x40, builder: Ld,
                ys: new RegX[] { RegX.B, RegX.D, RegX.H, RegX.HL },
                xs: bcdehl );

            // LD [B D H], (HL)
            FillY( m_instructions, offsetX: 0x46, builder: ( RegX dst ) => Ld( dst, RegX.HL ), ys: new RegX[] { RegX.B, RegX.D, RegX.H } );

            // LD [B D H (HL)], [A B C D E H L (HL) A]
            Fill( m_instructions, offsetX: 0x47, builder: Ld,
                ys: new RegX[] { RegX.B, RegX.D, RegX.H, RegX.HL }, // dst
                xs: bcdehlHLa.Prepend(RegX.A) ); // src

            // ADD A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0x80, builder: Add, xs: bcdehlHLa );

            // Adc A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0x88, builder: Adc, xs: bcdehlHLa );

            // SUB A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0x90, builder: Sub, xs: bcdehlHLa );

            // SBC A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0x98, builder: Sbc, xs: bcdehlHLa );

            // AND A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0xA0, And, xs: bcdehlHLa );

            // XOR A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0xA8, Xor, xs: bcdehlHLa );

            // OR A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0xB0, Or, xs: bcdehlHLa );

            // CP A, [r8 (HL)]
            FillX( m_instructions, offsetX: 0xB8, Cp, xs: bcdehlHLa );

            // LD SP, HL
            this[0xF9] = Ld( RegX.SP, RegX.HL );

            // LD (0xFF00+db8), A
            this[0XE0] = LdhImmA;

            // LD A, (0xFF00+db8)
            this[0XF0] = LdhAImm;

            // LD (0xFF00+C), A
            this[0xE2] = LdhCa;

            // LD A, (0xFF00+C)
            this[0xF2] = LdhAc;

            // LD HL, SP + r8
            this[0xF8] = LdHlSpR8;

            // LD (a16), A
            this[0xEA] = LdImmAddrA;

            // LD A, (a16)
            this[0xFA] = LdAImmAddr;

            // JP HL
            this[0xE9] = JpHl;

            // JP NZ, a16
            this[0xC2] = JpCcImm16( Ops.NZ );
            this[0xD2] = JpCcImm16( Ops.NC );

            // JP a16
            this[0xC3] = JpImm16;

            // JP Z, a16
            this[0xCA] = JpCcImm16( Ops.Z );
            this[0xDA] = JpCcImm16( Ops.C );

            // JR NZ, e8
            this[0x20] = JrCcImm( Ops.NZ );
            this[0x30] = JrCcImm( Ops.NC );

            // JR e8
            this[0x18] = JrImm;

            // JR Z, e8
            this[0x28] = JrCcImm( Ops.Z );
            this[0x38] = JrCcImm( Ops.C );

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

            this[0x05] = Dec( Reg8.B );
            this[0x15] = Dec( Reg8.D );
            this[0x25] = Dec( Reg8.H );
            this[0x35] = DecHl;

            this[0x0B] = Dec( Reg16.BC );
            this[0x1B] = Dec( Reg16.DE );
            this[0x2B] = Dec( Reg16.HL );
            this[0x3B] = Dec( Reg16.SP );

            this[0x0D] = Dec( Reg8.C );
            this[0x1D] = Dec( Reg8.E );
            this[0x2D] = Dec( Reg8.L );
            this[0x3D] = Dec( Reg8.A );

            // CALL cc, nn
            this[0xC4] = CallCc( Ops.NZ );
            this[0xD4] = CallCc( Ops.NC );
            this[0xCC] = CallCc( Ops.Z );
            this[0xDC] = CallCc( Ops.C );

            // CALL nn
            this[0xCD] = Call;

            // RET cc
            this[0xC0] = RetCc( Ops.NZ );
            this[0xD0] = RetCc( Ops.NC );
            this[0xC8] = RetCc( Ops.Z );
            this[0xD8] = RetCc( Ops.C );

            // RET
            this[0xC9] = Ret;

            // RETI
            this[0xD9] = Reti;

            // POP r16
            this[0xC1] = Pop (Reg16.BC);
            this[0xD1] = Pop( Reg16.DE );
            this[0xE1] = Pop( Reg16.HL );
            this[0xF1] = Pop( Reg16.AF );

            this[0xC5] = Push( Reg16.BC );
            this[0xD5] = Push( Reg16.DE );
            this[0xE5] = Push( Reg16.HL );
            this[0xF5] = Push( Reg16.AF );

            // RST vec
            this[0xC7] = Rst( 0x00 );
            this[0xD7] = Rst( 0x10 );
            this[0xE7] = Rst( 0x20 );
            this[0xF7] = Rst( 0x30 );

            this[0xCF] = Rst( 0x08 );
            this[0xDF] = Rst( 0x18 );
            this[0xEF] = Rst( 0x28 );
            this[0xFF] = Rst( 0x38 );

            // DAA
            this[0x27] = Daa;

            // SCF
            this[0x37] = Scf;

            // CPL
            this[0x2F] = Cpl;

            // CCF
            this[0x3F] = Ccf;

            // DI
            this[0xF3] = Di;

            // EI
            this[0xFB] = Ei;

			// RLCA
			this[0x07] = Rlc( RegX.A );

			// RLA
			this[0x17] = Rl( RegX.A );

			// RRCA
			this[0x0F] = Rrc( RegX.A );

			// RRA
			this[0x1F] = Rr( RegX.A );

            // ADD A, db8
            this[0xC6] = AddImm8;

            // ADC A, db8
            this[0xCE] = AdcImm8;

            // SUB A, db8
            this[0xD6] = SubImm8;

            // SBC A, db8
            this[0xDE] = SbcImm8;

            // AND A, db8
            this[0xE6] = AndImm8;

            // XOR A, db8
            this[0xEE] = XorImm8;

            // OR A, db8
            this[0xF6] = OrImm8;

            // CP A, db8
            this[0xFE] = CpImm8;

            // RLC r
            FillX( m_extInstructions, offsetX: 0x00, Rlc, bcdehlHLa );

            // RRC r
            FillX( m_extInstructions, offsetX: 0x08, Rrc, bcdehlHLa );

            // RL r
            FillX( m_extInstructions, offsetX: 0x10, Rl, bcdehlHLa );

            // RR r
            FillX( m_extInstructions, offsetX: 0x18, Rr, bcdehlHLa );

            // SLA r
            FillX( m_extInstructions, offsetX: 0x20, Sla, bcdehlHLa );

            // SRA r
            FillX( m_extInstructions, offsetX: 0x28, Sra, bcdehlHLa );

            // SWAP r
            FillX( m_extInstructions, offsetX: 0x30, Swap, bcdehlHLa );

            // SRL r
            FillX( m_extInstructions, offsetX: 0x38, Srl, bcdehlHLa );

            // BIT [0 2 4 6], [B C D E H L, HL, A]
            Fill( m_extInstructions, offsetX: 0x40, Bit, new byte[] { 0, 2, 4, 6 }, bcdehlHLa );

            // BIT [1 3 5 7], [B C D E H L, HL, A]
            Fill( m_extInstructions, offsetX: 0x48, Bit, new byte[] { 1, 3, 5, 7 }, bcdehlHLa );

            // BIT [0 2 4 6], [B C D E H L, HL, A]
            Fill( m_extInstructions, offsetX: 0x80, Res, new byte[] { 0, 2, 4, 6 }, bcdehlHLa );

            // BIT [1 3 5 7], [B C D E H L, HL, A]
            Fill( m_extInstructions, offsetX: 0x88, Res, new byte[] { 1, 3, 5, 7 }, bcdehlHLa );

            // BIT [0 2 4 6], [B C D E H L, HL, A]
            Fill( m_extInstructions, offsetX: 0xC0, Set, new byte[] { 0, 2, 4, 6 }, bcdehlHLa );

            // BIT [1 3 5 7], [B C D E H L, HL, A]
            Fill( m_extInstructions, offsetX: 0xC8, Set, new byte[] { 1, 3, 5, 7 }, bcdehlHLa );

            //DebugReport( 512 );
        }

		/// <summary>
		/// only advances PC if the instructions is implemented
		/// </summary>
		/// <param name="pc"></param>
		/// <param name="mem"></param>
		/// <returns></returns>
		public static string Disassemble( ref ushort pc, ISection mem, bool throwException = true )
        {
            byte opcode = mem[pc]; 

            StringBuilder sb = new();
            sb.Append( $"[0x{pc:X4}:0x{opcode:X2}] " );

            AsmInstr instr = Asm.Disassemble( ref pc, mem, throwException: throwException );
            sb.Append( instr.ToString().ToUpper() );

			return sb.ToString();
        }

        public static IEnumerable<string> Disassemble( ushort from_pc, ushort to_pc, ISection mem, bool throwException = true )
        {
            ushort prev = (ushort)( from_pc - 1 );
            while ( from_pc < to_pc && prev != from_pc )
            {
                prev = from_pc;
                yield return Disassemble( ref from_pc, mem, throwException : throwException );
            }
        }

        // TODO: remove
        private void DebugReport( ushort expected )
        {
            Mem mem = new( new Section(0, Mem.RomBankSize * 2 ) );

            int count = 1; // 0xCB

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

			Debug.WriteLine( $"{count} out of 512 Instructions implemented: {100.0f * count / 512.0f}%" );
			Debug.Assert( count == expected );
		}
        
        // HELPERS
        private static void Fill<Y, X>( ExecInstr[] target, byte offsetX, BuildFunc<Y, X> builder, IEnumerable<Y> ys, IEnumerable<X> xs )
        {
            foreach( Y y in ys )
            {
                foreach( (X x, int i) in xs.Indexed() )
                {
                    Debug.Assert( target[offsetX + i] == null );
                    target[offsetX + i] = builder( y, x );
                }
                offsetX += 0x10;
            }
        }
        private static void FillX<X>( ExecInstr[] target, byte offsetX, BuildFunc<X> builder, IEnumerable<X> xs )
        {
            foreach( (X x, int i) in xs.Indexed() )
            {
                Debug.Assert( target[offsetX + i] == null );
                target[offsetX + i] = builder( x );
            }
        }
        private static void FillY<Y>( ExecInstr[] target, byte offsetX, BuildFunc<Y> builder, IEnumerable<Y> ys )
        {
            foreach( Y y in ys )
            {
                Debug.Assert( target[offsetX] == null );
                target[offsetX] = builder( y );
                offsetX += 0x10;
            }
        }

        // ###################################################
        // INSTRUCTIONS 
        // ###################################################

        // INVALID
        private static readonly ExecInstr Invalid = Ops.Nop;

        // NOP
        private static readonly ExecInstr Nop = Ops.Nop;

        // HALT
        private static readonly ExecInstr Halt = Ops.Halt;

        // STOP
        private static readonly ExecInstr Stop = new ( Ops.Stop );

        // INC r8
        private static ExecInstr Inc( Reg8 dst ) => Ops.Inc( dst );
        // INC r16
        private static ExecInstr Inc( Reg16 dst ) => new ( () => Ops.Inc( dst ) );
        // INC (HL)
        private static readonly ExecInstr IncHl = new ( Ops.IncHl );

        // INC r8
        private static ExecInstr Dec( Reg8 dst ) => Ops.Dec( dst );
        // INC r16
        private static ExecInstr Dec( Reg16 dst ) => new ( () => Ops.Dec( dst ) );
        // INC (HL)
        private static readonly ExecInstr DecHl = new ( Ops.DecHl );

        // BIT i, [r8, (HL)]
        private static ExecInstr Bit( byte bit, RegX target ) => new ( () => Ops.Bit( bit, target ) );

        // SET i, [r8, (HL)]
        private static ExecInstr Set( byte bit, RegX target ) => new ( () => Ops.Set( bit, target ) );

        // SET i, [r8, (HL)]
        private static ExecInstr Res( byte bit, RegX target ) => new ( () => Ops.Res( bit, target ) );

        // XOR A, [r8, (HL)]
        private static ExecInstr Xor( RegX target ) => new ( () => Ops.Xor( target ) );

        // XOR A, db8
        private static readonly ExecInstr XorImm8 = new ( Ops.XorImm8 );

        // LD r8, db8 LD r16, db16
        private static ExecInstr LdImm( RegX dst ) => new ( () => Ops.LdImm( dst ) );

        /// <summary>
        /// LD r8, r8' 
        /// LD r16, r16'
        /// LD r, (r16)
        /// LD (r16), r
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <returns></returns>
        private static ExecInstr Ld( RegX dst, RegX src ) => new( () => Ops.LdRegOrAddr( dst, src ) );

        // LD (HL+), A
        private static readonly ExecInstr LdHlPlusA = new ( Ops.LdHlPlusA );
        // LD (HL-), A
        private static readonly ExecInstr LdHlMinusA = new ( Ops.LdHlMinusA );

        // LD A, (HL+)
        private static readonly ExecInstr LdAHlPlus = new ( Ops.LdAHlPlus );
        // LD A, (HL-)
        private static readonly ExecInstr LdAHlMinus = new ( Ops.LdAHlMinus );

        // LD A, (0xFF00+C)
        private static readonly ExecInstr LdhAc = new ( Ops.LdhAc );

        // LD (0xFF00+C), A
        private static readonly ExecInstr LdhCa = new ( Ops.LdhCa );

        // LD A, (0xFF00+db8)
        private static readonly ExecInstr LdhAImm = new ( Ops.LdhAImm );

        // LD (0xFF00+db8), A
        private static readonly ExecInstr LdhImmA = new ( Ops.LdhImmA );

        // LD (a16), SP
        private static readonly ExecInstr LdImm16Sp = new ( Ops.LdImm16Sp );

        // LD (a16), A
        private static readonly ExecInstr LdImmAddrA = new ( Ops.LdImmAddrA ) ;

        // LD A, (a16)
        private static readonly ExecInstr LdAImmAddr = new ( Ops.LdAImmAddr );

        // LD HL,SP + r8 - 3 cycles
        private static readonly ExecInstr LdHlSpR8 = new ( Ops.LdHlSpR8 );


        // ADD A, [r8 (HL)]
        private static ExecInstr Add( RegX src ) => new ( () => Ops.Add( src ) );

        // ADD HL, r16
        private static ExecInstr AddHl( Reg16 src ) => new ( () => Ops.AddHl( src ) ) ;

        // ADD A, db8
        private static readonly ExecInstr AddImm8 = new ( () => Ops.AddImm8( carry: 0 ));

        // ADC A, db8
        private static readonly ExecInstr AdcImm8 = new ( () => Ops.AddImm8( carry: 1 ) ) ;
        
        // ADD SP, R8
        private static readonly ExecInstr AddSpR8 = new ( Ops.AddSpR8 );

        // ADD A, [r8 (HL)]
        private static ExecInstr Adc( RegX src ) => new ( () => Ops.Add( src, carry: 1 ) );


        // SUB A, [r8 (HL)]
        private static ExecInstr Sub( RegX src ) => new ( () => Ops.Sub( src ) );

        // SUB A, db8
        private static readonly ExecInstr SubImm8 = new ( () => Ops.SubImm8( carry: 0 ) );

        // SBC A, [r8 (HL)]
        private static ExecInstr Sbc( RegX src ) => new ( () => Ops.Sub( src, carry: 1 ) );

        // SBC A, db8
        private static readonly ExecInstr SbcImm8 = new ( () => Ops.SubImm8( carry: 1 ) );

        // AND A, [r8 (HL)]
        private static ExecInstr And( RegX src ) => new ( () => Ops.And( src ) );

        // AND A, db8
        private static readonly ExecInstr AndImm8 = new ( Ops.AndImm8 );

        // OR A, [r8 (HL)]
        private static ExecInstr Or( RegX src ) => new ( () => Ops.Or( src ) );

        // OR A, db8
        private static readonly ExecInstr OrImm8 = new ( Ops.OrImm8 );

        // CP A, [r8 (HL)]
        private static ExecInstr Cp( RegX src ) => new ( () => Ops.Cp( src ) );

        // CP A, db8
        private static readonly ExecInstr CpImm8 = new ( Ops.CpImm8 ) ;

        // JP HL
        private static readonly ExecInstr JpHl = Ops.JpHl;

        // JP a16
        private static readonly ExecInstr JpImm16 = new ( () => Ops.JpImm16());

        // JP cc, a16
        private static ExecInstr JpCcImm16( Ops.Condition cc ) => new ( () => Ops.JpImm16( cc ) );

        // JR e8
        private static readonly ExecInstr JrImm = new ( () => Ops.JrImm() );

        // JR cc, e8
        private static ExecInstr JrCcImm( Ops.Condition cc ) => new ( () => Ops.JrImm( cc ) );

        // CALL nn
        private static readonly ExecInstr Call = new ( () => Ops.Call() ) ;

        // CALL cc, nn
        private static ExecInstr CallCc( Ops.Condition cc ) => new ( () => Ops.Call( cc ) );

        // RETI
        private static readonly ExecInstr Reti = new ( Ops.Reti );

        // EI
        private static readonly ExecInstr Ei = new ( Ops.Ei  );

        // DI
        private static readonly ExecInstr Di = new ( Ops.Di );

        // RET
        private static readonly ExecInstr Ret = new ( () => Ops.Ret() );

        // RET cc
        private static ExecInstr RetCc( Ops.Condition cc ) => new ( () => Ops.Ret( cc ) );

        // PUSH r16
        private static ExecInstr Push( Reg16 src ) => new ( () => Ops.Push( src ) );

        // POP r16
        private static ExecInstr Pop( Reg16 dst ) => new ( () => Ops.Pop( dst ) );

        // RST vec 0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38
        private static ExecInstr Rst( byte vec ) => new ( () => Ops.Rst( vec ) );

        // CCF
        private static readonly ExecInstr Ccf = new ( Ops.Ccf );
        // SCF
        private static readonly ExecInstr Scf = new ( Ops.Scf );
        // SCF
        private static readonly ExecInstr Cpl = new ( Ops.Cpl );
        // DAA
        private static readonly ExecInstr Daa = new ( Ops.Daa );

        // RLC
        private static ExecInstr Rlc( RegX dst ) => new ( () => Ops.Rlc( dst ) );
        // RRC
        private static ExecInstr Rrc( RegX dst ) => new ( () => Ops.Rrc( dst ));

        // RL
        private static ExecInstr Rl( RegX dst ) => new ( () => Ops.Rl( dst ));
        // RR
        private static ExecInstr Rr( RegX dst ) => new ( () => Ops.Rr( dst ) );

        // SLA
        private static ExecInstr Sla( RegX dst ) => new ( () => Ops.Sla( dst ) );
        // SRA
        private static ExecInstr Sra( RegX dst ) => new ( () => Ops.Sra( dst ));

        // SWAP
        private static ExecInstr Swap( RegX dst ) => new ( () => Ops.Swap( dst ) );

        // SRL
        private static ExecInstr Srl( RegX dst ) => new ( () => Ops.Srl( dst )) ;

		public IEnumerator<ExecInstr> GetEnumerator()
		{
			return ( (IEnumerable<ExecInstr>)m_instructions ).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_instructions.GetEnumerator();
		}
	}

    static class LoopExtensions
    {
        public static IEnumerable<(T item, int index)> Indexed<T>(this IEnumerable<T> self) =>  self.Select((item, index) => (item, index));
    }
}
