namespace rzr
{
	public class ModuleWriter : AsmBuilder
	{
		public bool ThrowException { get; set; } = true;

		// Header access
		public bool Japan { get; set; }
		public byte Version { get; set; }
		public bool SGBSupport { get; set; }
		public CartridgeType Type { get; set; } = CartridgeType.ROM_ONLY; // to be set by the implementing class
		public IEnumerable<byte> Logo { get; set; } = new byte[]{
			0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B, 0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D,
			0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E, 0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99,
			0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC, 0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E
		};
		public string Title { get; set; } = "rzrboy";
		public string Manufacturer { get; set; } = "FABI";
		public int RamBanks { get; set; }
		public ushort NewLicenseeCode { get; set; } = 0;
		public byte OldLicenseeCode { get; set; } = 0x33;
		public byte CGBSupport { get; set; }

		// Banks/Storage
		protected List<Storage> m_banks = new();
		public IReadOnlyList<Storage> Banks => m_banks;
		public byte BankIdx { get; private set; } = 0;
		public Storage CurBank => m_banks[BankIdx];
		public byte[] Rom() => m_banks.SelectMany( x => x.Data ).ToArray();

		//protected abstract (Storage bank, IEnumerable<AsmInstr> switchting) GetBank( uint IP );

		public ModuleWriter( uint initialBanks = 2 )
		{
			// alloc at least two banks 
			for( uint i = 0; i < initialBanks; i++ )
			{
				m_banks.Add( new Storage( new byte[Mbc.RomBankSize] ) );
			}

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

		public DelegateRecorder Rst0 { get; }
		public DelegateRecorder Rst8 { get; }
		public DelegateRecorder Rst10 { get; }
		public DelegateRecorder Rst18 { get; }
		public DelegateRecorder Rst20 { get; }
		public DelegateRecorder Rst28 { get; }
		public DelegateRecorder Rst30 { get; }
		public DelegateRecorder Rst38 { get; }

		public DelegateRecorder VBlank { get; }	// $40
		public DelegateRecorder LCDStat { get; }	// $48
		public DelegateRecorder Timer { get; }		// $50
		public DelegateRecorder Serial { get; }	// $58
		public DelegateRecorder Joypad { get; }    // $60

		protected void AddBank()
		{
			m_banks.Add( new Storage( new byte[Mbc.RomBankSize] ) );
		}

		public override ushort Consume( AsmInstr instr )
		{
			ushort prev = PC;

			// LD 3 byte instr vs 3 LD instructions
			var threshold = ( BankIdx > 0x1F ? 3 * 3 : 3 );
			bool switching = IP - threshold > ( m_banks.Count * Mbc.RomBankSize );

			ushort pc = prev;

			if( switching )
			{
				AsmRecorder sw = new();
				sw.SwitchBank( (byte)(BankIdx+1) ); // record on separate stream to not invoke this function (Consume)
				// write bank switching code to the end of this bank
				pc = sw.Assemble( pc: prev, mem: CurBank, throwException: ThrowException );

				if( BankIdx + 1 > m_banks.Count )
				{
					AddBank();
				}

				BankIdx++; // select current bank
			}

			instr.Assemble( ref pc, CurBank, throwException: ThrowException );

			IP += (uint)( pc - prev );

			return prev;
		}

		// direct write through to current rom bank, ignores any switching
		public void Write( byte[] data, uint ip )
		{
			for( uint i = 0; i < data.Length; ++i, ++ip )
			{
				uint bankIdx = ip / Mbc.RomBankSize;
				uint pc = ip % Mbc.RomBankSize;

				if( bankIdx >= m_banks.Count ) 
				{
					AddBank();
				}

				Storage bank = m_banks[(int)bankIdx];
				bank[(ushort)pc] = data[i];
			}
		}

		public ushort Write( params byte[] data )
		{
			ushort prev = PC;

			Write( data, IP );

			IP += (uint)data.Length;

			return prev;
		}

		protected void WritePreamble( ushort entryPoint = (ushort)HeaderOffsets.HeaderSize )
		{
			void interrupt( IEnumerable<AsmInstr> writer, ushort _bound = 0)
			{
				ushort bound = _bound != 0 ? _bound : (ushort)( PC + 8 );
				ushort end = writer.Assemble( PC, CurBank, ThrowException );
				if( end > bound && ThrowException )
				{
					throw new rzr.AsmException( $"Invalid PC bound for Writer: {end:X4} expected {bound}" );
				}
				IP = bound; // rest IP to acceptible bounds
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
			Asm.Jp( Asm.A16( entryPoint ) ).Assemble( ref EP, CurBank, throwException: ThrowException );

			HeaderView header = new( CurBank.Data );

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
		public byte HeaderChecksum { get; private set; } = 0;
		public ushort RomChecksum { get; private set; } = 0;

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

			// add two default banks
			AddBank();
			AddBank();

			IP = 0;

			WritePreamble( entryPoint: entryPoint );

			IP = entryPoint;
			WriteGameCode();

			var bank0 = m_banks.First();
			HeaderView header = new HeaderView( bank0.Data );

			header.RomBanks = m_banks.Count;
			HeaderChecksum = header.HeaderChecksum = HeaderView.ComputeHeaderChecksum( bank0.Data );
			RomChecksum = header.RomChecksum = HeaderView.ComputeRomChecksum( m_banks.SelectMany( x => x.Data ) );
#if DEBUG
			header.Valid();
#endif
		}
	}
}
