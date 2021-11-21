namespace emu
{
    public interface ISection 
    {
        ushort Start { get; }
        ushort End { get; }

        byte this[ushort address]
        {
            get;
            set;
        }
    }

    public class ProxySection : ISection
    {
        public ISection Source { get; set; }
        public ProxySection(ISection src = null) { Source = src; }

        public ushort Start => Source.Start;
        public ushort End => Source.End;
        public byte this[ushort address]
        {
            get => Source[address];
            set => Source[address] = value;
        }
    }

    public class RemapSection : ISection
    {
        public delegate ushort MapFunc(ushort address);
        public static MapFunc Identity = (ushort address) => address;

        public MapFunc Map { get; set; } = Identity;
        public ISection Source { get; set; }
        public RemapSection(MapFunc map, ushort start, ushort end, ISection src = null)
        {
            Map = map;
            Source = src;
            Start = start;
            End = end;
        }

        public ushort Start { get; }
        public ushort End { get; }
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
        public EmptySection(ushort address) { Start = address; End = address; }
        public ushort Start { get; }
        public ushort End { get; }
        public byte this[ushort address]
        {
            get => throw new AccessViolationException();
            set => throw new AccessViolationException();
        }
    }

    public class ListSection : ISection
    {
        private List<ISection> sections = new();

        public ushort Start => sections.First().Start;
        public ushort End => sections.Last().End;

        public void Add(ISection section)
        {
            sections.Add(section);
        }

        private ISection Find(ushort address)
        {
            EmptySection addr = new(address);
            int pos = sections.BinarySearch(0, sections.Count, addr, new SectionComparer());
            if (pos >= 0)
                return sections[pos];
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
        public ByteSection(ushort start, byte val = 0)
        {
            Start = start;
            End = start;
            mem = val;
        }

        public byte this[ushort address] { get => mem; set => mem = value; }

        public ushort Start { get; }

        public ushort End { get; }
    }

    public class RWSection : ISection
    {
        public byte[] mem { get; }
        public RWSection(ushort start, ushort end)
        {
            Start = start;
            End = end;
            mem = new byte[End-Start];
        }

        public RWSection(ushort start, ushort end, byte[] init) : this(start, end)
        {
            Array.Copy(init, mem, mem.Length);
        }

        public byte this[ushort address] { get => mem[address - Start]; set => mem[address - Start] = value; }

        public ushort Start { get; }

        public ushort End { get; }
    }

    public class RSection : RWSection
    {
        public RSection(ushort start, ushort end) : base(start, end){}

        public RSection(ushort start, ushort end, byte[] init) : base(start, end, init) { }

        public new byte this[ushort address] { get => base.mem[address - Start]; }
    }

    public class WSection : RWSection
    {
        public WSection(ushort start, ushort end) : base(start, end) { }

        public WSection(ushort start, ushort end, byte[] init) : base(start, end, init) { }

        public new byte this[ushort address] { set => base.mem[address - Start] = value; }
    }
}
