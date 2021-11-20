namespace emu
{
    public interface ISection 
    {
        ushort Start { get; }
        ushort End { get; }

        byte this[ushort index]
        {
            get;
            set;
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

        public byte this[ushort index] { get => mem[index-Start]; set => mem[index - Start] = value; }

        public ushort Start { get; }

        public ushort End { get; }
    }

    public class RSection : RWSection
    {
        public RSection(ushort start, ushort end) : base(start, end){}

        public RSection(ushort start, ushort end, byte[] init) : base(start, end, init) { }

        public new byte this[ushort index] { get => base.mem[index - Start]; }
    }

    public class WSection : RWSection
    {
        public WSection(ushort start, ushort end) : base(start, end) { }

        public WSection(ushort start, ushort end, byte[] init) : base(start, end, init) { }

        public new byte this[ushort index] { set => base.mem[index - Start] = value; }
    }
}
