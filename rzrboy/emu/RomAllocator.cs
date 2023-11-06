namespace rzr
{
	public class RomAllocation
	{
		public ushort Start { get; set; }
		public ushort Size { get; set; }
		public int Bank {get;}

		public rzr.Address Adr => new(Start);

		public RomAllocation(ushort start, ushort size, int bank)
		{
			Start = start;
			Size = size;
			Bank = bank;
		}
	}

	public class RomAllocator // one bank
	{
		public ushort Start { get; } // start address of the bank
		public ushort End {get;}
		public uint StartIP {get;}
		public int Bank {get;}

		private List<RomAllocation> m_freeSize = new();

		public RomAllocator(int bank)
		{
			StartIP = (uint)(bank * Mbc.RomBankSize);
			Start = AsmBuilder.IPtoPC(StartIP);
			End = (ushort)(Start + Mbc.RomBankSize);
			Bank = bank;

			var whole = new RomAllocation(start: Start, size: Mbc.RomBankSize, bank: bank);
			m_freeSize.Add(whole);
		}

		private int SearchSize( ushort size )
		{
			int min = 0;
			int max = m_freeSize.Count - 1;
			while (min <= max)
			{
				int mid = (min + max) / 2;
				ushort cur = m_freeSize[mid].Size;
				if (size == cur)
				{
					return mid;
				}
				else if (size < cur)
				{
					max = mid - 1;
				}
				else
				{
					min = mid + 1;
				}
			}
			return min;
		}

		public RomAllocation Alloc(ushort size)
		{
			if (m_freeSize.Count > 0)
			{
				int sidx = SearchSize(size);
				RomAllocation v = m_freeSize[sidx];
				if (v.Size >= size)
				{
					m_freeSize.RemoveAt(sidx);
					if (v.Size > size) // split
					{
						ushort diff = (ushort)(v.Size - size);
						v.Size = size;
						RomAllocation remainder = new RomAllocation(start: (ushort)(v.Start + size), size: diff, bank: Bank);

						sidx = SearchSize(remainder.Size);
						m_freeSize.Insert(sidx, remainder);
					}
					return v;
				}
			}
			
			throw new System.OutOfMemoryException($"Allocator {this} is out of memory and can't allocate {size} bytes in 0x{Start:X}-0x{End:X}");
		}
	}

	public class BankedRomAllocator
	{
		public ushort Start { get; } = 0x0000;
		public ushort BankStart {get;} = Mbc.RomBankSize;
		public ushort End { get; } = Mbc.RomBankSize*2;

		private RomAllocator[] m_banks;
		public BankedRomAllocator(int numBanks)
		{
			m_banks = new RomAllocator[numBanks];
			for (int i = 0; i < numBanks; i++)
			{
				m_banks[i] = new(bank: i);
			}
		}

		public RomAllocation Alloc(ushort size, int bank)
		{
			return m_banks[bank].Alloc(size);
		}

		public RomAllocation Alloc(ushort size, out int bank)
		{
			for (int i = 0; i < m_banks.Length; i++)
			{
				try
				{
					RomAllocation v = m_banks[i].Alloc(size);
					if (v != null)
					{
						bank = i;
						return v;
					}
				}
				catch (System.OutOfMemoryException){} // ignore bank exception
			}

			bank = -1;
			throw new System.OutOfMemoryException($"Allocator {this} is out of memory and can't allocate {size} bytes in 0x{Start:X}-0x{End:X}");
		}

		public RomAllocation Alloc(ushort size) => Alloc(size: size, out int _);
	}
}