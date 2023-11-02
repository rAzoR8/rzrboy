namespace rzr
{
	public class RamVariable : IDisposable
	{
		public ushort Start { get; }
		public ushort Size { get; set; }
		public int Bank => Owner.Bank;

		public rzr.Address Adr => new(Start);

		public RamAllocator Owner {get;}

		public RamVariable(ushort start, ushort size, RamAllocator owner)
		{
			Start = start;
			Size = size;
			Owner = owner;
		}

		public void Dispose()
		{
			Owner.Free(this);
		}
	}

	public class RamVariableComparer : IComparer<RamVariable>
	{
		public int Compare(RamVariable? x, RamVariable? y)
		{
			if (x == null && y == null) return 0;
			if (x != null && y == null) return 1;
			if (x == null && y != null) return -1;
			return (x.Size).CompareTo(y.Size);
		}
	}

	public class RamAllocator
	{
		public ushort Start { get; }
		public ushort End { get; }
		public ushort Cur { get; private set; }
		public int Bank {get;}

		private List<RamVariable> m_free = new();

		public RamAllocator(ushort start, ushort end, int bank = 0) { Start = Cur = start; End = end; Bank = bank; }

		private int Search(ushort size)
		{
			int min = 0;
			int max = m_free.Count - 1;
			while (min <= max)
			{
				int mid = (min + max) / 2;
				if (size == m_free[mid].Size)
				{
					return mid;
				}
				else if (size < m_free[mid].Size)
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

		public RamVariable Alloc(ushort size)
		{
			if (Cur + size < End)
			{
				var variable = new RamVariable(Cur, size, this);
				Cur += size;
				return variable;
			}

			if (m_free.Count > 0)
			{
				int idx = Search(size);
				RamVariable v = m_free[idx];
				if (v.Size >= size)
				{
					m_free.RemoveAt(idx);
					if (v.Size > size) // split
					{
						ushort diff = (ushort)(v.Size - size);
						v.Size = size;
						RamVariable remainder = new RamVariable(start: (ushort)(v.Start + size), size: diff, this);
						idx = Search(diff);
						m_free.Insert(idx, remainder);
					}
					return v;
				}
			}

			
			throw new System.OutOfMemoryException($"Allocator {this} is out of memory and can't allocate {size} bytes in 0x{Start:X}-0x{End:X}");
		}

		/// <summary>
		/// Return a variable to its owner
		/// </summary>
		/// <param name="var"></param>
		/// <returns>true when </returns>
		public void Free(RamVariable var)
		{
			if(var.Owner != this)
				throw new System.ArgumentException($"Variable {var} not owned by this allocator {this}");

			int idx = Search(var.Size);
			m_free.Insert(idx, var);
			// TODO: defrag / merge
		}
	}

	public class BankedRamAllocator
	{
		// TODO VRAM 0x8000-0xA000
		// TODO ERAM 0xA000-0xC000
		public static BankedRamAllocator WRAM =>  new BankedRamAllocator(start: 0xC000, bankStart: 0xD000, end: 0xE000, numBanks: 7);

		public ushort Start { get; }
		public ushort BankStart {get;}
		public ushort End { get; }

		private RamAllocator[] m_banks;
		public BankedRamAllocator(ushort start, ushort bankStart, ushort end, int numBanks)
		{
			Start = start;
			BankStart = bankStart;
			End = End;

			m_banks = new RamAllocator[numBanks+1];
			m_banks[0] = new(start: start, end: bankStart, bank: 0);
			for (int i = 1; i < numBanks+1; i++)
			{
				m_banks[i] = new(start: bankStart, end: end, bank: i);
			}
		}

		public RamVariable Alloc(ushort size, int bank)
		{
			return m_banks[bank].Alloc(size);
		}

		public RamVariable Alloc(ushort size, out int bank)
		{
			for (int i = 0; i < m_banks.Length; i++)
			{
				try
				{
					RamVariable v = m_banks[i].Alloc(size);
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

		public RamVariable? Alloc(ushort size) => Alloc(size: size, out int _);
	}
}