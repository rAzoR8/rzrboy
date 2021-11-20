namespace emu
{
    public class AddressNotMappedException : System.AccessViolationException 
    {
        public AddressNotMappedException(ushort address) : base($"0x{address.ToString("X4")} not mapped to any memory section") { }
    }

    public class mem : ISection
    {
        private List<ISection> sections = new();

        public ushort Start => sections.First().Start;
        public ushort End => sections.Last().End;

        public void Add(ISection section) 
        {
            // sort by address
            int pos = sections.FindIndex(0, s => section.Start < s.Start);
            sections.Insert(pos == -1 ? 0 : pos, section);
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
}
