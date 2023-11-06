using System.Diagnostics;

namespace rzr
{
	public class RamVariable : IDisposable
	{
		public ushort Start { get; set; }
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
		public int Bank {get;}

		private List<RamVariable> m_freeSize = new();
		private List<RamVariable> m_freeAddress = new();

		public RamAllocator(ushort start, ushort end, int bank = 0)
		{
			Start = start; End = end; Bank = bank;
			var whole = new RamVariable(start: start, size: (ushort)(end - start), this);
			m_freeSize.Add(whole);
			m_freeAddress.Add(whole);
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

		private int SearchAddress( ushort address )
		{
			int min = 0;
			int max = m_freeAddress.Count - 1;
			while (min <= max)
			{
				int mid = (min + max) / 2;
				ushort cur = m_freeAddress[mid].Start;
				if (address == cur)
				{
					return mid;
				}
				else if (address < cur)
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
			Debug.Assert(m_freeAddress.Count == m_freeSize.Count);

			if (m_freeSize.Count > 0)
			{
				int sidx = SearchSize(size);
				RamVariable v = m_freeSize[sidx];
				if (v.Size >= size)
				{
					m_freeSize.RemoveAt(sidx);
					int aidx = SearchAddress(v.Start);
					m_freeAddress.RemoveAt(aidx);
					if (v.Size > size) // split
					{
						ushort diff = (ushort)(v.Size - size);
						v.Size = size;
						RamVariable remainder = new RamVariable(start: (ushort)(v.Start + size), size: diff, this);

						sidx = SearchSize(remainder.Size);
						m_freeSize.Insert(sidx, remainder);
						aidx = SearchAddress(remainder.Start);
						m_freeAddress.Insert(aidx, remainder);
					}
					return v;
				}
			}
			
			throw new System.OutOfMemoryException($"Allocator {this} is out of memory and can't allocate {size} bytes in 0x{Start:X}-0x{End:X}");
		}

		/// <summary>
		/// Return a variable to its owner
		/// </summary>
		/// <param name="freed"></param>
		/// <returns>true when </returns>
		public void Free(RamVariable freed)
		{
			Debug.Assert(m_freeAddress.Count == m_freeSize.Count);

			if(freed.Owner != this)
				throw new System.ArgumentException($"Variable {freed} not owned by this allocator {this}");

			// merge: we want to merge-on-free so that the next call to free() will have access to bigger allocations again
			if(m_freeAddress.Count > 0)
			{
				// try to merge first, if not mergable, just insert
				int adr = SearchAddress(freed.Start);

				bool merged = false;
				for(int i = adr > 0 ? adr-1 : adr; !merged && i < adr+1 && i < m_freeAddress.Count; ++i)
				{
					var m = m_freeAddress[i];
					if(freed.Start == m.Start + m.Size) // starts after old end
					{						
						m_freeSize.RemoveAt(SearchSize(m.Size));
						m.Size += freed.Size; // just extend size to the right
						m_freeSize.Insert(SearchSize(m.Size), m);
						// address didnt change, no need to update m_freeAddress
						merged = true;
					}
					else if(freed.Start+freed.Size == m.Start) // ends at old star
					{
						m_freeAddress.RemoveAt(i);
						m_freeSize.RemoveAt(SearchSize(m.Size));

						m.Size += freed.Size;
						m.Start = freed.Start;

						m_freeSize.Insert(SearchSize(m.Size), m);
						m_freeAddress.Insert(SearchAddress(m.Start), m);

						merged = true;
					}
				}

				if(!merged) // unable to merge, just insert based on size
				{
					int sidx = SearchSize(freed.Size);
					m_freeSize.Insert(sidx, freed);
					int aidx = SearchSize(freed.Start);
					m_freeAddress.Insert(aidx, freed);
				}
			}
			else // nothing to merge with
			{
				m_freeSize.Add(freed);
				m_freeAddress.Add(freed);
			}
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

		public RamVariable Alloc(ushort size) => Alloc(size: size, out int _);
	}
}