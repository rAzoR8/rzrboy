﻿namespace rzr
{
	public static class Asm
	{
		// Operands
		public static Operand D8( byte val ) => new Operand( OperandType.d8, val );
		public static Operand R8( sbyte val ) => new Operand( OperandType.r8, val );
		public static Operand R8( byte val ) => new Operand( OperandType.r8, val );
		public static Operand D16( byte lsb, byte msb ) => new Operand( OperandType.d16, msb.Combine( lsb ) );
		public static Operand Io8( byte val ) => new Operand( OperandType.io8, val );
		public static Operand RstAdr( byte val ) => new Operand( OperandType.RstAddr, val );
		public static Operand BitIdx( byte idx ) => new Operand( OperandType.BitIdx, idx );

		// Instructions
		public static AsmInstr Nop() => new AsmInstr( InstrType.Nop );
		public static AsmInstr Stop( params Operand[] ops ) => new AsmInstr( InstrType.Stop, ops );
		public static AsmInstr Halt() => new AsmInstr( InstrType.Halt );
		public static AsmInstr Ld( Operand lhs, Operand rhs ) => new AsmInstr( InstrType.Ld, lhs, rhs );
		public static AsmInstr Jr( params Operand[] ops ) => new AsmInstr( InstrType.Jr, ops );
		public static AsmInstr Jp( params Operand[] ops ) => new AsmInstr( InstrType.Jp, ops );
		public static AsmInstr Inc( Operand lhs ) => new AsmInstr( InstrType.Inc, lhs );
		public static AsmInstr Dec( Operand lhs ) => new AsmInstr( InstrType.Dec, lhs );
		public static AsmInstr Add( params Operand[] ops ) => new AsmInstr( InstrType.Add, ops );
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
				(1, _ ) when y < 4 => Ld( BcDeHlSp[y], D16( mem[pc++], mem[pc++] ) ),
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
				(9, _ ) when y < 4 => Add( HL, BcDeHlSp[y] ),
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
				(_, _ ) when x < 8 && y >= 4 && y < 8 => Ld( BDHAdrHl[y - 4], BCDEHLAdrHlA[x] ),
				// LD [C E L A], [B C D E H L (HL) A]
				(_, _ ) when x >= 8 && y >= 4 && y < 8 => Ld( CELA[y - 4], BCDEHLAdrHlA[x - 8] ),
				// 0x80->0x88 ADD A, [B C D E H L (HL) A]
				(_, 8 ) when x < 8 => Add( BCDEHLAdrHlA[x] ),
				// 0x88->0x8F ADC A, [B C D E H L (HL) A]
				(_, 8 ) when x >= 8 => Adc( BCDEHLAdrHlA[x - 8] ),
				// 0x90->0x98 SUB A, [B C D E H L (HL) A]
				(_, 9 ) when x < 8 => Sub( BCDEHLAdrHlA[x] ),
				// 0x98->0x9F SBC A, [B C D E H L (HL) A]
				(_, 9 ) when x >= 8 => Sbc( BCDEHLAdrHlA[x - 8] ),
				// 0xA0->0xA8 AND A, [B C D E H L (HL) A]
				(_, 0xA ) when x < 8 => And( BCDEHLAdrHlA[x] ),
				// 0xA8->0xAF XOR A, [B C D E H L (HL) A]
				(_, 0xA ) when x >= 8 => Xor( BCDEHLAdrHlA[x - 8] ),
				// 0xB0->0xB8 AND A, [B C D E H L (HL) A]
				(_, 0xB ) when x < 8 => Or( BCDEHLAdrHlA[x] ),
				// 0xB8->0xBF XOR A, [B C D E H L (HL) A]
				(_, 0xB ) when x >= 8 => Cp( BCDEHLAdrHlA[x - 8] ),
				// 0xC0 RET NZ
				(0, 0xC ) => Ret( condNZ ),
				// 0xC0 RET NC
				(0, 0xD ) => Ret( condNC ),
				// 0xE0 LD (0xFF00+db8), A
				(0, 0xE ) => Ld( Io8( mem[pc++] ), A ),
				// 0xF0 LD (0xFF00+db8), A
				(0, 0xF ) => Ld( A, Io8( mem[pc++] ) ),
				// 0xC1->0xF1 POP [BC DE HL]
				(1, _ ) when y >= 0xC && y < 0xF => Pop( BcDeHlSp[y - 0xC] ),
				// 0xF1 POP AF
				(1, 0xF ) => Pop( AF ),
				// JP NZ, a16
				(2, 0xC ) => Jp( condNZ, D16( mem[pc++], mem[pc++] ) ),
				// JP NC, a16
				(2, 0xD ) => Jp( condNC, D16( mem[pc++], mem[pc++] ) ),
				// 0xE2 LD (0xFF00+C), A
				(2, 0xE ) => Ld( IoC, A ),
				// 0xF2 LD A, (0xFF00+C)
				(2, 0xF ) => Ld( A, IoC ),
				// 0xC3 JP a16
				(3, 0xC ) => Jp( D16( mem[pc++], mem[pc++] ) ),
				// 0xF3 DI
				(3, 0xF ) => Di(),
				// 0xC4 CALL NZ, a16
				(4, 0xC ) => Call( condNZ, D16( mem[pc++], mem[pc++] ) ),
				// 0xD4 CALL NC, a16
				(4, 0xD ) => Call( condNC, D16( mem[pc++], mem[pc++] ) ),
				// 0xC5->0xF5 PUSH [BC DE HL]
				(5, _ ) when y >= 0xC && y < 0xF => Push( BcDeHlSp[y - 0xC] ),
				// 0xF5 POP AF
				(5, 0xF ) => Push( AF ),
				// 0xC6 ADD A, db8
				(6, 0xC ) => Add( D8( mem[pc++] ) ),
				// 0xD6 SUB A, db8
				(6, 0xD ) => Sub( D8( mem[pc++] ) ),
				// 0xE6 AND A, db8
				(6, 0xE ) => And( D8( mem[pc++] ) ),
				// 0xF6 OR A, db8
				(6, 0xF ) => Or( D8( mem[pc++] ) ),
				// 0xC7 RST 00h
				(7, 0xC ) => Rst( RstAdr( 0x00 ) ),
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
				(8, 0xE ) => Add( SP, R8( mem[pc++] ) ),
				// 0xF8 LD HL, SP + r8
				(8, 0xF ) => Ld( HL, new Operand( OperandType.SPr8, mem[pc++] ) ),
				// 0xC9 RET
				(9, 0xC ) => Ret(),
				// 0xD9 RETI
				(9, 0xD ) => Reti(),
				// 0xE9 JP HL
				(9, 0xE ) => Jp( HL ),
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
				(0xB, 0xF ) => Ei(),
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
		public static AsmInstr Bit( byte idx, OperandType reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );
		public static AsmInstr Bit( byte idx, Operand reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );

		public static AsmInstr Res( Operand idx, Operand reg ) => new AsmInstr( InstrType.Bit, idx, reg );
		public static AsmInstr Res( byte idx, OperandType reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );
		public static AsmInstr Res( byte idx, Operand reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );

		public static AsmInstr Set( Operand idx, Operand reg ) => new AsmInstr( InstrType.Bit, idx, reg );
		public static AsmInstr Set( byte idx, OperandType reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );
		public static AsmInstr Set( byte idx, Operand reg ) => new AsmInstr( InstrType.Bit, BitIdx( idx ), reg );

		private static AsmInstr Ext( ref ushort pc, ISection mem )
		{
			//++pc; // caller cant modify ref param, so we need to do it here

			byte op = mem[pc++];
			(byte x, byte y) = op.Nibbles();

			return (x, y) switch
			{
				(_, 0 ) when x < 8 => Rlc( BCDEHLAdrHlA[x] ),
				(_, 0 ) when x >= 8 => Rrc( BCDEHLAdrHlA[x - 8] ),
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
}