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

    public class RWSection : ISection
    {
        protected byte[] mem;
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
