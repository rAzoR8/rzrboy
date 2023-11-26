using System.Diagnostics;

namespace rzr
{
	// TODO: implement SectionAccess
	public class BankedMemory : IBankedMemory, ISection
	{
		private List<byte[]> m_banks = new();
		private int m_selectedBank = 1;

		// IBankedMemory
		public ushort BankSize { get; }
		public int Banks => (ushort)m_banks.Count;
		public int SelectedBank { get => m_selectedBank; set { m_selectedBank = value > 0 || DirectMappedBank ? value : 1; } }
		public IList<byte> GetBank( int bank ) => m_banks[bank];

		// Helper
		public ushort SelectableBankStart => (ushort)( StartAddr + BankSize );
		public bool DirectMappedBank { get; } = false;

		// ISection
		public ushort StartAddr { get; }
		public ushort Length => (ushort)( BankSize * 2 );
		public byte this[ushort address]
		{
			get
			{
				if( DirectMappedBank )
					return m_banks[m_selectedBank][address - StartAddr];
				else if( address < SelectableBankStart )
					return m_banks[0][address - StartAddr];
				else
					return m_banks[m_selectedBank][address - SelectableBankStart];
			}
			set
			{
				if( DirectMappedBank )
					m_banks[m_selectedBank][address - StartAddr] = value;
				else if( address < SelectableBankStart )
					m_banks[0][address - StartAddr] = value;
				else
					m_banks[m_selectedBank][address - SelectableBankStart] = value;
			}
		}

		public BankedMemory( ushort start, ushort bankSize, ushort banks, bool directMapped )
		{
			StartAddr = start;
			BankSize = bankSize;
			DirectMappedBank = directMapped;
			for( int i = 0; i < banks; i++ )
			{
				m_banks.Add( new byte[BankSize] );
			}
		}

		public void Resize( ushort bankCount )
		{
			if( bankCount > m_banks.Count )
			{
				for( int i = m_banks.Count; i < bankCount; ++i )
				{
					m_banks.Add( new byte[BankSize] );
				}
			}
			else if( bankCount < m_banks.Count )
			{
				m_banks.RemoveRange( bankCount - 1, m_banks.Count - bankCount );
			}
		}

		public void Load( byte[] data )
		{
			Debug.Assert( data.Length % BankSize == 0 );
			m_banks.Clear();
			m_banks.AddRange( data.Split( BankSize ) );
		}

		public byte[] Save() => m_banks.SelectMany( x => x ).ToArray();
	}
}
