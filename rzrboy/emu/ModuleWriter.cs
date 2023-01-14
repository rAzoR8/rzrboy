﻿using System.Collections;

namespace rzr
{
	public class AsmRecorder : IEnumerable<AsmInstr>
	{
		protected List<AsmInstr> m_instructions = new ();
		public IReadOnlyList<AsmInstr> Instructions => m_instructions;

		public AsmRecorder() { }
		public AsmRecorder( IEnumerable<AsmInstr> instructions ) { m_instructions = new( instructions ); }

		protected virtual AsmInstr Add( AsmInstr instr )
		{
			m_instructions.Add( instr );
			return m_instructions.Last();
		}

		protected AsmInstr Add( InstrType instr, params AsmOperand[] operands )
		{
			return Add( new AsmInstr( instr, operands ) );
		}

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

		protected static readonly CondZtype condZ;
		protected static readonly CondNZtype condNZ;
		protected static readonly CondCtype condC;
		protected static readonly CondNCtype condNC;

		public AsmInstr Nop() => Add( InstrType.Nop );
		public AsmInstr Stop( byte corrupt = 0x00 ) => Add( InstrType.Stop, Asm.D8( corrupt ) );
		public AsmInstr Halt() => Add( InstrType.Halt );

		// LD [BC DE HL SP], d16
		public AsmInstr Ld( BcDeHlSp lhs, ushort rhs ) => Add( InstrType.Ld, lhs.Type, Asm.D16( rhs ) );
		// LD [(BC) (DE) (HL+) (HL-)], A
		public AsmInstr Ld( AdrBcDeHliHld lhs, Atype A ) => Add( InstrType.Ld, lhs.Type, A.Type );
		// LD [B D H (HL)], d8
		public AsmInstr Ld( BDHhl lhs, byte d8 ) => Add( InstrType.Ld, lhs.Type, Asm.D8(d8) );
		// LD [C E L A], d8
		public AsmInstr Ld( CELA lhs, byte d8 ) => Add( InstrType.Ld, lhs.Type, Asm.D8( d8 ) );
		// LD (a16), SP
		public AsmInstr Ld( Address adr, SPtype SP ) => Add( InstrType.Ld, Asm.A16( adr ), SP.Type );
		// LD A, [(BC) (DE) (HL+) (HL-)]
		public AsmInstr Ld( Atype A, AdrBcDeHliHld rhs ) => Add( InstrType.Ld, A.Type, rhs.Type );
		// LD [B D H (HL)], [B D H (HL)]
		public AsmInstr Ld( BDHhl lhs, BDHhl rhs ) => Add( InstrType.Ld, lhs.Type, rhs.Type );
		// LD [B D H (HL)], [C E L A]
		public AsmInstr Ld( BDHhl lhs, CELA rhs ) => Add( InstrType.Ld, lhs.Type, rhs.Type );
		// LD [C E L A], [B D H (HL)]
		public AsmInstr Ld( CELA lhs, BDHhl rhs ) => Add( InstrType.Ld, lhs.Type, rhs.Type );
		// LD [C E L A], [C E L A]
		public AsmInstr Ld( CELA lhs, CELA rhs ) => Add( InstrType.Ld, lhs.Type, rhs.Type );
		// LDH (a8), A
		public AsmInstr Ldh( byte ioAdr, Atype A ) => Add( InstrType.Ld, Asm.Io8( ioAdr ), A.Type );
		// LDH A, (a8)
		public AsmInstr Ldh( Atype A, byte ioAdr ) => Add( InstrType.Ld, A.Type, Asm.Io8( ioAdr ) );
		// LD (C), A (LDH)
		public AsmInstr Ld( AdrCtype adrC, Atype A ) => Add( InstrType.Ld, adrC.Type, A.Type );
		// LD A, (C) (LDH)
		public AsmInstr Ld( Atype A, AdrCtype adrC ) => Add( InstrType.Ld, A.Type, adrC.Type );
		// LD (a16), A
		public AsmInstr Ld( Address adr, Atype A ) => Add( InstrType.Ld, Asm.A16(adr), A.Type );
		// LD A, (a16)
		public AsmInstr Ld( Atype A, Address adr ) => Add( InstrType.Ld, A.Type, Asm.A16( adr ) );

		// Helper for LD (a16), d8
		public void Ld( ushort adr, byte val ) 
		{
			Ld( A, val );
			Ld( adr.Adr(), A );
		}

		// INC [BC DE HL SP]
		public AsmInstr Inc( BcDeHlSp lhs ) => Add( InstrType.Inc, lhs.Type );
		// INC [B D H (HL)]
		public AsmInstr Inc( BDHhl lhs ) => Add( InstrType.Inc, lhs.Type );
		// INC [C E L A]
		public AsmInstr Inc( CELA lhs ) => Add( InstrType.Inc, lhs.Type );

		// INC [BC DE HL SP]
		public AsmInstr Dec( BcDeHlSp lhs ) => Add( InstrType.Dec, lhs.Type );
		// INC [B D H (HL)]
		public AsmInstr Dec( BDHhl lhs ) => Add( InstrType.Dec, lhs.Type );
		// INC [C E L A]
		public AsmInstr Dec( CELA lhs ) => Add( InstrType.Dec, lhs.Type );

		// ADD A, [B D H (HL)]
		public AsmInstr Add ( Atype A, BDHhl rhs ) => Add ( InstrType.Add, A.Type, rhs.Type );
		// ADD A, [C E L A]
		public AsmInstr Add( Atype A, CELA rhs ) => Add( InstrType.Add, A.Type, rhs.Type );
		// ADD A, d8
		public AsmInstr Add( Atype A, byte d8 ) => Add( InstrType.Add, A.Type, Asm.D8(d8) );
		// ADD HL, [BC DE HL SP]
		public AsmInstr Add( HLtype HL, BcDeHlSp rhs ) => Add( InstrType.Add, HL.Type, rhs.Type );
		// ADD SP, r8
		public AsmInstr Add( SPtype SP, sbyte rhs ) => Add( InstrType.Add, SP.Type, Asm.R8( rhs ) );

		// ADC A, [B D H (HL)]
		public AsmInstr Adc( BDHhl rhs ) => Add( InstrType.Adc, rhs.Type );
		// ADC A, [C E L A]
		public AsmInstr Adc( CELA rhs ) => Add( InstrType.Adc, rhs.Type );
		// ADC A, d8
		public AsmInstr Adc( byte d8 ) => Add( InstrType.Adc, Asm.D8( d8 ) );

		// SUB A, [B D H (HL)]
		public AsmInstr Sub( BDHhl rhs ) => Add( InstrType.Sub, rhs.Type );
		// SUB A, [C E L A]
		public AsmInstr Sub( CELA rhs ) => Add( InstrType.Sub, rhs.Type );
		// SUB A, d8
		public AsmInstr Sub( byte d8 ) => Add( InstrType.Sub, Asm.D8( d8 ) );

		// SBC A, [B D H (HL)]
		public AsmInstr Sbc( BDHhl rhs ) => Add( InstrType.Sbc, rhs.Type );
		// SBC A, [C E L A]
		public AsmInstr Sbc( CELA rhs ) => Add( InstrType.Sbc, rhs.Type );
		// SBC A, d8
		public AsmInstr Sbc( byte d8 ) => Add( InstrType.Sbc, Asm.D8( d8 ) );

		// JR r8
		public AsmInstr Jr( sbyte r8 ) => Add( InstrType.Jr, Asm.R8( r8 ) );
		// JR Z, r8
		public AsmInstr Jr( Condtype cond, sbyte r8 ) => Add( InstrType.Jr, cond.Type, Asm.R8( r8 ) );

		// JP a16
		public AsmInstr Jp( ushort adr ) => Add( InstrType.Jp, Asm.A16( adr ) );
		// JP Z, a16
		public AsmInstr Jp( Condtype cond, ushort adr ) => Add( InstrType.Jp, cond.Type, Asm.A16( adr ) );
		// JP HL
		public AsmInstr Jp( HLtype HL ) => Add( InstrType.Jp, HL.Type );

		// AND A, [B D H (HL)]
		public AsmInstr And( BDHhl rhs ) => Add( InstrType.And, rhs.Type );
		// AND A, [C E L A]
		public AsmInstr And( CELA rhs ) => Add( InstrType.And, rhs.Type );

		// OR A, [B D H (HL)]
		public AsmInstr Or( BDHhl rhs ) => Add( InstrType.Or, rhs.Type );
		// OR A, [C E L A]
		public AsmInstr Or( CELA rhs ) => Add( InstrType.Or, rhs.Type );

		// XOR A, [B D H (HL)]
		public AsmInstr Xor( BDHhl rhs ) => Add( InstrType.Xor, rhs.Type );
		// XOR A, [C E L A]
		public AsmInstr Xor( CELA rhs ) => Add( InstrType.Xor, rhs.Type );

		// CP A, [B D H (HL)]
		public AsmInstr Cp( BDHhl rhs ) => Add( InstrType.Cp, rhs.Type );
		// CP A, [C E L A]
		public AsmInstr Cp( CELA rhs ) => Add( InstrType.Cp, rhs.Type );

		// RET
		public AsmInstr Ret() => Add( InstrType.Ret );
		// RET NZ
		public AsmInstr Ret( Condtype cond ) => Add( InstrType.Ret, cond.Type );
		
		// RETI
		public AsmInstr Reti() => Add( InstrType.Reti );

		// POP [BC DE HL AF]
		public AsmInstr Pop( BCtype rhs ) => Add( InstrType.Pop, rhs.Type );
		public AsmInstr Pop( DEtype rhs ) => Add( InstrType.Pop, rhs.Type );
		public AsmInstr Pop( HLtype rhs ) => Add( InstrType.Pop, rhs.Type );
		public AsmInstr Pop( AFtype rhs ) => Add( InstrType.Pop, rhs.Type );

		// PUSH [BC DE HL AF]
		public AsmInstr Push( BCtype rhs ) => Add( InstrType.Push, rhs.Type );
		public AsmInstr Push( DEtype rhs ) => Add( InstrType.Push, rhs.Type );
		public AsmInstr Push( HLtype rhs ) => Add( InstrType.Push, rhs.Type );
		public AsmInstr Push( AFtype rhs ) => Add( InstrType.Push, rhs.Type );

		// CALL a16
		public AsmInstr Call( ushort adr ) => Add( InstrType.Call, Asm.A16( adr ) );
		// CALL C, a16
		public AsmInstr Call( Condtype cond, ushort adr ) => Add( InstrType.Call, cond.Type, Asm.A16( adr ) );

		public AsmInstr Di() => Add( InstrType.Di );
		public AsmInstr Ei() => Add( InstrType.Ei );
		public AsmInstr Rlca() => Add( InstrType.Rlca );
		public AsmInstr Rla() => Add( InstrType.Rla );
		public AsmInstr Daa() => Add( InstrType.Daa );
		public AsmInstr Scf() => Add( InstrType.Scf );
		public AsmInstr Rrca() => Add( InstrType.Rrca );
		public AsmInstr Rra() => Add( InstrType.Rra );
		public AsmInstr Cpl() => Add( InstrType.Cpl );
		public AsmInstr Ccf() => Add( InstrType.Ccf );
		public AsmInstr Rst( byte vec ) => Add( InstrType.Rst, Asm.RstAdr( vec ) );

		public AsmInstr Db( params byte[] vals ) 
		{
			foreach( byte val in vals )
			{
				Add( InstrType.Db, Asm.D8( val ) );
			}

			return m_instructions.Last();
		}

		public IEnumerator<AsmInstr> GetEnumerator()
		{
			return Instructions.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ( (IEnumerable)Instructions ).GetEnumerator();
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

	public abstract class ModuleWriter : AsmRecorder
	{
		private Storage? m_curBank = null; // current bank

		public ushort PC { get; protected set; } = 0;
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

		public virtual ushort InstrByteThreshold { get; } = 3;
		protected abstract (Storage bank, IEnumerable<AsmInstr> switchting) GetNextBank( out ushort pcAfterSwitching );

		protected override AsmInstr Add( AsmInstr instr )
		{
			ushort pc = PC;

			if( m_curBank == null )
			{
				m_curBank = GetNextBank( out _ ).bank; // ignore switching for first bank
			}
			else if( pc + InstrByteThreshold >= Mbc.RomBankSize ) // switch to next bank
			{
				var (next, switching) = GetNextBank( out ushort pcAfterSwitching );
				// write bank switching code to the end of this bank
				pc = switching.Write( pc, mem: m_curBank, throwException: ThrowException );
				// get new bank and possibly adjust PC
				m_curBank = next;
				pc = pcAfterSwitching;
			}

			AsmInstr newInstr = base.Add( instr );
			newInstr.Assemble( ref pc, m_curBank, throwException: ThrowException );
			PC = pc;

			return newInstr;
		}

		public AsmRecorder Rst0 { get; } = new();
		public AsmRecorder Rst8 { get; } = new();
		public AsmRecorder Rst10 { get; } = new();
		public AsmRecorder Rst18 { get; } = new();
		public AsmRecorder Rst20 { get; } = new();
		public AsmRecorder Rst28 { get; } = new();
		public AsmRecorder Rst30 { get; } = new();
		public AsmRecorder Rst38 { get; } = new();

		public AsmRecorder VBlank { get; } = new(); // $40
		public AsmRecorder LCDStat { get; } = new();// $48
		public AsmRecorder Timer { get; } = new();	// $50
		public AsmRecorder Serial { get; } = new();	// $58
		public AsmRecorder Joypad { get; } = new(); // $60

		protected void WritePreamble( ushort entryPoint = (ushort)HeaderOffsets.HeaderSize )
		{
			if( m_curBank == null )
			{
				m_curBank = GetNextBank( out _ ).bank; // ignore switching for first bank
			}

			void interrupt( IEnumerable<AsmInstr> writer, ushort _bound = 0)
			{
				ushort bound = _bound != 0 ? _bound : (ushort)( PC + 8 );
				ushort end = writer.Write( PC, m_curBank, ThrowException );
				if( end > bound && ThrowException )
				{
					throw new rzr.AsmException( $"Invalid PC bound for Writer: {end:X4} expected {bound}" );
				}
				PC = bound; // rest pc to acceptible bounds
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
			Asm.Jp( Asm.A16( entryPoint ) ).Assemble( ref EP, m_curBank, throwException: ThrowException );

			HeaderView header = new( m_curBank.Data );

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
			PC = entryPoint;
		}
	}

	public class MbcWriter : ModuleWriter
	{
		private List<Storage> m_banks = new();
		public IReadOnlyList<Storage> Banks => m_banks;
		public byte[] Rom() => m_banks.SelectMany( x => x.Data ).ToArray();

		// LD 3 byte instr vs 3 LD instructions
		public override ushort InstrByteThreshold => (ushort)( m_banks.Count > 0x1F ? 3 * 3 : 3 );

		public override CartridgeType Type => CartridgeType.MBC1_RAM;

		protected override (Storage bank, IEnumerable<AsmInstr> switchting) GetNextBank( out ushort pcAfterSwitching )
		{
			m_banks.Add( new Storage( new byte[Mbc.RomBankSize] ) );

			// offset
			pcAfterSwitching = 0;

			AsmRecorder sw = new();

			// https://retrocomputing.stackexchange.com/questions/11732/how-does-the-gameboys-memory-bank-switching-work

			// first two banks are always mapped, so no need to switch between bank0 and bank1
			if( m_banks.Count == 1)
			{
				return ( m_banks.Last(), sw);
			}

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

			return (m_banks.Last(), sw);
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
			PC = 0;

			WritePreamble( entryPoint: entryPoint );

			PC = entryPoint;
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
