using System.Collections;

namespace rzr
{
	public abstract class AsmConsumer
	{
		private ushort Instr( InstrType instr, params AsmOperand[] operands )
		{
			return Consume( new AsmInstr( instr, operands ) );
		}

		public abstract ushort Consume( AsmInstr instr );

		public interface IOpType { OperandType Type { get; } }

		// B D H (HL)
		public interface BDHhl : IOpType { }
		// C E L A
		public interface CELA : IOpType { }
		// BC DE HL SP
		public interface BcDeHlSp : IOpType { }
		// (BC) (DE) (HL+) (HL-)
		public interface AdrBcDeHliHld : IOpType { }

		public struct Atype : CELA { public OperandType Type => OperandType.A; }
		public struct Btype : BDHhl { public OperandType Type => OperandType.B; }
		public struct Ctype : CELA { public OperandType Type => OperandType.C; }
		public struct Dtype : BDHhl { public OperandType Type => OperandType.D; }
		public struct Etype : CELA { public OperandType Type => OperandType.E; }
		public struct Htype : BDHhl { public OperandType Type => OperandType.H; }
		public struct Ltype : CELA { public OperandType Type => OperandType.L; }

		// (HL)
		public struct AdrHLtype : BDHhl { public OperandType Type => OperandType.AdrHL; }

		public struct AdrBCtype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrBC; }
		public struct AdrDEtype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrDE; }
		public struct AdrHLitype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrHLi; }
		public struct AdrHLdtype : AdrBcDeHliHld { public OperandType Type => OperandType.AdrHLd; }

		// IoC 0xFF00+C
		public struct AdrCtype : IOpType { public OperandType Type => OperandType.ioC; }

		public struct BCtype : BcDeHlSp { public OperandType Type => OperandType.BC; public AdrBCtype Adr => adrBC;  }
		public struct DEtype : BcDeHlSp { public OperandType Type => OperandType.DE; public AdrDEtype Adr => adrDE; }
		public struct HLtype : BcDeHlSp { public OperandType Type => OperandType.HL; public AdrHLtype Adr => adrHL; }
		public struct SPtype : BcDeHlSp { public OperandType Type => OperandType.SP; }
		
		public struct AFtype : IOpType { public OperandType Type => OperandType.AF; }

		public interface Condtype : IOpType { }
		public struct CondZtype : Condtype { public OperandType Type => OperandType.condZ; }
		public struct CondNZtype : Condtype { public OperandType Type => OperandType.condNZ; }
		public struct CondCtype : Condtype { public OperandType Type => OperandType.condC; }
		public struct CondNCtype : Condtype { public OperandType Type => OperandType.condNC; }

		protected static readonly Atype A;
		protected static readonly Btype B;
		protected static readonly Ctype C;
		protected static readonly Dtype D;
		protected static readonly Etype E;
		protected static readonly Htype H;
		protected static readonly Ltype L;

		protected static readonly BCtype BC;
		protected static readonly DEtype DE;
		protected static readonly HLtype HL;
		protected static readonly SPtype SP;
		protected static readonly AFtype AF; // Push/Pop only

		protected static readonly AdrBCtype adrBC;
		protected static readonly AdrDEtype adrDE;
		protected static readonly AdrHLtype adrHL;
		protected static readonly AdrHLitype adrHLi;
		protected static readonly AdrHLdtype adrHLd;
		protected static readonly AdrCtype adrC; // IoC

		protected static readonly CondZtype isZ;
		protected static readonly CondNZtype isNZ;
		protected static readonly CondCtype isC;
		protected static readonly CondNCtype isNC;

		public ushort Nop() => Instr( InstrType.Nop );
		public ushort Stop( byte corrupt = 0x00 ) => Instr( InstrType.Stop, Asm.D8( corrupt ) );
		public ushort Halt() => Instr( InstrType.Halt );

		// LD [BC DE HL SP], d16
		public ushort Ld( BcDeHlSp lhs, ushort rhs ) => Instr( InstrType.Ld, lhs.Type, Asm.D16( rhs ) );
		// LD [(BC) (DE) (HL+) (HL-)], A
		public ushort Ld( AdrBcDeHliHld lhs, Atype A ) => Instr( InstrType.Ld, lhs.Type, A.Type );
		// LD [B D H (HL)], d8
		public ushort Ld( BDHhl lhs, byte d8 ) => Instr( InstrType.Ld, lhs.Type, Asm.D8(d8) );
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
		public ushort Ld( Address adr, Atype A ) => Instr( InstrType.Ld, Asm.A16(adr), A.Type );
		// LD A, (a16)
		public ushort Ld( Atype A, Address adr ) => Instr( InstrType.Ld, A.Type, Asm.A16( adr ) );

		// Helper for LD (a16), d8
		public ushort Ld( ushort adr, byte val ) 
		{
			var label = Ld( A, val );
			Ld( adr.Adr(), A );
			return label;
		}

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
		public ushort Add ( Atype A, BDHhl rhs ) => Instr ( InstrType.Add, A.Type, rhs.Type );
		// ADD A, [C E L A]
		public ushort Add( Atype A, CELA rhs ) => Instr( InstrType.Add, A.Type, rhs.Type );
		// ADD A, d8
		public ushort Add( Atype A, byte d8 ) => Instr( InstrType.Add, A.Type, Asm.D8(d8) );
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

		// OR A, [B D H (HL)]
		public ushort Or( BDHhl rhs ) => Instr( InstrType.Or, rhs.Type );
		// OR A, [C E L A]
		public ushort Or( CELA rhs ) => Instr( InstrType.Or, rhs.Type );

		// XOR A, [B D H (HL)]
		public ushort Xor( BDHhl rhs ) => Instr( InstrType.Xor, rhs.Type );
		// XOR A, [C E L A]
		public ushort Xor( CELA rhs ) => Instr( InstrType.Xor, rhs.Type );

		// CP A, [B D H (HL)]
		public ushort Cp( BDHhl rhs ) => Instr( InstrType.Cp, rhs.Type );
		// CP A, [C E L A]
		public ushort Cp( CELA rhs ) => Instr( InstrType.Cp, rhs.Type );

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

		public ushort Db( params byte[] vals ) 
		{
			var label = Instr( InstrType.Db, Asm.D8( vals[0] ) );
			foreach( byte val in vals.Skip(1) )
			{
				Instr( InstrType.Db, Asm.D8( val ) );
			}

			return label;
		}
	}

	public class AsmRecorder : AsmConsumer, IEnumerable<AsmInstr>
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
			m_instructions.Add( instr );
			return (ushort)m_instructions.Count;
		}
	}

	public static class AsmWriter 
	{
		public static ushort Write( this AsmRecorder rec, ushort pc, ISection mem, bool throwException = true )
		{
			foreach( AsmInstr instr in rec.Instructions )
			{
				instr.Assemble( ref pc, mem, throwException: throwException );
			}

			return pc;
		}

		public static ushort Write( this IEnumerable<AsmInstr> instructions, ushort pc, ISection mem, bool throwException = true )
		{
			foreach( AsmInstr instr in instructions )
			{
				instr.Assemble( ref pc, mem, throwException: throwException );
			}

			return pc;
		}
	}

	public abstract class ModuleWriter : AsmConsumer
	{
		public class FixedConsumer : AsmRecorder
		{
			public delegate ushort Consumer( AsmInstr instr );
			private Consumer m_consumer;
			public FixedConsumer( Consumer consumer ) { m_consumer = consumer; }
			public override ushort Consume( AsmInstr instr )
			{
				base.Consume( instr );
				return m_consumer( instr );
			}
		}

		public uint IP { get; protected set; }
		public ushort PC => (ushort)( IP < Mbc.RomBankSize ? IP : Mbc.RomBankSize + IP % Mbc.RomBankSize );
		public bool ThrowException { get; set; } = true;

		// Header access
		public bool Japan { get; set; }
		public byte Version { get; set; }
		public bool SGBSupport { get; set; }
		public abstract CartridgeType Type { get; } // to be set by the implementing class
		public IEnumerable<byte> Logo { get; set; } = new byte[]{
			0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D , 0x00 , 0x0B , 0x03 , 0x73 , 0x00 , 0x83 , 0x00 , 0x0C , 0x00 , 0x0D,
			0x00, 0x08, 0x11, 0x1F, 0x88, 0x89 , 0x00 , 0x0E , 0xDC , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00 , 0x00,
		};
		public string Title { get; set; } = "rzrboy";
		public string Manufacturer { get; set; } = "FABI";
		public int RamBanks { get; set; }
		public ushort NewLicenseeCode { get; set; } = 0;
		public byte OldLicenseeCode { get; set; } = 0x33;
		public byte CGBSupport { get; set; }

		protected abstract (Storage bank, IEnumerable<AsmInstr> switchting) GetBank( uint IP );

		public override ushort Consume( AsmInstr instr )
		{
			ushort prev = PC;

			var (bank, switching) = GetBank( IP: IP );

			// write bank switching code to the end of this bank
			ushort pc = switching.Write( prev, mem: bank, throwException: ThrowException );

			if( pc > prev )
			{
				(bank,_) = GetBank( IP: IP + (uint)(pc-prev)  );
			}

			instr.Assemble( ref pc, bank, throwException: ThrowException );

			IP += (uint)( pc - prev );

			return prev;
		}

		public ModuleWriter() 
		{
			Rst0 = new( Consume );
			Rst8 = new( Consume );
			Rst10 = new( Consume );
			Rst18 = new( Consume );
			Rst20 = new( Consume );
			Rst28 = new( Consume );
			Rst30 = new( Consume );
			Rst38 = new( Consume );

			VBlank = new( Consume );
			LCDStat = new( Consume );
			Timer = new( Consume );
			Serial = new( Consume );
			Joypad = new( Consume );
		}

		public FixedConsumer Rst0 { get; }
		public FixedConsumer Rst8 { get; }
		public FixedConsumer Rst10 { get; }
		public FixedConsumer Rst18 { get; }
		public FixedConsumer Rst20 { get; }
		public FixedConsumer Rst28 { get; }
		public FixedConsumer Rst30 { get; }
		public FixedConsumer Rst38 { get; }

		public FixedConsumer VBlank { get; }	// $40
		public FixedConsumer LCDStat { get; }	// $48
		public FixedConsumer Timer { get; }		// $50
		public FixedConsumer Serial { get; }	// $58
		public FixedConsumer Joypad { get; }	// $60

		protected void WritePreamble( ushort entryPoint = (ushort)HeaderOffsets.HeaderSize )
		{
			var (bank0,_) = GetBank( IP );
			void interrupt( IEnumerable<AsmInstr> writer, ushort _bound = 0)
			{
				ushort bound = _bound != 0 ? _bound : (ushort)( PC + 8 );
				ushort end = writer.Write( PC, bank0, ThrowException );
				if( end > bound && ThrowException )
				{
					throw new rzr.AsmException( $"Invalid PC bound for Writer: {end:X4} expected {bound}" );
				}
				IP = bound; // rest pc to acceptible bounds
			}

			interrupt( Rst0);
			interrupt( Rst8 );
			interrupt( Rst10 );
			interrupt( Rst18 );
			interrupt( Rst20 );
			interrupt( Rst28 );
			interrupt( Rst30 );
			interrupt( Rst38 );

			interrupt( VBlank );
			interrupt( LCDStat );
			interrupt( Timer );
			interrupt( Serial );
			interrupt( Joypad, 0x100 ); //$60-$100

			ushort EP = (ushort)HeaderOffsets.EntryPointStart;
			// jump to EntryPoint
			Asm.Jp( Asm.A16( entryPoint ) ).Assemble( ref EP, bank0, throwException: ThrowException );

			HeaderView header = new( bank0.Data );

			header.Manufacturer = Manufacturer;
			header.Version = Version;
			header.NewLicenseeCode = NewLicenseeCode;
			header.OldLicenseeCode = OldLicenseeCode;
			header.Japan = Japan;
			header.RamBanks = RamBanks;
			header.CGBSupport = CGBSupport;
			header.SGBSupport = SGBSupport;
			header.Logo = Logo;
			header.Title = Title;
			header.Version = Version;
			header.Type = Type;

			// skip header to game code:
			IP = entryPoint;
		}
	}

	public class MbcWriter : ModuleWriter
	{
		private List<Storage> m_banks = new();
		public IReadOnlyList<Storage> Banks => m_banks;
		public byte[] Rom() => m_banks.SelectMany( x => x.Data ).ToArray();

		public override CartridgeType Type => CartridgeType.MBC1_RAM;

		protected override (Storage bank, IEnumerable<AsmInstr> switchting) GetBank( uint IP )
		{
			int i = (int)( IP / Mbc.RomBankSize );
			// LD 3 byte instr vs 3 LD instructions

			var threshold = ( m_banks.Count > 0x1F ? 3 * 3 : 3 );
			bool switching = IP - threshold > ( m_banks.Count * Mbc.RomBankSize );

			if( i >= m_banks.Count || switching )
			{
				m_banks.Add( new Storage( new byte[Mbc.RomBankSize] ) );
			}

			AsmRecorder sw = new();

			// https://retrocomputing.stackexchange.com/questions/11732/how-does-the-gameboys-memory-bank-switching-work

			if( switching )
			{
				if( m_banks.Count <= 0x1f )
				{
					sw.Ld( 0x2000, (byte)m_banks.Count );
				}
				else
				{
					//ld $6000, $00; Set ROM mode
					//ld $2000, $06; Set lower 5 bits, could also use $46
					//ld $4000, $02; Set upper 2 bits
					sw.Ld( 0x6000, 0 );
					sw.Ld( 0x2000, (byte)( m_banks.Count & 0b11111 ) );
					sw.Ld( 0x4000, (byte)( ( m_banks.Count >> 5 ) & 0b11 ) );
				}
			}

			return (m_banks[i], sw);
		}

		/// <summary>
		/// Override this function with you game assembly producing code which will be placed after the preamble
		/// </summary>
		protected virtual void WriteGameCode() { }

		/// <summary>
		/// Make sure to set the header corretly before calling this function
		/// </summary>
		/// <param name="entryPoint">Location in the first bank to start writing the game code after the preamble, usually at 0x150</param>
		public void WriteAll( ushort entryPoint = (ushort)HeaderOffsets.HeaderSize )
		{
			// reset banks
			m_banks.Clear();
			IP = 0;

			WritePreamble( entryPoint: entryPoint );

			IP = entryPoint;
			WriteGameCode();

			var bank0 = m_banks.First();
			HeaderView header = new HeaderView( bank0.Data );

			header.RomBanks = m_banks.Count;
			header.HeaderChecksum = HeaderView.ComputeHeaderChecksum( bank0.Data );
			header.RomChecksum = HeaderView.ComputeRomChecksum( m_banks.SelectMany( x => x.Data ) );
#if DEBUG
			header.Valid();
#endif
		}
	}
}
