namespace emu
{
    public interface ISection 
    {
        string Name { get; }
        ushort Start { get; }
        ushort Length { get; }

        byte this[ushort address]
        {
            get;
            set;
        }
    }

    public static class SectionExtensions
    {
        public static bool Contains(this ISection section, ushort address)
        {
            return address >= section.Start && address < (section.Start + section.Length);
        }

        public static string ToString(this ISection sec) { return sec.Name; }
    }

    public class ProxySection : ISection
    {
        public ISection Source { get; set; }
        public ProxySection(ISection src) { Source = src; }

        public string Name => $"({Source.Name})*";
        public ushort Start => Source.Start;
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
        public ushort Start => Low.Start;
        public ushort Length => (ushort)(Low.Length + High.Length);

        public ISection Select(ushort address) => address < High.Start ? Low : High;

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
            Start = start;
            Length = length;
        }

        public string Name { get; }
        public ushort Start { get; }
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
            Start = start;
            Length = length;
        }

        public string Name { get; }
        public ushort Start { get; }
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
            Start = start;
            Length = length;
            DefaultReadValue = defaultReadValue;
        }

        public string Name { get; }
        public ushort Start { get; }
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
            Start = start;
            Length = len;
        }

        public string Name => $"{Start}->{Map(Start)}:{Source.Name}";
        public ushort Start { get; }
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
                return x.Start.CompareTo(y.Start);

            if (x == null && y != null) return -1; // x is less
            else if (x != null && y == null) return 1; // x is more
            return 0;
        }
    }

    // used to reflect address
    public class EmptySection : ISection
    {
        public EmptySection(ushort address) { Start = address; Length = 0; }

        public string Name => $"{Start}:Empty";
        public ushort Start { get; }
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
        public ushort Start => sections.First().Start;
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
                else if (address < sections[mid].Start)
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

        public byte this[ushort address]
        {
            get => Find(address)[address];
            set => Find(address)[address] = value;
        }
    }

    public class ByteSection : ISection
    {
        public byte mem { get; set; }
        public ByteSection(ushort start, byte val, string name)
        {
            Start = start;
            Length = 1;
            mem = val;
            Name = $"{start}:{name}";
        }

        public byte this[ushort address] { get => mem; set => mem = value; }

        public string Name { get; }

        public ushort Start { get; }

        public ushort Length { get; }
    }

    public class RWSection : ISection
    {
        public byte[] mem { get; }
        public RWSection(ushort start, ushort len, string name)
        {
            Start = start;
            Length = len;
            mem = new byte[len];
            Name = $"{start}:{name}";
        }

        public RWSection(ushort start, ushort len, string name, byte[] init) : this(start, len, name)
        {
            var size = (ushort)Math.Min(init.Length, len);
            Array.Copy(init, mem, size);
        }

        public byte this[ushort address] { get => mem[address - Start]; set => mem[address - Start] = value; }

        public string Name { get; }
        public ushort Start { get; }
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

        public new byte this[ushort address] { get => base.mem[address - Start]; }
    }

    public class WSection : RWSection
    {
        public WSection(ushort start, ushort len, string name) : base(start, len, name) { }

        public WSection(ushort start, ushort len, string name, byte[] init) : base(start, len, name, init) { }

        public new byte this[ushort address] { set => base.mem[address - Start] = value; }
    }
}
