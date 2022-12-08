using System.Collections;

namespace rzr
{
	public class AsmRecorder : IEnumerable<AsmInstr>
	{
		protected List<AsmInstr> m_instructions = new List<AsmInstr>();
		public IReadOnlyList<AsmInstr> Instructions => m_instructions;

		protected virtual AsmInstr Add( AsmInstr instr )
		{
			m_instructions.Add( instr );
			return m_instructions.Last();
		}

		protected AsmInstr Add( InstrType instr, params AsmOperand[] operands )
		{
			return Add( new AsmInstr( instr, operands ) );
		}

		public AsmInstr Nop() => Add( InstrType.Nop );
		public AsmInstr Stop( params AsmOperand[] ops ) => Add( InstrType.Stop, ops );
		public AsmInstr Halt() => Add( InstrType.Halt );
		public AsmInstr Ld( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Ld, lhs, rhs );
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
		public ushort PC { get; private set; } = 0;
		public ISection Bank { get; private set; } // current bank
		public bool ThrowException { get; set; } = false;

		public ModuleWriter()
		{
			Bank = GetNextBank( out ushort pcAfterSwitching ).bank; // ignore switching for first bank
			PC = pcAfterSwitching;
		}

		public virtual ushort InstrByteThreshold { get; } = 3;
		protected abstract (ISection bank, IEnumerable<AsmInstr> switchting) GetNextBank( out ushort pcAfterSwitching );

		protected override AsmInstr Add( AsmInstr instr )
		{
			ushort pc = PC;
			if( pc + InstrByteThreshold >= Mbc.RomBankSize ) // switch to next bank
			{
				var (next, switching) = GetNextBank( out var pcAfterSwitching );
				// write bank switching code to the end of this bank
				pc = switching.Write( pc, mem: Bank, throwException: ThrowException );
				// get new bank and possibly adjust PC
				Bank = next;
				pc = pcAfterSwitching;
			}

			AsmInstr newInstr = base.Add( instr );
			newInstr.Assemble( ref pc, Bank, throwException: ThrowException );
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

		public void WritePreamble()
		{
			void interrupt( IEnumerable<AsmInstr> writer, ushort _bound = 0)
			{
				ushort bound = _bound != 0 ? _bound : (ushort)( PC + 8 );
				ushort end = writer.Write( PC, Bank, ThrowException );
				if( end > bound  && ThrowException )
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

			// TODO:
			//EntryPoint = 0x100,
			//LogoStart = 0x104,
			//LogoEnd = 0x133, // inclusive
			//TitleStart = 0x134,
			//TitleEnd = 0x143, // inclusive           
			//ManufacturerStart = 0x13F,
			//ManufacturerEnd = 0x142, // inclusuvie
			//CGBFlag = 0x143,
			//NewLicenseeCodeStart = 0x144,
			//NewLicenseeCodeEnd = 0x145,
			//SGBFlag = 0x146,
			//Type = 0x147,
			//RomBanks = 0x148,
			//RamBanks = 0x149,
			//DestinationCode = 0x14A, // 0 = japanese
			//OldLicenseeCode = 0x14B,
			//Version = 0x14C, // game version
			//HeaderChecksum = 0x14D, // 0x134-14C
			//RomChecksumStart = 0x14E,
			//RomChecksumEnd = 0x14F,

			PC = 0x150;
		}
	}

	public class Mbc1Writer : ModuleWriter
	{
		private List<byte[]> m_banks = new( capacity: 1);
		public IReadOnlyList<byte[]> Banks => m_banks;

		// LD 3 byte instr vs 3 LD instructions
		public override ushort InstrByteThreshold => (ushort)( m_banks.Count > 0x1F ? 3 * 3 : 3 );

		protected override (ISection bank, IEnumerable<AsmInstr> switchting) GetNextBank( out ushort pcAfterSwitching )
		{
			m_banks.Add( new byte[Mbc.RomBankSize] );

			AsmRecorder sw = new();

			// https://retrocomputing.stackexchange.com/questions/11732/how-does-the-gameboys-memory-bank-switching-work

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

			// offset
			pcAfterSwitching = 0;

			return (new Storage( m_banks.Last() ), sw);
		}
	}
}
