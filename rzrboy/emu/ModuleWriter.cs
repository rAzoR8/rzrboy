using System.Collections;

namespace rzr
{
	public class AsmRecorder : IEnumerable<AsmInstr>
	{
		protected List<AsmInstr> m_instructions = new List<AsmInstr>();
		public IReadOnlyList<AsmInstr> Instructions => m_instructions;
		private void Add( InstrType instr, params AsmOperand[] operands ) => m_instructions.Add( new( instr, operands ) );

		public void Nop() => Add( InstrType.Nop );
		public void Stop( params AsmOperand[] ops ) => Add( InstrType.Stop, ops );
		public void Halt() => Add( InstrType.Halt );
		public void Ld( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Ld, lhs, rhs );
		public void Jr( params AsmOperand[] ops ) => Add( InstrType.Jr, ops );
		public void Jp( params AsmOperand[] ops ) => Add( InstrType.Jp, ops );
		public void Inc( AsmOperand lhs ) => Add( InstrType.Inc, lhs );
		public void Dec( AsmOperand lhs ) => Add( InstrType.Dec, lhs );
		public void Add( AsmOperand lhs, AsmOperand rhs ) => Add( InstrType.Add, lhs, rhs );
		public void Adc( AsmOperand rhs ) => Add( InstrType.Adc, rhs );
		public void Sub( AsmOperand rhs ) => Add( InstrType.Sub, rhs );
		public void Sbc( AsmOperand rhs ) => Add( InstrType.Sbc, rhs );
		public void And( AsmOperand rhs ) => Add( InstrType.And, rhs );
		public void Or( AsmOperand rhs ) => Add( InstrType.Or, rhs );
		public void Xor( AsmOperand rhs ) => Add( InstrType.Xor, rhs );
		public void Cp( AsmOperand rhs ) => Add( InstrType.Cp, rhs );
		public void Ret( params AsmOperand[] ops ) => Add( InstrType.Ret, ops );
		public void Reti() => Add( InstrType.Reti );
		public void Pop( AsmOperand lhs ) => Add( InstrType.Pop, lhs );
		public void Push( AsmOperand lhs ) => Add( InstrType.Push, lhs );
		public void Call( params AsmOperand[] ops ) => Add( InstrType.Call, ops );
		public void Di() => Add( InstrType.Di );
		public void Ei() => Add( InstrType.Ei );
		public void Rlca() => Add( InstrType.Rlca );
		public void Rla() => Add( InstrType.Rla );
		public void Daa() => Add( InstrType.Daa );
		public void Scf() => Add( InstrType.Scf );
		public void Rrca() => Add( InstrType.Rrca );
		public void Rra() => Add( InstrType.Rra );
		public void Cpl() => Add( InstrType.Cpl );
		public void Ccf() => Add( InstrType.Ccf );
		public void Rst( AsmOperand vec ) => Add( InstrType.Rst, vec );

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

	public interface IAsmBankSwitcher 
	{
		/// <summary>
		/// How many bytes before the end of *bank* should the ModuleWriter request a new bank
		/// </summary>
		/// <param name="bank"></param>
		/// <returns>Number of bytes</returns>
		ushort GetInstrByteThreshold( ushort bank );

		/// <summary>
		/// Get the next ROM section for the ModuleWriter to write to, together with the bank switching instructions
		/// </summary>
		/// <param name="pc">Program count in the current bank</param>
		/// <returns>Next free ROM section to write to. Instructions to write to the end for switching the bank</returns>
		(ISection next, IEnumerable<AsmInstr> instr) GetNextBank( ushort pc );
	}

	public class ModuleWriter : AsmRecorder
	{
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

		public ushort Write( IAsmBankSwitcher bankSwitcher, bool throwException = true )
		{
			ushort pc = 0; ushort bankIdx = 0;
			ushort threshold = bankSwitcher.GetInstrByteThreshold( bank: 0 );
			(ISection bank, IEnumerable<AsmInstr> switching) = bankSwitcher.GetNextBank( pc );

			void write( IEnumerable<AsmInstr> writer, ushort _bound = 0)
			{
				ushort bound = _bound != 0 ? _bound : (ushort)( pc + 8 );
				ushort end = writer.Write( pc, bank, throwException );
				if( end > bound )
				{
					pc = bound; // rest pc to acceptible bounds
					if( throwException )
						throw new rzr.AsmException( $"Invalid PC bound for Writer: {end:X4} expected {bound}" );
				}

				pc += 8;
			}

			write( Rst0);
			write( Rst8 );
			write( Rst10 );
			write( Rst18 );
			write( Rst20 );
			write( Rst28 );
			write( Rst30 );
			write( Rst38 );

			write( VBlank );
			write( LCDStat );
			write( Timer );
			write( Serial );
			write( Joypad, 0x100 ); //$60-$100

			pc = 0x100;

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

			pc = 0x150;

			foreach( AsmInstr instr in m_instructions )
			{
				if( pc + threshold >= Mbc.RomBankSize ) // switch to next bank
				{
					threshold = bankSwitcher.GetInstrByteThreshold( ++bankIdx );
					(ISection next, switching) = bankSwitcher.GetNextBank( pc );

					// write bank switching code to the end of this bank
					pc = switching.Write( pc, mem: bank, throwException: throwException );

					bank = next;
					pc = 0;
				}

				instr.Assemble( ref pc, bank, throwException: throwException );
			}

			return pc;
		}
	}

	public class Mbc1Switcher : IAsmBankSwitcher
	{
		private List<byte[]> m_banks = new( capacity: 1);
		public IReadOnlyList<byte[]> Banks => m_banks;

		public ushort GetInstrByteThreshold( ushort bank )
		{
			// LD 3 byte instr vs 3 LD instructions
			return (ushort)( bank > 0x1F ? 3 * 3 : 3 );
		}

		public (ISection next, IEnumerable<AsmInstr> instr) GetNextBank( ushort pc )
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

			return (new Storage( m_banks.Last() ), sw);
		}
	}
}
