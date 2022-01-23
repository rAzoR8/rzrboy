using System.Collections;

namespace rzr
{
    public class SectionReadAccessViolationException : System.AccessViolationException
    {
        public SectionReadAccessViolationException( ushort address, Section section ) : base( $"0x{address.ToString( "X4" )} can not be read from section {section.Name}" ) { }
    }

    public class SectionWriteAccessViolationException : System.AccessViolationException
    {
        public SectionWriteAccessViolationException( ushort address, Section section ) : base( $"0x{address.ToString( "X4" )} can not be written to section {section.Name}" ) { }
    }

    public class Section
    {
        public virtual string Name { get; }
        public virtual ushort StartAddr { get; }
        public virtual ushort Length { get; }

        public virtual IList<byte>? Storage { get; set; }

        public static implicit operator byte[](Section sec) { return sec.Storage as byte[]; }

        public Section( ushort start = 0, ushort len = 0, string? name = null, bool alloc = true )
        {
            StartAddr = start;
            Length = len;
            if( alloc ) Storage = new byte[len];
            Name = $"{start}:{name}";
        }

        public Section( ushort start, ushort len, string name, byte[] init ) : this( start, len, name, alloc: true )
        {
			var size = (ushort)Math.Min( init.Length, len );
			if( Storage != null ) Array.Copy( init, Storage as byte[], size );
		}

        public virtual bool Contains(ushort address )
        {
            return address >= StartAddr && address < ( StartAddr + Length );
        }

        public override string ToString() { return Name; }

        // direct access for debugger
        public virtual byte Read( ushort address )
		{
			if( Storage == null )
				throw new SectionReadAccessViolationException( address, this );
			else
				return Storage[address-StartAddr];
		}
		public virtual void Write( ushort address, byte value )
		{
			if( Storage == null )
				throw new SectionWriteAccessViolationException( address, this );
			else
				Storage[address-StartAddr] = value;
		}

		// mapped access for emulator, default impl
		public virtual byte this[ushort address]
        {
            get => Read( address );
            set => Write( address, value );
        }

        public void Write( byte[] src, int src_offset, ushort dst_offset = 0, ushort len = 0 )
        {
            len = len != 0 ? Math.Min( len, (ushort)src.Length ) : (ushort)src.Length;
            Array.Copy( src, src_offset, this, dst_offset, len );
        }
    }

	public class ProxySection : Section
    {
        public Section Source { get; set; }
        public ProxySection(Section src) { Source = src; }

        public string Name => $"({Source.Name})*";
        public ushort StartAddr => Source.StartAddr;
        public ushort Length => Source.Length;
        public byte this[ushort address]
        {
            get => Source[address];
            set => Source[address] = value;
        }
	}

    public class CombiSection : Section
    {
        public Section Low { get; set; }
        public Section High { get; set; }

        public CombiSection(Section low, Section high) { Low = low; High = high; }

        public override string Name => $"({Low.Name})({High.Name})";
        public override ushort StartAddr => Low.StartAddr;
        public override ushort Length => (ushort)(Low.Length + High.Length);

        public Section Select(ushort address) => address < High.StartAddr ? Low : High;

        public override byte this[ushort address]
        {
            get => Select(address)[address];
            set => Select(address)[address] = value;
        }
    }

    public delegate byte ReadFunc( ushort address );
    public delegate void WriteFunc( ushort address, byte value );

    public class RWInterceptSection : Section
    {
        public ReadFunc Read { get; set; }
        public WriteFunc Write { get; set; }

        public RWInterceptSection( ReadFunc read, WriteFunc write, string name, ushort start, ushort length )
        {
            Read = read;
            Write = write;
            Name = name;
            StartAddr = start;
            Length = length;
        }

        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; }
        public byte this[ushort address]
        {
            get => Read(address);
            set => Write(address,value);
        }
    }

    public class RemapSection : Section
    {
        public delegate ushort MapFunc(ushort address);
        public static MapFunc Identity = (ushort address) => address;

        public MapFunc Map { get; set; } = Identity;
        public Section Source { get; set; }
        public RemapSection(MapFunc map, ushort start, ushort len, Section src = null)
        {
            Map = map;
            Source = src;
            StartAddr = start;
            Length = len;
        }

        public string Name => $"{StartAddr}->{Map(StartAddr)}:{Source.Name}";
        public ushort StartAddr { get; }
        public ushort Length { get; }
        public byte this[ushort address]
        {
            get => Source[Map(address)];
            set => Source[Map(address)] = value;
        }
    }

    public class SectionComparer : IComparer<Section>
    {
        public int Compare(Section? x, Section? y)
        {
            if (x != null && y != null)
                return x.StartAddr.CompareTo(y.StartAddr);

            if (x == null && y != null) return -1; // x is less
            else if (x != null && y == null) return 1; // x is more
            return 0;
        }
    }

    public class ListSection : Section
    {
        private List<Section> sections = new();

        public override ushort StartAddr => sections.First().StartAddr;
        public override ushort Length => (ushort)sections.Sum( s => s.Length );

        public void Add(Section section)
        {
            sections.Add(section);
        }

        private Section Find(ushort address)
        {
            int min = 0;
            int max = sections.Count - 1;

            while (min <= max)
            {
                int mid = (min + max) / 2;
                if (sections[mid].Contains(address))
                {
                    return sections[mid];
                }
                else if (address < sections[mid].StartAddr)
                {
                    max = mid - 1;
                }
                else
                {
                    min = mid + 1;
                }
            }

            throw new AddressNotMappedException(address);
        }

        public delegate void OnRead(Section section, ushort address);
        public delegate void OnWrite(Section section, ushort address, byte value);

        public List<OnRead> ReadCallbacks { get; } = new();
        public List<OnWrite> WriteCallbacks { get; } = new();

		public override byte Read( ushort address )
		{
			Section section = Find( address );
			foreach( OnRead onRead in ReadCallbacks )
			{
				onRead( section, address );
			}
			return section[address];
		}

		public override void Write( ushort address, byte val )
		{
			Section section = Find( address );
			section[address] = val;
			foreach( OnWrite onWrite in WriteCallbacks )
			{
				onWrite( section, address, val );
			}
		}
	}

    public class ByteSection : Section
    {
        public OnReadByte? OnRead { get; set; }
        public OnWriteByte? OnWrite { get; set; }

        protected byte m_value;

		public byte Value
        {
			get { OnRead?.Invoke( m_value ); return m_value; }
            set { OnWrite?.Invoke( m_value, value ); m_value = value; }
        }

		public ByteSection( ushort start, byte val, string name ) : base( start: start, len: 1, name: name )
		{
			Value = val;
		}

        // value read
        public delegate void OnReadByte ( byte val );
        public delegate void OnWriteByte ( byte oldVal, byte newVal );

		public  override byte this[ushort address] { get => Value; set => Value = value; }

        public static implicit operator byte( ByteSection sec ) { return sec.Value; }
    }
}
