using System.Collections;
using static rzr.AsmOperandTypes;

namespace rzr
{
	public abstract class AsmBuilder
	{
		// absolute InstructionPointer (IP) in the current instruction stream (ROM)
		public uint IP { get; set; }
		// bank 0 is always mapped, so any PC pointing to the selectable bank X is mapped to adsress 0x4000+x 
		public ushort PC => (ushort)( IP < Mbc.RomBankSize ? IP : Mbc.RomBankSize + ( IP % Mbc.RomBankSize ) );
		// dummy bank index based on IP
		public virtual byte BankIdx { get => (byte)( IP / Mbc.RomBankSize ); protected set { } } // { get; protected set; } =>;

		public ushort Instr( InstrType instr, params AsmOperand[] operands )
		{
			return Consume( new AsmInstr( instr, operands ) );
		}

		public abstract ushort Consume( AsmInstr instr );

		public static readonly Atype A;
		public static readonly Btype B;
		public static readonly Ctype C;
		public static readonly Dtype D;
		public static readonly Etype E;
		public static readonly Htype H;
		public static readonly Ltype L;

		public static readonly BCtype BC;
		public static readonly DEtype DE;
		public static readonly HLtype HL;
		public static readonly SPtype SP;
		public static readonly AFtype AF; // Push/Pop only

		public static readonly AdrBCtype adrBC;
		public static readonly AdrDEtype adrDE;
		public static readonly AdrHLtype adrHL;
		public static readonly AdrHLitype adrHLi;
		public static readonly AdrHLdtype adrHLd;
		public static readonly AdrCtype adrC; // IoC

		public static readonly CondZtype isZ;
		public static readonly CondNZtype isNZ;
		public static readonly CondCtype isC;
		public static readonly CondNCtype isNC;

		public ushort Nop() => Instr( InstrType.Nop );
		public ushort Stop( byte corrupt = 0x00 ) => Instr( InstrType.Stop, Asm.D8( corrupt ) );
		public ushort Halt() => Instr( InstrType.Halt );

		// LD [BC DE HL SP], d16
		public ushort Ld( BcDeHlSp lhs, ushort rhs ) => Instr( InstrType.Ld, lhs.Type, Asm.D16( rhs ) );
		// LD [(BC) (DE) (HL+) (HL-)], A
		public ushort Ld( AdrBcDeHliHld lhs, Atype A ) => Instr( InstrType.Ld, lhs.Type, A.Type );
		// LD [B D H (HL)], d8
		public ushort Ld( BDHhl lhs, byte d8 ) => Instr( InstrType.Ld, lhs.Type, Asm.D8( d8 ) );
		// LD [C E L A], d8
		public ushort Ld( CELA lhs, byte d8 ) => Instr( InstrType.Ld, lhs.Type, Asm.D8( d8 ) );
		// LD (a16), SP
		public ushort Ld( Address adr, SPtype SP ) => Instr( InstrType.Ld, Asm.A16( adr ), SP.Type );
		// LD A, [(BC) (DE) (HL+) (HL-)]
		public ushort Ld( Atype A, AdrBcDeHliHld rhs ) => Instr( InstrType.Ld, A.Type, rhs.Type );
		// LD [B D H (HL)], [B D H (HL)]
		public ushort Ld( BDHhl lhs, BDHhl rhs ) => Instr( InstrType.Ld, lhs.Type, rhs.Type );
		// LD [B D H (HL)], [C E L A]
		public ushort Ld( BDHhl lhs, CELA rhs ) => Instr( InstrType.Ld, lhs.Type, rhs.Type );
		// LD [C E L A], [B D H (HL)]
		public ushort Ld( CELA lhs, BDHhl rhs ) => Instr( InstrType.Ld, lhs.Type, rhs.Type );
		// LD [C E L A], [C E L A]
		public ushort Ld( CELA lhs, CELA rhs ) => Instr( InstrType.Ld, lhs.Type, rhs.Type );
		// LDH (a8), A
		public ushort Ldh( byte ioAdr, Atype A ) => Instr( InstrType.Ld, Asm.Io8( ioAdr ), A.Type );
		// LDH A, (a8)
		public ushort Ldh( Atype A, byte ioAdr ) => Instr( InstrType.Ld, A.Type, Asm.Io8( ioAdr ) );
		// LD (C), A (LDH)
		public ushort Ld( AdrCtype adrC, Atype A ) => Instr( InstrType.Ld, adrC.Type, A.Type );
		// LD A, (C) (LDH)
		public ushort Ld( Atype A, AdrCtype adrC ) => Instr( InstrType.Ld, A.Type, adrC.Type );
		// LD (a16), A
		public ushort Ld( Address adr, Atype A ) => Instr( InstrType.Ld, Asm.A16( adr ), A.Type );
		// LD A, (a16)
		public ushort Ld( Atype A, Address adr ) => Instr( InstrType.Ld, A.Type, Asm.A16( adr ) );

		// INC [BC DE HL SP]
		public ushort Inc( BcDeHlSp lhs ) => Instr( InstrType.Inc, lhs.Type );
		// INC [B D H (HL)]
		public ushort Inc( BDHhl lhs ) => Instr( InstrType.Inc, lhs.Type );
		// INC [C E L A]
		public ushort Inc( CELA lhs ) => Instr( InstrType.Inc, lhs.Type );

		// INC [BC DE HL SP]
		public ushort Dec( BcDeHlSp lhs ) => Instr( InstrType.Dec, lhs.Type );
		// INC [B D H (HL)]
		public ushort Dec( BDHhl lhs ) => Instr( InstrType.Dec, lhs.Type );
		// INC [C E L A]
		public ushort Dec( CELA lhs ) => Instr( InstrType.Dec, lhs.Type );

		// ADD A, [B D H (HL)]
		public ushort Add( Atype A, BDHhl rhs ) => Instr( InstrType.Add, A.Type, rhs.Type );
		// ADD A, [C E L A]
		public ushort Add( Atype A, CELA rhs ) => Instr( InstrType.Add, A.Type, rhs.Type );
		// ADD A, d8
		public ushort Add( Atype A, byte d8 ) => Instr( InstrType.Add, A.Type, Asm.D8( d8 ) );
		// ADD HL, [BC DE HL SP]
		public ushort Add( HLtype HL, BcDeHlSp rhs ) => Instr( InstrType.Add, HL.Type, rhs.Type );
		// ADD SP, r8
		public ushort Add( SPtype SP, sbyte rhs ) => Instr( InstrType.Add, SP.Type, Asm.R8( rhs ) );

		// ADC A, [B D H (HL)]
		public ushort Adc( BDHhl rhs ) => Instr( InstrType.Adc, rhs.Type );
		// ADC A, [C E L A]
		public ushort Adc( CELA rhs ) => Instr( InstrType.Adc, rhs.Type );
		// ADC A, d8
		public ushort Adc( byte d8 ) => Instr( InstrType.Adc, Asm.D8( d8 ) );

		// SUB A, [B D H (HL)]
		public ushort Sub( BDHhl rhs ) => Instr( InstrType.Sub, rhs.Type );
		// SUB A, [C E L A]
		public ushort Sub( CELA rhs ) => Instr( InstrType.Sub, rhs.Type );
		// SUB A, d8
		public ushort Sub( byte d8 ) => Instr( InstrType.Sub, Asm.D8( d8 ) );

		// SBC A, [B D H (HL)]
		public ushort Sbc( BDHhl rhs ) => Instr( InstrType.Sbc, rhs.Type );
		// SBC A, [C E L A]
		public ushort Sbc( CELA rhs ) => Instr( InstrType.Sbc, rhs.Type );
		// SBC A, d8
		public ushort Sbc( byte d8 ) => Instr( InstrType.Sbc, Asm.D8( d8 ) );

		// JR r8
		public ushort Jr( sbyte r8 ) => Instr( InstrType.Jr, Asm.R8( r8 ) );
		// JR Z, r8
		public ushort Jr( Condtype cond, sbyte r8 ) => Instr( InstrType.Jr, cond.Type, Asm.R8( r8 ) );

		// JP a16
		public ushort Jp( ushort adr ) => Instr( InstrType.Jp, Asm.A16( adr ) );
		// JP Z, a16
		public ushort Jp( Condtype cond, ushort adr ) => Instr( InstrType.Jp, cond.Type, Asm.A16( adr ) );
		// JP HL
		public ushort Jp( HLtype HL ) => Instr( InstrType.Jp, HL.Type );

		// AND A, [B D H (HL)]
		public ushort And( BDHhl rhs ) => Instr( InstrType.And, rhs.Type );
		// AND A, [C E L A]
		public ushort And( CELA rhs ) => Instr( InstrType.And, rhs.Type );
		// AND A, d8
		public ushort And( byte rhs ) => Instr( InstrType.And, Asm.D8( rhs ) );

		// OR A, [B D H (HL)]
		public ushort Or( BDHhl rhs ) => Instr( InstrType.Or, rhs.Type );
		// OR A, [C E L A]
		public ushort Or( CELA rhs ) => Instr( InstrType.Or, rhs.Type );
		// Or A, d8
		public ushort Or( byte rhs ) => Instr( InstrType.Or, Asm.D8( rhs ) );

		// XOR A, [B D H (HL)]
		public ushort Xor( BDHhl rhs ) => Instr( InstrType.Xor, rhs.Type );
		// XOR A, [C E L A]
		public ushort Xor( CELA rhs ) => Instr( InstrType.Xor, rhs.Type );
		// XOR A, d8
		public ushort Xor( byte rhs ) => Instr( InstrType.Xor, Asm.D8( rhs ) );

		// CP A, [B D H (HL)]
		public ushort Cp( BDHhl rhs ) => Instr( InstrType.Cp, rhs.Type );
		// CP A, [C E L A]
		public ushort Cp( CELA rhs ) => Instr( InstrType.Cp, rhs.Type );
		// CP A, d8
		public ushort Cp( byte rhs ) => Instr( InstrType.Cp, Asm.D8( rhs ) );

		// RET
		public ushort Ret() => Instr( InstrType.Ret );
		// RET NZ
		public ushort Ret( Condtype cond ) => Instr( InstrType.Ret, cond.Type );

		// RETI
		public ushort Reti() => Instr( InstrType.Reti );

		// POP [BC DE HL AF]
		public ushort Pop( BCtype rhs ) => Instr( InstrType.Pop, rhs.Type );
		public ushort Pop( DEtype rhs ) => Instr( InstrType.Pop, rhs.Type );
		public ushort Pop( HLtype rhs ) => Instr( InstrType.Pop, rhs.Type );
		public ushort Pop( AFtype rhs ) => Instr( InstrType.Pop, rhs.Type );

		// PUSH [BC DE HL AF]
		public ushort Push( BCtype rhs ) => Instr( InstrType.Push, rhs.Type );
		public ushort Push( DEtype rhs ) => Instr( InstrType.Push, rhs.Type );
		public ushort Push( HLtype rhs ) => Instr( InstrType.Push, rhs.Type );
		public ushort Push( AFtype rhs ) => Instr( InstrType.Push, rhs.Type );

		// CALL a16
		public ushort Call( ushort adr ) => Instr( InstrType.Call, Asm.A16( adr ) );
		// CALL C, a16
		public ushort Call( Condtype cond, ushort adr ) => Instr( InstrType.Call, cond.Type, Asm.A16( adr ) );

		public ushort Di() => Instr( InstrType.Di );
		public ushort Ei() => Instr( InstrType.Ei );
		public ushort Rlca() => Instr( InstrType.Rlca );
		public ushort Rla() => Instr( InstrType.Rla );
		public ushort Daa() => Instr( InstrType.Daa );
		public ushort Scf() => Instr( InstrType.Scf );
		public ushort Rrca() => Instr( InstrType.Rrca );
		public ushort Rra() => Instr( InstrType.Rra );
		public ushort Cpl() => Instr( InstrType.Cpl );
		public ushort Ccf() => Instr( InstrType.Ccf );
		public ushort Rst( byte vec ) => Instr( InstrType.Rst, Asm.RstAdr( vec ) );

		public ushort Db( byte val ) => Instr( InstrType.Db, Asm.D8( val ) );

		///
		/// EXTENSION INSTRUCTIONS
		/// 

		// RLC [B D H (HL)]
		public ushort Rlc( BDHhl rhs ) => Instr( InstrType.Rlc, rhs.Type );
		// RLC [C E L A]
		public ushort Rlc( CELA rhs ) => Instr( InstrType.Rlc, rhs.Type );

		// RRC [B D H (HL)]
		public ushort Rrc( BDHhl rhs ) => Instr( InstrType.Rrc, rhs.Type );
		// RRC [C E L A]
		public ushort Rrc( CELA rhs ) => Instr( InstrType.Rrc, rhs.Type );

		// RL [B D H (HL)]
		public ushort Rl( BDHhl rhs ) => Instr( InstrType.Rl, rhs.Type );
		// RL [C E L A]
		public ushort Rl( CELA rhs ) => Instr( InstrType.Rl, rhs.Type );

		// RR [B D H (HL)]
		public ushort Rr( BDHhl rhs ) => Instr( InstrType.Rr, rhs.Type );
		// RR [C E L A]
		public ushort Rr( CELA rhs ) => Instr( InstrType.Rr, rhs.Type );

		// SLA [B D H (HL)]
		public ushort Sla( BDHhl rhs ) => Instr( InstrType.Sla, rhs.Type );
		// SLA [C E L A]
		public ushort Sla( CELA rhs ) => Instr( InstrType.Sla, rhs.Type );

		// SRA [B D H (HL)]
		public ushort Sra( BDHhl rhs ) => Instr( InstrType.Sra, rhs.Type );
		// SRA [C E L A]
		public ushort Sra( CELA rhs ) => Instr( InstrType.Sra, rhs.Type );

		// SWAP [B D H (HL)]
		public ushort Swap( BDHhl rhs ) => Instr( InstrType.Swap, rhs.Type );
		// SWAP [C E L A]
		public ushort Swap( CELA rhs ) => Instr( InstrType.Swap, rhs.Type );

		// SRL [B D H (HL)]
		public ushort Srl( BDHhl rhs ) => Instr( InstrType.Srl, rhs.Type );
		// SRL [C E L A]
		public ushort Srl( CELA rhs ) => Instr( InstrType.Srl, rhs.Type );

		// BIT BitIdx, [B D H (HL)]
		public ushort Bit( byte idx, BDHhl rhs ) => Instr( InstrType.Bit, Asm.BitIdx( idx ), rhs.Type );
		// BIT BitIdx, [C E L A]
		public ushort Bit( byte idx, CELA rhs ) => Instr( InstrType.Bit, Asm.BitIdx( idx ), rhs.Type );

		// RES BitIdx, [B D H (HL)]
		public ushort Res( byte idx, BDHhl rhs ) => Instr( InstrType.Res, Asm.BitIdx( idx ), rhs.Type );
		// RES BitIdx, [C E L A]
		public ushort Res( byte idx, CELA rhs ) => Instr( InstrType.Res, Asm.BitIdx( idx ), rhs.Type );

		// SET BitIdx, [B D H (HL)]
		public ushort Set( byte idx, BDHhl rhs ) => Instr( InstrType.Set, Asm.BitIdx( idx ), rhs.Type );
		// SET BitIdx, [C E L A]
		public ushort Set( byte idx, CELA rhs ) => Instr( InstrType.Set, Asm.BitIdx( idx ), rhs.Type );
	}

	public static class AsmBuilderExtensions
	{
		// Helper for LD (a16), d8
		public static ushort Ld( this AsmBuilder self, ushort adr, byte val )
		{
			var label = self.Ld( AsmBuilder.A, val );
			self.Ld( adr.Adr(), AsmBuilder.A );
			return label;
		}

		public static ushort Db( this AsmBuilder self, byte first, params byte[] vals )
		{
			var label = self.Db( first );
			foreach( byte val in vals )
			{
				self.Db( val );
			}

			return label;
		}

		private static ushort Not( this AsmBuilder self, AsmOperand rhs )
		{
			ushort ret = self.Ld( AsmBuilder.A, 255 );
			self.Instr( InstrType.Sub, rhs );
			return ret;
		}

		// NOT A, [B D H (HL)]
		public static ushort Not( this AsmBuilder self, BDHhl rhs ) => Not( self, rhs.Type );
		// NOT A, [C E L A]
		public static ushort Not( this AsmBuilder self, CELA rhs ) => Not( self, rhs.Type );
		// NOT A, d8
		public static ushort Not( this AsmBuilder self, byte rhs ) => Not( self, Asm.D8( rhs ) );

		public static ushort Push( this AsmBuilder self, ushort rhs )
		{
			var label = self.Ld( AsmBuilder.BC, rhs );
			self.Push( AsmBuilder.BC );
			return label;
		}

		public static ushort SwitchBank(this AsmBuilder self, byte bank ) 
		{
			// https://retrocomputing.stackexchange.com/questions/11732/how-does-the-gameboys-memory-bank-switching-work
			ushort label;
			if( bank <= 0x1f )
			{
				label = self.Ld( 0x2000, bank );
			}
			else
			{
				//ld $6000, $00; Set ROM mode
				//ld $2000, $06; Set lower 5 bits, could also use $46
				//ld $4000, $02; Set upper 2 bits
				label =
				self.Ld( 0x6000, 0 );
				self.Ld( 0x2000, (byte)( bank & 0b11111 ) );
				self.Ld( 0x4000, (byte)( ( bank >> 5 ) & 0b11 ) );
			}
			return label;
		}
	}

	public class AsmRecorder : AsmBuilder, IEnumerable<AsmInstr>
	{
		protected List<AsmInstr> m_instructions = new();
		public IReadOnlyList<AsmInstr> Instructions => m_instructions;

		public AsmRecorder() { }
		public AsmRecorder( IEnumerable<AsmInstr> instructions ) { m_instructions = new( instructions ); }

		public IEnumerator<AsmInstr> GetEnumerator()
		{
			return Instructions.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable)Instructions ).GetEnumerator();
		}

		public override ushort Consume( AsmInstr instr )
		{
			var label = PC;
			m_instructions.Add( instr );
			IP += instr.ByteLength;
			return label;
		}
	}

	public class DelegateRecorder : AsmRecorder
	{
		public delegate ushort Consumer( AsmInstr instr );
		private Consumer m_consumer;
		public DelegateRecorder( Consumer consumer ) { m_consumer = consumer; }
		public override ushort Consume( AsmInstr instr )
		{
			// dont call base.Consume, we dont want to increment the IP/PC twice so m_consumer has to take care of that
			m_instructions.Add( instr );
			return m_consumer( instr );
		}
	}

	public class SectionBuilder : AsmBuilder, ISection
	{
		public byte this[ushort address] { get => Section[address]; set => Section[address] = value; }
		public ushort StartAddr => Section.StartAddr;
		public ushort Length => Section.Length;
		
		public ISection Section { get; set; }

		public SectionBuilder( ISection section) 
		{
			Section = section;
		}

		public override ushort Consume( AsmInstr instr )
		{
			ushort pc = PC;
			ushort label = pc;
			instr.Assemble( ref pc, Section, throwException: true );
			IP += (uint)( pc - label );
			return label;
		}
	}

	public class GrowingSectionBuilder : AsmBuilder, ISection
	{
		public byte this[ushort address] { get => Section[address]; set => Section[address] = value; }
		public ushort StartAddr => Section.StartAddr;
		public ushort Length => Section.Length;

		// executable section
		public List<byte> Data { get; }
		public Storage Section { get; }

		public GrowingSectionBuilder()
		{
			Data = new();
			Section = new( Data );
		}

		public override ushort Consume( AsmInstr instr )
		{
			ushort pc = PC;
			ushort label = pc;
			Data.EnsureCapacity( (int)IP + 3 );
			instr.Assemble( ref pc, Section, throwException: true );
			IP += (uint)( pc - label );
			Section.Length = pc;
			return label;
		}
	}
}
