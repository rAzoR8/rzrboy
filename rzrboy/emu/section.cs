﻿namespace rzr
{
    public class SectionReadAccessViolationException : System.AccessViolationException
    {
        public SectionReadAccessViolationException( ushort address, ISection section ) : base( $"0x{address.ToString( "X4" )} can not be read from section {section.Name}" ) { }
    }

    public class SectionWriteAccessViolationException : System.AccessViolationException
    {
        public SectionWriteAccessViolationException( ushort address, ISection section ) : base( $"0x{address.ToString( "X4" )} can not be written to section {section.Name}" ) { }
    }

    public interface ISection 
    {
        string Name { get; }
        ushort StartAddr { get; }
        ushort Length { get; }

        IList<byte>? Storage => null;

		// direct access for debugger
		byte Read( ushort address )
		{
			if( Storage == null )
				throw new SectionReadAccessViolationException( address, this );
			else
				return Storage[address];
		}
		void Write( ushort address, byte value )
		{
			if( Storage == null )
				throw new SectionWriteAccessViolationException( address, this );
			else
				Storage[address] = value;
		}

		// mapped access for emulator, default impl
		byte this[ushort address]
        {
            get => Read( address );
            set => Write( address, value );
        }
    }

    public static class SectionExtensions
    {
        public static bool Contains(this ISection section, ushort address)
        {
            return address >= section.StartAddr && address < (section.StartAddr + section.Length);
        }

        public static string ToString(this ISection sec) { return sec.Name; }
    }

    public class ProxySection : ISection
    {
        public ISection Source { get; set; }
        public ProxySection(ISection src) { Source = src; }

        public string Name => $"({Source.Name})*";
        public ushort StartAddr => Source.StartAddr;
        public ushort Length => Source.Length;
        public byte this[ushort address]
        {
            get => Source[address];
            set => Source[address] = value;
        }
    }

    public class CombiSection : ISection
    {
        public ISection Low { get; set; }
        public ISection High { get; set; }

        public CombiSection(ISection low, ISection high) { Low = low; High = high; }

        public string Name => $"({Low.Name})({High.Name})";
        public ushort StartAddr => Low.StartAddr;
        public ushort Length => (ushort)(Low.Length + High.Length);

        public ISection Select(ushort address) => address < High.StartAddr ? Low : High;

        public byte this[ushort address]
        {
            get => Select(address)[address];
            set => Select(address)[address] = value;
        }
    }

    public delegate byte ReadFunc( ushort address );
    public delegate void WriteFunc( ushort address, byte value );

    public class RWInterceptSection : ISection
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

    public class RInterceptSection : ISection
    {
        public ReadFunc Read { get; set; }

        public RInterceptSection( ReadFunc read, string name, ushort start, ushort length )
        {
            Read = read;
            Name = name;
            StartAddr = start;
            Length = length;
        }

        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; }
        public byte this[ushort address]
        {
            get => Read( address );
            set { }
        }
    }

    public class WInterceptSection : ISection
    {
        public WriteFunc Write { get; set; }

        public byte DefaultReadValue { get; set; } = 0xFF;

        public WInterceptSection( WriteFunc write, byte defaultReadValue, string name, ushort start, ushort length )
        {
            Write = write;
            Name = name;
            StartAddr = start;
            Length = length;
            DefaultReadValue = defaultReadValue;
        }

        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; }
        public byte this[ushort address]
        {
            get => DefaultReadValue;
            set => Write( address, value );
        }
    }

    public class RemapSection : ISection
    {
        public delegate ushort MapFunc(ushort address);
        public static MapFunc Identity = (ushort address) => address;

        public MapFunc Map { get; set; } = Identity;
        public ISection Source { get; set; }
        public RemapSection(MapFunc map, ushort start, ushort len, ISection src = null)
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

    public class SectionComparer : IComparer<ISection>
    {
        public int Compare(ISection? x, ISection? y)
        {
            if (x != null && y != null)
                return x.StartAddr.CompareTo(y.StartAddr);

            if (x == null && y != null) return -1; // x is less
            else if (x != null && y == null) return 1; // x is more
            return 0;
        }
    }

    // used to reflect address
    public class EmptySection : ISection
    {
        public EmptySection(ushort address) { StartAddr = address; Length = 0; }

        public string Name => $"{StartAddr}:Empty";
        public ushort StartAddr { get; }
        public ushort Length { get; }
        public byte this[ushort address]
        {
            get => throw new AccessViolationException();
            set => throw new AccessViolationException();
        }
    }

    public class ListSection : ISection
    {
        private List<ISection> sections = new();

        public string Name => sections.Count != 0 ? $"{sections.First().Name}...{sections.Last().Name}" : "";
        public ushort StartAddr => sections.First().StartAddr;
        public ushort Length => (ushort)sections.Sum( s => s.Length );

        public void Add(ISection section)
        {
            sections.Add(section);
        }

        private ISection Find(ushort address)
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

        public delegate void OnRead(ISection section, ushort address);
        public delegate void OnWrite(ISection section, ushort address, byte value);

        public List<OnRead> ReadCallbacks { get; } = new();
        public List<OnWrite> WriteCallbacks { get; } = new();

        private byte Read(ushort address ) 
        {
            ISection section = Find(address);
            foreach ( OnRead onRead in ReadCallbacks )
            {
                onRead( section, address );
            }
            return section[address];
        }

        private void Write( ushort address, byte val )
        {
            ISection section = Find( address );
            section[address] = val;
            foreach ( OnWrite onWrite in WriteCallbacks )
            {
                onWrite( section, address, val );
            }
        }

        public byte this[ushort address]
        {
            get => Read( address );
            set => Write( address, value );
        }
    }

    public class ByteSection : ISection
    {
        public OnReadByte? OnRead { get; set; }
        public OnWriteByte? OnWrite { get; set; }

        protected byte m_value;

		public byte Value
        {
			get { OnRead?.Invoke( m_value ); return m_value; }
            set { OnWrite?.Invoke( m_value, value ); m_value = value; }
        }

		public ByteSection( ushort start, byte val, string name )
		{
			StartAddr = start;
			Length = 1;
			Value = val;
			Name = $"{start}:{name}";
		}

        // value read
        public delegate void OnReadByte ( byte val );
        public delegate void OnWriteByte ( byte oldVal, byte newVal );

		public byte this[ushort address] { get => Value; set => Value = value; }

        public static implicit operator byte( ByteSection sec ) { return sec.Value; }

        public string Name { get; }

        public ushort StartAddr { get; }

        public ushort Length { get; }
    }

    public class RWSection : ISection
    {
        public byte[] mem { get; }
        public RWSection(ushort start, ushort len, string name)
        {
            StartAddr = start;
            Length = len;
            mem = new byte[len];
            Name = $"{start}:{name}";
        }

        public RWSection(ushort start, ushort len, string name, byte[] init) : this(start, len, name)
        {
            var size = (ushort)Math.Min(init.Length, len);
            Array.Copy(init, mem, size);
        }

        public byte this[ushort address] { get => mem[address - StartAddr]; set => mem[address - StartAddr] = value; }

        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; }

        public void write( byte[] src, int src_offset, ushort dst_offset = 0, ushort len = 0 )
        {
            len = len != 0 ? Math.Min( len, (ushort)src.Length ) : (ushort)src.Length;
            Array.Copy( src, src_offset, mem, dst_offset, len );
        }
    }

    public class RSection : RWSection
    {
        public RSection(ushort start, ushort len, string name) : base(start, len, name){}

        public RSection(ushort start, ushort len, string name, byte[] init) : base(start, len, name, init) { }

        public new byte this[ushort address] { get => base.mem[address - StartAddr]; }
    }

    public class WSection : RWSection
    {
        public WSection(ushort start, ushort len, string name) : base(start, len, name) { }

        public WSection(ushort start, ushort len, string name, byte[] init) : base(start, len, name, init) { }

        public new byte this[ushort address] { set => base.mem[address - StartAddr] = value; }
    }
}
