namespace rzr
{
	public static class Asm
	{

		public static Operand D8( byte val ) => new Operand( OperandType.d8, val );
		public static Operand R8( sbyte val ) => new Operand( OperandType.r8, val );
		public static Operand R8( byte val ) => new Operand( OperandType.r8, val );
		public static Operand D16( byte msb, byte lsb ) => new Operand( OperandType.d16, msb.Combine( lsb ) );
		public static Operand Io8( byte val ) => new Operand( OperandType.io8, val );
		public static Operand RstAdr( byte val ) => new Operand( OperandType.RstAddr, val );
		public static Operand BitIdx( byte idx ) => new Operand( OperandType.BitIdx, idx );

		public static AsmInstr Nop() => new AsmInstr( InstrType.Nop );
		public static AsmInstr Stop( params Operand[] ops ) => new AsmInstr( InstrType.Stop, ops );
		public static AsmInstr Halt() => new AsmInstr( InstrType.Halt );
		public static AsmInstr Ld( Operand lhs, Operand rhs ) => new AsmInstr( InstrType.Ld, lhs, rhs );
		public static AsmInstr Jr( params Operand[] ops ) => new AsmInstr( InstrType.Jr, ops);
		public static AsmInstr Jp( params Operand[] ops ) => new AsmInstr( InstrType.Jp, ops);
		public static AsmInstr Inc( Operand lhs ) => new AsmInstr( InstrType.Inc, lhs );
		public static AsmInstr Dec( Operand lhs ) => new AsmInstr( InstrType.Dec, lhs );
		public static AsmInstr Add( params Operand[] ops ) => new AsmInstr( InstrType.Add, ops);
		public static AsmInstr Adc( params Operand[] ops ) => new AsmInstr( InstrType.Adc, ops );
		public static AsmInstr Sub( Operand rhs ) => new AsmInstr( InstrType.Sub, rhs );
		public static AsmInstr Sbc( Operand rhs ) => new AsmInstr( InstrType.Sbc, rhs );
		public static AsmInstr And( Operand rhs ) => new AsmInstr( InstrType.And, rhs );
		public static AsmInstr Or( Operand rhs ) => new AsmInstr( InstrType.Or, rhs );
		public static AsmInstr Xor( Operand rhs ) => new AsmInstr( InstrType.Xor, rhs );
		public static AsmInstr Cp( Operand rhs ) => new AsmInstr( InstrType.Cp, rhs );
		public static AsmInstr Ret( params Operand[] ops ) => new AsmInstr( InstrType.Ret, ops );
		public static AsmInstr Reti() => new AsmInstr( InstrType.Reti );
		public static AsmInstr Pop( Operand lhs ) => new AsmInstr( InstrType.Pop, lhs );
		public static AsmInstr Push( Operand lhs ) => new AsmInstr( InstrType.Push, lhs );
		public static AsmInstr Call( params Operand[] ops ) => new AsmInstr( InstrType.Call, ops );
		public static AsmInstr Di() => new AsmInstr( InstrType.Di );
		public static AsmInstr Ei() => new AsmInstr( InstrType.Ei );
		public static AsmInstr Rlca() => new AsmInstr( InstrType.Rlca );
		public static AsmInstr Rla() => new AsmInstr( InstrType.Rla );
		public static AsmInstr Daa() => new AsmInstr( InstrType.Daa );
		public static AsmInstr Scf() => new AsmInstr( InstrType.Scf );
		public static AsmInstr Rrca() => new AsmInstr( InstrType.Rrca );
		public static AsmInstr Rra() => new AsmInstr( InstrType.Rra );
		public static AsmInstr Cpl() => new AsmInstr( InstrType.Cpl );
		public static AsmInstr Ccf() => new AsmInstr( InstrType.Ccf );
		public static AsmInstr Rst( Operand vec ) => new AsmInstr( InstrType.Rst, vec );


		private static readonly OperandType[] BcDeHlSp = { OperandType.BC, OperandType.DE, OperandType.HL, OperandType.HL };
		private static readonly OperandType[] adrBcDeHlID = { OperandType.AdrBC, OperandType.AdrDE, OperandType.AdrHLI, OperandType.AdrHLD };
		private static readonly OperandType[] BDHAdrHl = { OperandType.B, OperandType.D, OperandType.H, OperandType.AdrHL };
		private static readonly OperandType[] CELA = { OperandType.C, OperandType.E, OperandType.L, OperandType.A };
		private static readonly OperandType[] BCDEHLAdrHlA = { OperandType.B, OperandType.C, OperandType.D, OperandType.E, OperandType.H, OperandType.L, OperandType.AdrHL, OperandType.A };

		private const OperandType A = OperandType.A;
		private const OperandType B = OperandType.B;
		private const OperandType C = OperandType.C;
		private const OperandType D = OperandType.D;
		private const OperandType E = OperandType.E;
		private const OperandType H = OperandType.H;
		private const OperandType L = OperandType.L;

		private const OperandType BC = OperandType.BC;
		private const OperandType DE = OperandType.DE;
		private const OperandType HL = OperandType.HL;
		private const OperandType SP = OperandType.SP;
		private const OperandType AF = OperandType.AF;

		private const OperandType IoC = OperandType.ioC;

		private const OperandType condC = OperandType.condC;
		private const OperandType condNC = OperandType.condNC;
		private const OperandType condZ = OperandType.condZ;
		private const OperandType condNZ = OperandType.condNZ;

		public static AsmInstr Disassemble( ref ushort pc, ISection mem )
		{
			(byte x, byte y) = mem[pc++].Nibbles();
			return (x, y) switch
			{
				// 0x00 NOP
				(0, 0 ) => Nop(),
				// 0x10 STOP
				(0, 1 ) => Stop(),
				// 0x20 JR NZ, r8
				(0, 2 ) => Jr( condNZ, R8( mem[pc++] ) ),
				// 0x30 JR NC, r8
				(0, 3 ) => Jr( condNC, R8( mem[pc++] ) ),
				// 0x01->0x31 -> LD BC, d16
				(1, _ ) when y < 4 => Ld( BcDeHlSp[y], D16( mem[pc]++, mem[pc]++ ) ),
				// 0x02->0x32 LD (BC), A
				(2, _ ) when y < 4 => Ld( adrBcDeHlID[y], A ),
				// 0x03->0x33 INC [BC DE HL SP]
				(3, _ ) when y < 4 => Inc( BcDeHlSp[y] ),
				// 0x04->0x34 INC [B D H (HL)]
				(4, _ ) when y < 4 => Inc( BDHAdrHl[y] ),
				// 0x05->0x35 DEC [BC DE HL SP]
				(5, _ ) when y < 4 => Dec( BcDeHlSp[y] ),
				// 0x6->0x36 LD [B D H (HL)], db8
				(6, _ ) when y < 4 => Ld( BDHAdrHl[y], D8( mem[pc++] ) ),
				// 0x07
				(7, 0 ) => Rlca(),
				// 0x17
				(7, 1 ) => Rla(),
				// 0x27
				(7, 2 ) => Daa(),
				// 0x37
				(7, 3 ) => Scf(),
				// 0x08 LD (a16), SP
				(8, 0 ) => Ld( D16( mem[pc++], mem[pc++] ), SP ),
				// 0x18 JR r8
				(8, 1 ) => Jr( R8( mem[pc++] ) ),
				// 0x28 JR Z, r8
				(8, 2 ) => Jr( condZ, R8( mem[pc++] ) ),
				// 0x38 JR C, r8
				(8, 3 ) => Jr( condC, R8( mem[pc++] ) ),
				// 0x09-0x39 ADD HL, [BC DE HL SP]
				(9, _) when y < 4 => Add( HL, BcDeHlSp[y]),
				// 0x0A->0x3A LD A, [(BC) (DE) (HL+) (HL-)]
				(0xA, _ ) when y < 4 => Ld( A, adrBcDeHlID[y] ),
				// 0x0B->0x3B DEC [BC DE HL SP]
				(0xB, _ ) when y < 4 => Dec( BcDeHlSp[y] ),
				// 0x0C->0x3C INC [C E L A]
				(0xC, _ ) when y < 4 => Inc( CELA[y] ),
				// 0x0D->0x3D Dec [C E L A]
				(0xD, _ ) when y < 4 => Dec( CELA[y] ),
				// 0x0E->0x3E LD [C E L A], d8
				(0xE, _ ) when y < 4 => Ld( CELA[y], D8( mem[pc++] ) ),
				// 0x0F
				(0xF, 0 ) => Rrca(),
				// 0x1F
				(0xF, 1 ) => Rra(),
				// 0x2F
				(0xF, 2 ) => Cpl(),
				// 0x3F
				(0xF, 3 ) => Ccf(),
				// 0x76 HALT
				(6, 7 ) => Halt(),
				// LD [B D H (HL)], [B C D E H L (HL) A]
				(_, _ ) when x < 8 && y >= 4 && y < 8 => Ld( BDHAdrHl[y-4], BCDEHLAdrHlA[x] ),
				// LD [C E L A], [B C D E H L (HL) A]
				(_, _ ) when x >= 8 && y >= 4 && y < 8 => Ld( CELA[y-4], BCDEHLAdrHlA[x-8] ),
				// 0x80->0x88 ADD A, [B C D E H L (HL) A]
				(_, 8 ) when x < 8 => Add( BCDEHLAdrHlA[x] ),
				// 0x88->0x8F ADC A, [B C D E H L (HL) A]
				(_, 8 ) when x >= 8 => Adc( BCDEHLAdrHlA[x-8] ),
				// 0x90->0x98 SUB A, [B C D E H L (HL) A]
				(_, 9 ) when x < 8 => Sub( BCDEHLAdrHlA[x] ),
				// 0x98->0x9F SBC A, [B C D E H L (HL) A]
				(_, 9 ) when x >= 8 => Sbc( BCDEHLAdrHlA[x-8] ),
				// 0xA0->0xA8 AND A, [B C D E H L (HL) A]
				(_, 0xA) when x < 8 => And( BCDEHLAdrHlA[x] ),
				// 0xA8->0xAF XOR A, [B C D E H L (HL) A]
				(_, 0xA ) when x >= 8 => Xor( BCDEHLAdrHlA[x-8] ),
				// 0xB0->0xB8 AND A, [B C D E H L (HL) A]
				(_, 0xB ) when x < 8 => Or( BCDEHLAdrHlA[x] ),
				// 0xB8->0xBF XOR A, [B C D E H L (HL) A]
				(_, 0xB ) when x >= 8 => Cp( BCDEHLAdrHlA[x-8] ),
				// 0xC0 RET NZ
				(0, 0xC) => Ret(condNZ),
				// 0xC0 RET NC
				(0, 0xD ) => Ret( condNC ),
				// 0xE0 LD (0xFF00+db8), A
				(0, 0xE ) => Ld( Io8( mem[pc++] ), A ),
				// 0xF0 LD (0xFF00+db8), A
				(0, 0xF ) => Ld( A, Io8( mem[pc++] ) ),
				// 0xC1->0xF1 POP [BC DE HL]
				(1, _ ) when y >= 0xC && y < 0xF => Pop( BcDeHlSp[y-0xC] ),
				// 0xF1 POP AF
				(1, 0xF ) => Pop( AF ),
				// JP NZ, a16
				(2, 0xC) => Jp(condNZ, D16(mem[pc++], mem[pc++])),
				// JP NC, a16
				(2, 0xD ) => Jp( condNC, D16( mem[pc++], mem[pc++] ) ),
				// 0xE2 LD (0xFF00+C), A
				(2, 0xE ) => Ld( IoC, A ),
				// 0xF2 LD A, (0xFF00+C)
				(2, 0xF ) => Ld( A, IoC ),
				// 0xC3 JP a16
				(3, 0xC ) => Jp( D16( mem[pc++], mem[pc++] ) ),
				// 0xF3 DI
				(3, 0xF) => Di(),
				// 0xC4 CALL NZ, a16
				(4, 0xC) => Call(condNZ, D16( mem[pc++], mem[pc++] ) ),
				// 0xD4 CALL NC, a16
				(4, 0xD ) => Call( condNC, D16( mem[pc++], mem[pc++] ) ),
				// 0xC5->0xF5 PUSH [BC DE HL]
				(5, _ ) when y >= 0xC && y < 0xF => Push( BcDeHlSp[y-0xC] ),
				// 0xF5 POP AF
				(5, 0xF ) => Push( AF ),
				// 0xC6 ADD A, db8
				(6, 0xC ) => Add( D8(mem[pc++]) ),
				// 0xD6 SUB A, db8
				(6, 0xD ) => Sub( D8( mem[pc++] ) ),
				// 0xE6 AND A, db8
				(6, 0xE ) => And( D8( mem[pc++] ) ),
				// 0xF6 OR A, db8
				(6, 0xF ) => Or( D8( mem[pc++] ) ),
				// 0xC7 RST 00h
				(7, 0xC) => Rst( RstAdr(0x00) ),
				// 0xD7 RST 10h
				(7, 0xD ) => Rst( RstAdr( 0x10 ) ),
				// 0xE7 RST 10h
				(7, 0xE ) => Rst( RstAdr( 0x20 ) ),
				// 0xF7 RST 30h
				(7, 0xF ) => Rst( RstAdr( 0x30 ) ),
				// 0xC8 RET Z
				(8, 0xC ) => Ret( condZ ),
				// 0xD8 RET C
				(8, 0xD ) => Ret( condC ),
				// 0xE8 ADD SP, r8
				(8, 0xE ) => Add( SP, R8(mem[pc++]) ),
				// 0xF8 LD HL, SP + r8
				(8, 0xF ) => Ld( HL, new Operand( OperandType.SPr8, mem[pc++] ) ),
				// 0xC9 RET
				(9, 0xC) => Ret(),
				// 0xD9 RETI
				(9, 0xD) => Reti(),
				// 0xE9 JP HL
				(9, 0xE) => Jp(HL),
				// 0xF9 LD SP, HL
				(9, 0xF ) => Ld( SP, HL ),
				// 0xCA JP Z, A16
				(0xA, 0xC ) => Jp( condZ, D16( mem[pc++], mem[pc++] ) ),
				// 0xCA JP C, A16
				(0xA, 0xD ) => Jp( condC, D16( mem[pc++], mem[pc++] ) ),
				// 0xEA LD (a16), A
				(0xA, 0xE ) => Ld( D16( mem[pc++], mem[pc++] ), A ),
				// 0xEA LD A, (a16)
				(0xA, 0xF ) => Ld( A, D16( mem[pc++], mem[pc++] ) ),
				// 0xCB PREFIX / EXT
				(0xB, 0xC ) => Ext( ref pc, mem ),
				// 0xFB EI
				(0xB, 0xF) => Ei(),
				// 0xCC CALL Z, a16
				(0xC, 0xC ) => Call( condZ, D16( mem[pc++], mem[pc++] ) ),
				// 0xDC CALL C, a16
				(0xC, 0xD ) => Call( condC, D16( mem[pc++], mem[pc++] ) ),
				// 0xCD CALL a16
				(0xD, 0xC ) => Call( D16( mem[pc++], mem[pc++] ) ),
				// 0xCE ADC A, db8
				(0xE, 0xC ) => Adc( D8( mem[pc++] ) ),
				// 0xDE SBC A, db8
				(0xE, 0xD ) => Sbc( D8( mem[pc++] ) ),
				// 0xEE XOR A, db8
				(0xE, 0xE ) => Xor( D8( mem[pc++] ) ),
				// 0xFE CP A, db8
				(0xE, 0xF ) => Cp( D8( mem[pc++] ) ),
				// 0xCF RST 08h
				(0xF, 0xC ) => Rst( RstAdr( 0x08 ) ),
				// 0xDF RST 18h
				(0xF, 0xD ) => Rst( RstAdr( 0x18 ) ),
				// 0xEF RST 18h
				(0xF, 0xE ) => Rst( RstAdr( 0x28 ) ),
				// 0xFF RST 38h
				(0xF, 0xF ) => Rst( RstAdr( 0x38 ) ),
				_ => AsmInstr.Invalid
			};
		}

		public static AsmInstr Rlc( Operand rhs ) => new AsmInstr( InstrType.Rlc, rhs );
		public static AsmInstr Rrc( Operand rhs ) => new AsmInstr( InstrType.Rrc, rhs );
		public static AsmInstr Rl( Operand rhs ) => new AsmInstr( InstrType.Rl, rhs );
		public static AsmInstr Rr( Operand rhs ) => new AsmInstr( InstrType.Rr, rhs );
		public static AsmInstr Sla( Operand rhs ) => new AsmInstr( InstrType.Sla, rhs );
		public static AsmInstr Sra( Operand rhs ) => new AsmInstr( InstrType.Sra, rhs );

		public static AsmInstr Swap( Operand rhs ) => new AsmInstr( InstrType.Swap, rhs );
		public static AsmInstr Srl( Operand rhs ) => new AsmInstr( InstrType.Srl, rhs );

		public static AsmInstr Bit( Operand idx, Operand reg ) => new AsmInstr( InstrType.Bit, idx, reg );
		public static AsmInstr Bit( byte idx, OperandType reg ) => new AsmInstr( InstrType.Bit, BitIdx(idx), reg );
		public static AsmInstr Bit( byte idx, Operand reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );

		public static AsmInstr Res( Operand idx, Operand reg ) => new AsmInstr( InstrType.Bit, idx, reg );
		public static AsmInstr Res( byte idx, OperandType reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );
		public static AsmInstr Res( byte idx, Operand reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );

		public static AsmInstr Set( Operand idx, Operand reg ) => new AsmInstr( InstrType.Bit, idx, reg );
		public static AsmInstr Set( byte idx, OperandType reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );
		public static AsmInstr Set( byte idx, Operand reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );

		private static AsmInstr Ext( ref ushort pc, ISection mem )
		{ 
			++pc; // caller cant modify ref param, so we need to do it here

			byte op = mem[pc++];
			( byte x, byte y) = op.Nibbles();

			return (x, y) switch
			{
				(_, 0 ) when x < 8 => Rlc( BCDEHLAdrHlA[x] ),
				(_, 0 ) when x >= 8 => Rrc( BCDEHLAdrHlA[x-8] ),
				(_, 1 ) when x < 8 => Rl( BCDEHLAdrHlA[x] ),
				(_, 1 ) when x >= 8 => Rr( BCDEHLAdrHlA[x - 8] ),
				(_, 2 ) when x < 8 => Sla( BCDEHLAdrHlA[x] ),
				(_, 2 ) when x >= 8 => Sra( BCDEHLAdrHlA[x - 8] ),
				(_, 3 ) when x < 8 => Swap( BCDEHLAdrHlA[x] ),
				(_, 3 ) when x >= 8 => Srl( BCDEHLAdrHlA[x - 8] ),
				(_, _ ) when x < 8 && y >= 4 && y < 8 => Bit( (byte)( ( op - 0x40 ) / 8 ), BCDEHLAdrHlA[x] ),
				(_, _ ) when x >= 8 && y >= 4 && y < 8 => Bit( (byte)( ( op - 0x40 ) / 8 ), BCDEHLAdrHlA[x - 8] ),
				(_, _ ) when x < 8 && y >= 8 && y < 0xC => Res( (byte)( ( op - 0x80 ) / 8 ), BCDEHLAdrHlA[x] ),
				(_, _ ) when x >= 8 && y >= 8 && y < 0xC => Res( (byte)( ( op - 0x80 ) / 8 ), BCDEHLAdrHlA[x - 8] ),
				(_, _ ) when x < 8 && y >= 0xC => Set( (byte)( ( op - 0xC0 ) / 8 ), BCDEHLAdrHlA[x] ),
				(_, _ ) when x >= 8 && y >= 0xC => Set( (byte)( ( op - 0xC0 ) / 8 ), BCDEHLAdrHlA[x - 8] ),

				_ => AsmInstr.Invalid
			};
		}
	} // !Asm

	public static class Ops
	{
		/// <summary>
		/// DISASSEMBLERS
		/// </summary>

		public static Dis mnemonic( string str ) => ( ref ushort pc, ISection mem ) => str;
		public static Dis operand( RegX reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
		public static Dis operand( Reg8 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
		public static Dis operand( Reg16 reg ) => ( ref ushort pc, ISection mem ) => reg.ToString();
		public static Dis addr( Reg16 reg ) => ( ref ushort pc, ISection mem ) => $"({reg})";
		public static Dis operand8OrAdd16( RegX reg ) => reg.Is8() ? operand( reg ) : addr( reg.To16() );

		public static readonly Dis operandE8 = ( ref ushort pc, ISection mem ) => $"{(sbyte)mem[pc++]}";
		public static Dis operandE8x( string prefix ) => ( ref ushort pc, ISection mem ) => $"{prefix}{(sbyte)mem[pc++]}";

		public static readonly Dis operandDB8 = ( ref ushort pc, ISection mem ) => $"0x{mem[pc++]:X2}";
		public static Dis operandDB8x( string prefix ) => ( ref ushort pc, ISection mem ) => $"{prefix}{mem[pc++]:X2}";

		public static readonly Dis operandDB16 = ( ref ushort pc, ISection mem ) =>
		{
			string str = $"0x{mem[(ushort)( pc + 1 )]:X2}{mem[pc]:X2}";
			pc += 2;
			return str;
		};

		public readonly static Dis addrDB16 = ( ref ushort pc, ISection mem ) => $"({operandDB16( ref pc, mem )})";

		/// <summary>
		/// OPERATIONS
		/// </summary>

		public static readonly Op Nop = ( reg, mem ) => { };

		public static readonly Op Halt = ( reg, mem ) =>
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

		public static IEnumerable<Op> Stop()
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
			yield return ( reg, mem ) => { reg[dst] = reg[src]; };
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

		public static IEnumerable<Op> LdRegOrAddr( RegX dst, RegX src )
		{
			if( dst.Is8() && src.Is8() ) return LdReg8( dst.To8(), src.To8() );
			if( dst.Is16() && src.Is16() ) return LdReg16( dst.To16(), src.To16() );
			if( dst.Is8() && src.Is16() ) return LdAddr( dst.To8(), src.To16() );
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

		// LD HL,SP + r8 - 3 cycles
		public static IEnumerable<Op> LdHlSpR8()
		{
			byte rhs = 0; ushort res = 0;
			yield return ( reg, mem ) => rhs = mem[reg.PC++];
			yield return ( reg, mem ) => res = SignedAddHelper( reg, reg.SP, (sbyte)rhs );
			yield return ( reg, mem ) => reg.HL = res;
		}

		// LD (a16), A - 4 cycles
		public static IEnumerable<Op> LdImmAddrA( )
		{
			ushort nn = 0;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			yield return Nop;
			yield return ( reg, mem ) => mem[nn] = reg.A;
		}

		// LD A, (a16) - 4 cycles
		public static IEnumerable<Op> LdAImmAddr()
		{
			ushort nn = 0;
			yield return ( reg, mem ) => nn = mem[reg.PC++];
			yield return ( reg, mem ) => nn |= (ushort)( mem[reg.PC++] << 8 );
			yield return Nop;
			yield return ( reg, mem ) => reg.A = mem[nn];
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

		private static ushort SignedAddHelper( Reg reg, ushort lhs, sbyte rhs )
		{
			var res = lhs + rhs;

			reg.Sub = false;
			reg.Zero = false; // Accumulator not used
			reg.Carry = ( res & 0b1_0000_0000 ) != 0;
			reg.HalfCarry = ( res & 0b1_0000 ) != 0;

			return (ushort)( res );
		} 

		// ADD SP, R8 - 4 cycles
		public static IEnumerable<Op> AddSpR8()
		{
			byte rhs = 0; ushort res = 0;
			yield return ( reg, mem ) => rhs = mem[reg.PC++];
			yield return ( reg, mem ) => res = SignedAddHelper( reg, reg.SP, (sbyte)rhs );
			yield return Nop; // no idea why this is a 4 cycle op
			yield return ( reg, mem ) => reg.SP = res;
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
			yield return ( Reg reg, ISection mem ) =>
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

		public static void CpHelper( Reg reg, byte rhs )
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
			if( cc != null ) yield return ( reg, mem ) => takeBranch = cc( reg );
			if( takeBranch )
			{
				yield return ( reg, mem ) => { reg.PC = nn; };
			}
		}

		// JR cc, e8 2/3 ycles
		public static IEnumerable<Op> JrImm( Condition? cc = null )
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
		public static IEnumerable<Op> Xor( RegX src )
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
			byte res = (byte)( val + 1 );
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
			if( takeBranch )
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
				yield return ( reg, mem ) => reg.PC = binutil.SetLsb( reg.PC, lsb );
				yield return ( reg, mem ) => reg.PC = binutil.SetMsb( reg.PC, msb );
			}
		}

		// RETI 4 cycles
		public static IEnumerable<Op> Reti()
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
			yield return ( reg, mem ) => reg.PC = binutil.Combine( 0x00, vec );
		}

		// CCF
		public static IEnumerable<Op> Ccf()
		{
			yield return ( reg, mem ) => reg.SetFlags( Z: reg.Zero, N: false, H: false, C: !reg.Carry );
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
			if( reg.Carry ) res |= ( 1 << 7 );

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
}
