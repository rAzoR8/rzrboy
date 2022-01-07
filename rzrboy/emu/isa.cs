using System.Collections;
using System.Diagnostics;
using System.Text;

namespace rzr
{
    public class Isa : IEnumerable<Instruction>
    {
        private static readonly Instruction[] m_instructions = new Instruction[256];
        private static readonly Instruction[] m_extInstructions = new Instruction[256];

        public Instruction this[byte opcode]
        { 
            get => m_instructions[opcode];
            private set
            {
                Debug.Assert( m_instructions[opcode] == null );
                m_instructions[opcode] = value;
            }
        }

        private class ExtInstr : Instruction
        {
            public ExtInstr( ) : base( ExtOps ) {}

            private static IEnumerable<Op> ExtOps()
            {
                byte opcode = 0;
                yield return ( reg, mem ) => opcode = mem[reg.PC++]; // fetch, 1 M-cycle

                Instruction ext = m_extInstructions[opcode];
                foreach ( Op op in ext.Make() )
                {
                    yield return op;
                }
            }

            public override IEnumerable<string> Operands( Ref<ushort> pc, ISection mem )
            {
                byte opcode = mem[pc.Value];
                Instruction ext = m_extInstructions[opcode];
                pc.Value++;
                return ext.Operands( pc, mem );
            }
        }

        private delegate Instruction BuildFunc<Y, X>(Y y, X x);
        private delegate Instruction BuildFunc<X>( X x );

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
            this[0xC4] = CallCc( Ops.NZ, "NZ" );
            this[0xD4] = CallCc( Ops.NC, "NC" );
            this[0xCC] = CallCc( Ops.Z, "Z" );
            this[0xDC] = CallCc( Ops.C, "C" );

            // CALL nn
            this[0xCD] = Call;

            // RET cc
            this[0xC0] = RetCc( Ops.NZ, "NZ" );
            this[0xD0] = RetCc( Ops.NC, "NC" );
            this[0xC8] = RetCc( Ops.Z, "Z" );
            this[0xD8] = RetCc( Ops.C, "C" );

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
            this[0xC7] = Rst( 0x00);
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

            DebugReport( 512 );
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
            Instruction builder = this[opcode];

            StringBuilder sb = new();
            sb.Append( $"[0x{pc:X4}:0x{opcode:X2}] " );

            if ( builder != null )
            {
                ++pc;
                sb.Append( builder.ToString( ref pc, bin ) );
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

        // TODO: remove
        private void DebugReport( ushort expected )
        {
            Mem mem = new();

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
        private static void Fill<Y, X>( Instruction[] target, byte offsetX, BuildFunc<Y, X> builder, IEnumerable<Y> ys, IEnumerable<X> xs )
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
        private static void FillX<X>( Instruction[] target, byte offsetX, BuildFunc<X> builder, IEnumerable<X> xs )
        {
            foreach( (X x, int i) in xs.Indexed() )
            {
                Debug.Assert( target[offsetX + i] == null );
                target[offsetX + i] = builder( x );
            }
        }
        private static void FillY<Y>( Instruction[] target, byte offsetX, BuildFunc<Y> builder, IEnumerable<Y> ys )
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
        private static readonly Instruction Invalid = Ops.Nop.Get( "INVALID" );

        // NOP
        private static readonly Instruction Nop = Ops.Nop.Get( "NOP" );

        // HALT
        private static readonly Instruction Halt = Ops.Halt.Get( "HALT" );

        // STOP
        private static readonly Instruction Stop = new Instruction( Ops.Stop, "STOP" );

        // INC r8
        private static Instruction Inc( Reg8 dst ) => Ops.Inc( dst ).Get( "INC" ) + Ops.operand( dst );
        // INC r16
        private static Instruction Inc( Reg16 dst ) => new Instruction( () => Ops.Inc( dst ), "INC" ) + Ops.operand( dst );
        // INC (HL)
        private static readonly Instruction IncHl = new Instruction( Ops.IncHl, "INC" ) + "(HL)";

        // INC r8
        private static Instruction Dec( Reg8 dst ) => Ops.Dec( dst ).Get( "Dec" ) + Ops.operand( dst );
        // INC r16
        private static Instruction Dec( Reg16 dst ) => new Instruction( () => Ops.Dec( dst ), "Dec" ) + Ops.operand( dst );
        // INC (HL)
        private static readonly Instruction DecHl = new Instruction( Ops.DecHl, "Dec" ) + "(HL)";

        // BIT i, [r8, (HL)]
        private static Instruction Bit( byte bit, RegX target ) => new Instruction( () => Ops.Bit( bit, target ), "BIT" ) + $"{bit}" + Ops.operand8OrAdd16( target );

        // SET i, [r8, (HL)]
        private static Instruction Set( byte bit, RegX target ) => new Instruction( () => Ops.Set( bit, target ), "SET" ) + $"{bit}" + Ops.operand8OrAdd16( target );

        // SET i, [r8, (HL)]
        private static Instruction Res( byte bit, RegX target ) => new Instruction( () => Ops.Res( bit, target ), "RES" ) + $"{bit}" + Ops.operand8OrAdd16( target );

        // XOR A, [r8, (HL)]
        private static Instruction Xor( RegX target ) => new Instruction( () => Ops.Xor( target ), "XOR" ) + "A" + Ops.operand( target );

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
        private static readonly Instruction LdhImmA = new Instruction( Ops.LdhImmA, "LD" ) + Ops.operandDB8x( "0xFF00+" ) + "A";

        // LD (a16), SP
        private static readonly Instruction LdImm16Sp = new Instruction( Ops.LdImm16Sp, "LD" ) + Ops.addrDB16 + "SP";

        // LD (a16), A
        private static readonly Instruction LdImmAddrA = new Instruction( Ops.LdImmAddrA, "LD" ) + Ops.addrDB16 + "A";

        // LD A, (a16)
        private static readonly Instruction LdAImmAddr = new Instruction( Ops.LdAImmAddr, "LD" ) + "A" + Ops.addrDB16;

        // LD HL,SP + r8 - 3 cycles
        private static readonly Instruction LdHlSpR8 = new Instruction( Ops.LdHlSpR8, "LD" ) + "HL" + Ops.operandE8x("SP+");


        // ADD A, [r8 (HL)]
        private static Instruction Add( RegX src ) => new Instruction( () => Ops.Add( src ), "ADD" ) + "A" + Ops.operand8OrAdd16( src );

        // ADD HL, r16
        private static Instruction AddHl( Reg16 src ) => new Instruction( () => Ops.AddHl( src ), "ADD" ) + "HL" + Ops.operand( src );

        // ADD A, db8
        private static readonly Instruction AddImm8 = new Instruction( () => Ops.AddImm8( carry: 0 ), "ADD" ) + "A" + Ops.operandDB8;

        // ADC A, db8
        private static readonly Instruction AdcImm8 = new Instruction( () => Ops.AddImm8( carry: 1 ), "ADC" ) + "A" + Ops.operandDB8;
        
        // ADD SP, R8
        private static readonly Instruction AddSpR8 = new Instruction( Ops.AddSpR8, "ADD" ) + "SP" + Ops.operandE8;

        // ADD A, [r8 (HL)]
        private static Instruction Adc( RegX src ) => new Instruction( () => Ops.Add( src, carry: 1 ), "ADC" ) + "A" + Ops.operand8OrAdd16( src );


        // SUB A, [r8 (HL)]
        private static Instruction Sub( RegX src ) => new Instruction( () => Ops.Sub( src ), "SUB" ) + "A" + Ops.operand8OrAdd16( src );

        // SUB A, db8
        private static readonly Instruction SubImm8 = new Instruction( () => Ops.SubImm8( carry: 0 ), "SUB" ) + "A" + Ops.operandDB8;

        // SBC A, [r8 (HL)]
        private static Instruction Sbc( RegX src ) => new Instruction( () => Ops.Sub( src, carry: 1 ), "SBC" ) + "A" + Ops.operand8OrAdd16( src );

        // SBC A, db8
        private static readonly Instruction SbcImm8 = new Instruction( () => Ops.SubImm8( carry: 1 ), "SBC" ) + "A" + Ops.operandDB8;

        // AND A, [r8 (HL)]
        private static Instruction And( RegX src ) => new Instruction( () => Ops.And( src ), "AND" ) + "A" + Ops.operand8OrAdd16( src );

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
        private static Instruction CallCc( Ops.Condition cc, string flag ) => new Instruction( () => Ops.Call( cc ), "CALL" ) + flag + Ops.operandDB16;

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

		public IEnumerator<Instruction> GetEnumerator()
		{
			return ( (IEnumerable<Instruction>)m_instructions ).GetEnumerator();
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
