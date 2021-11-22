namespace emu
{
    public interface ISection 
    {
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
    }

    public class ProxySection : ISection
    {
        public ISection Source { get; set; }
        public ProxySection(ISection src = null) { Source = src; }

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

        public CombiSection(ISection low = null, ISection high = null) { Low = low; High = high; }

        public ushort Start => Low.Start;
        public ushort Length => (ushort)(Low.Length + High.Length);

        public ISection Select(ushort address) => address < High.Start ? Low : High;

        public byte this[ushort address]
        {
            get => Select(address)[address];
            set => Select(address)[address] = value;
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
        public ByteSection(ushort start, byte val = 0)
        {
            Start = start;
            Length = 1;
            mem = val;
        }

        public byte this[ushort address] { get => mem; set => mem = value; }

        public ushort Start { get; }

        public ushort Length { get; }
    }

    public class RWSection : ISection
    {
        public byte[] mem { get; }
        public RWSection(ushort start, ushort len)
        {
            Start = start;
            Length = len;
            mem = new byte[len];
        }

        public RWSection(ushort start, ushort len, byte[] init) : this(start, len)
        {
            var size = (ushort)Math.Min(init.Length, len);
            Array.Copy(init, mem, size);
        }

        public byte this[ushort address] { get => mem[address - Start]; set => mem[address - Start] = value; }

        public ushort Start { get; }

        public ushort Length { get; }

        public void write(byte[] src, ushort address, ushort len = 0)
        {
            address -= Start;
            len = len != 0 ? Math.Min(len, (ushort)src.Length) : (ushort)src.Length;
            Array.Copy(src, 0, mem, address, len);
        }
    }

    public class RSection : RWSection
    {
        public RSection(ushort start, ushort len) : base(start, len){}

        public RSection(ushort start, ushort len, byte[] init) : base(start, len, init) { }

        public new byte this[ushort address] { get => base.mem[address - Start]; }
    }

    public class WSection : RWSection
    {
        public WSection(ushort start, ushort len) : base(start, len) { }

        public WSection(ushort start, ushort len, byte[] init) : base(start, len, init) { }

        public new byte this[ushort address] { set => base.mem[address - Start] = value; }
    }
}
