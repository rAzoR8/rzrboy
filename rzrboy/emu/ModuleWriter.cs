using System.Collections;

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

		public enum Adr : byte
		{
			BC = OperandType.AdrBC, // (BC)
			DE = OperandType.AdrDE, // (DE)
			HL = OperandType.AdrHL, // (HL)
			HLi = OperandType.AdrHLi, // (HL+)
			HLd = OperandType.AdrHLd, // (HL-)
		}

		public enum Cond : byte
		{
			Z = OperandType.condZ,
			NZ = OperandType.condNZ,
			C = OperandType.condC,
			NC = OperandType.condNC,
		}

		public interface IOpType { OperandType Type { get; } }

		public interface BDHhl : IOpType { } // LD lhs
		public interface CELA : IOpType { }// LD lhs
		public interface BCDEHLhlA : BDHhl, CELA { }

		public struct Atype : BCDEHLhlA { public OperandType Type => OperandType.A; }
		public struct Btype : BCDEHLhlA { public OperandType Type => OperandType.B; }
		public struct Ctype : BCDEHLhlA { public OperandType Type => OperandType.C; }
		public struct Dtype : BCDEHLhlA { public OperandType Type => OperandType.D; }
		public struct Etype : BCDEHLhlA { public OperandType Type => OperandType.E; }
		public struct Htype : BCDEHLhlA { public OperandType Type => OperandType.H; }
		public struct Ltype : BCDEHLhlA { public OperandType Type => OperandType.L; }
		public struct AdrHLtype : BCDEHLhlA { public OperandType Type => OperandType.AdrHL; }

		protected static Atype A;
		protected static Btype B;
		protected static Ctype C;
		protected static Dtype D;
		protected static Etype E;
		protected static Htype H;
		protected static Ltype L;

		protected const Reg16 BC = Reg16.BC;
		protected const Reg16 DE = Reg16.DE;
		protected const Reg16 HL = Reg16.HL;
		protected const Reg16 SP = Reg16.SP;
		protected const Reg16 AF = Reg16.AF; // Push/Pop only

		protected const Adr adrBC = Adr.BC;
		protected const Adr adrDe = Adr.DE;
		protected const Adr adrHL = Adr.HL;
		protected const Adr adrHLi = Adr.HLi;
		protected const Adr adrHLd = Adr.HLd;

		protected const Cond condZ = Cond.Z;
		protected const Cond condNZ = Cond.NZ;
		protected const Cond condC = Cond.C;
		protected const Cond condNC = Cond.NC;

		public AsmInstr Nop() => Add( InstrType.Nop );
		public AsmInstr Stop( byte corrupt = 0x00 ) => Add( InstrType.Stop, Asm.D8( corrupt ) );
		public AsmInstr Halt() => Add( InstrType.Halt );
		public AsmInstr Ld( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Ld, lhs, rhs );
		// LD [BC DE HL SP], d16
		public AsmInstr Ld( Reg16 lhs, ushort rhs ) => Add( InstrType.Ld, lhs.ToOp(), new AsmOperand(rhs) );
		// LD [(BC) (DE) (HL+) (HL-)], A
		public AsmInstr Ld( Adr lhs, Reg8 A ) => Add( InstrType.Ld, (OperandType)lhs, OperandType.A );
		// LD A, [(BC) (DE) (HL+) (HL-)]
		public AsmInstr Ld( Reg8 A, Adr rhs ) => Add( InstrType.Ld, OperandType.A, (OperandType)rhs );

		// LD [B D H L (HL)], [B C D E H L (HL) A]
		public AsmInstr Ld( BDHhl lhs, BCDEHLhlA rhs ) => Add( InstrType.Ld, lhs.Type, rhs.Type );

		// ADD A, [B C D E H L (HL) A]
		public AsmInstr Add ( Atype a, BCDEHLhlA rhs ) => Add ( InstrType.Add, a.Type, rhs.Type );

		// ADD A, d8
		public AsmInstr Add( Atype a, byte d8 ) => Add( InstrType.Add, a.Type, Asm.D8(d8) );

		// TODO:
		// ADD HL, BC DE HL SP
		// ADD SP, r8

		public AsmInstr Jr( params AsmOperand[] ops ) => Add( InstrType.Jr, ops );
		public AsmInstr Jp( params AsmOperand[] ops ) => Add( InstrType.Jp, ops );
		public AsmInstr Inc( AsmOperand lhs ) => Add( InstrType.Inc, lhs );
		public AsmInstr Dec( AsmOperand lhs ) => Add( InstrType.Dec, lhs );
		public AsmInstr Add( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Add, lhs, rhs );
		public AsmInstr Adc( AsmOperand rhs ) => Add( InstrType.Adc, rhs );
		public AsmInstr Sub( AsmOperand rhs ) => Add( InstrType.Sub, rhs );
		public AsmInstr Sbc( AsmOperand rhs ) => Add( InstrType.Sbc, rhs );
		public AsmInstr And( AsmOperand rhs ) => Add( InstrType.And, rhs );
		public AsmInstr Or( AsmOperand rhs ) => Add( InstrType.Or, rhs );
		public AsmInstr Xor( AsmOperand rhs ) => Add( InstrType.Xor, rhs );
		public AsmInstr Cp( AsmOperand rhs ) => Add( InstrType.Cp, rhs );
		public AsmInstr Ret( params AsmOperand[] ops ) => Add( InstrType.Ret, ops );
		public AsmInstr Reti() => Add( InstrType.Reti );
		public AsmInstr Pop( AsmOperand lhs ) => Add( InstrType.Pop, lhs );
		public AsmInstr Push( AsmOperand lhs ) => Add( InstrType.Push, lhs );
		public AsmInstr Call( params AsmOperand[] ops ) => Add( InstrType.Call, ops );
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
		public AsmInstr Rst( AsmOperand vec ) => Add( InstrType.Rst, vec );

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
			Asm.Jp( Asm.D16( entryPoint ) ).Assemble( ref EP, m_curBank, throwException: ThrowException );

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
				sw.Ld( Asm.D16( 0x2000 ), Asm.D8( (byte)m_banks.Count ) );
			}
			else
			{
				//ld $6000, $00; Set ROM mode
				//ld $2000, $06; Set lower 5 bits, could also use $46
				//ld $4000, $02; Set upper 2 bits
				sw.Ld( Asm.D16( 0x6000 ), Asm.D8( 0 ) );
				sw.Ld( Asm.D16( 0x2000 ), Asm.D8( (byte)( m_banks.Count & 0b11111 ) ) );
				sw.Ld( Asm.D16( 0x4000 ), Asm.D8( (byte)( ( m_banks.Count >> 5 ) & 0b11 ) ) );
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
