using System.Collections;

namespace rzr
{
    public class SectionReadAccessViolationException : rzr.ExecException
	{
        public SectionReadAccessViolationException(string message) : base(message) { }
		public SectionReadAccessViolationException( ushort address, Section section ) : base( $"0x{address.ToString( "X4" )} can not be read from section {section.Name}" ) { }
    }

    public class SectionWriteAccessViolationException : rzr.ExecException
	{
        public SectionWriteAccessViolationException( string message ) : base(message) { }
        public SectionWriteAccessViolationException( ushort address, Section section ) : base( $"0x{address.ToString( "X4" )} can not be written to section {section.Name}" ) { }
    }

    public interface ISection
    {
        byte this[ushort address] { get; set; }
        public ushort StartAddr { get; }
        public ushort Length { get; }

        public string Name => "unnamed";
        public bool Accepts( ushort address ) => address >= StartAddr && address < (StartAddr + Length);
    }

	// Dummy section that reads and writes nothing
	public class NullSection : ISection
	{
		public byte this[ushort address] { get => 0; set { } }
		public ushort StartAddr => 0;
		public ushort Length => 0;
	}

	public delegate void OnRead( ISection section, ushort address );
	public delegate void OnWrite( ISection section, ushort address, byte value );

	[Flags]
	public enum SectionAccess
	{
		None = 0,
		Read = 1 << 0,
		Write = 1 << 1,
		ReadWrite = Read | Write
	}

	/// <summary>
	/// Section is the implementation of ISection that is backed by memory
	/// </summary>
	public class Section : ISection
    {
        // ISection
        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; set; }

		// Section
		public IList<byte> Data { get; private set; }
		public int BufferOffset { get; set; } = 0;
		public SectionAccess Access { get; } = SectionAccess.ReadWrite;

		public byte[] Save() => Data.Skip(BufferOffset).Take(Length).ToArray();
		public void Load( byte[] data, int bufferOffset = 0) { Data = data; BufferOffset = bufferOffset; }

		public Section AsReadOnly() => new Section( start: StartAddr, len: Length, name: Name, access: SectionAccess.Read, data: Data, offset: BufferOffset );
		public Section AsReadWrite() => new Section( start: StartAddr, len: Length, name: Name, access: SectionAccess.ReadWrite, data: Data, offset: BufferOffset );

		// this constructor allocates a byte array of length len
		public Section( ushort start, ushort len, string name, SectionAccess access )
        {
            Name = $"{start}:{name}";
            StartAddr = start;
            Length = len;
            Data = new byte[len];
			BufferOffset = 0;
			Access = access;
		}

		// this constructor uses data passed in to back this section
        public Section( ushort start, ushort len, string name, SectionAccess access, IList<byte> data, int offset = 0 )
        {
			Name = $"{start}:{name}";
			StartAddr = start;
			Length = len;
			Data = data;
			BufferOffset = offset;
			Access = access;
		}

		public override string ToString() { return Name; }

		// mapped access for emulator, default impl
		public byte this[ushort address]
        {
			get
			{
				if( Access.HasFlag( SectionAccess.Read ) && ( (ISection)this ).Accepts( address ) )
					return Data[BufferOffset + address - StartAddr];
				else
					throw new SectionReadAccessViolationException( address, this );
			}
			set
			{
				if( Access.HasFlag( SectionAccess.Write ) && ( (ISection)this ).Accepts( address ) )
					Data[BufferOffset + address - StartAddr] = value;
				else
					throw new SectionWriteAccessViolationException( address, this );
			}
		}

        public void Write( IList<byte> src, int src_offset, ushort dst_offset = 0, ushort len = 0 )
        {
            len = len != 0 ? Math.Min( len, (ushort)src.Count ) : (ushort)src.Count;
            if( Data != null )
            {
				for( int i = 0; i < len; ++i ) 
				{
					Data[dst_offset + i] = src[src_offset + i];
				}
            }
        }
    }

	public class CombiSection : ISection
	{
		public ISection Low { get; set; }
		public ISection High { get; set; }

		public CombiSection( ISection low, ISection high ) { Low = low; High = high; }

		public string Name => $"({Low.Name})({High.Name})";
		public ushort StartAddr => Low.StartAddr;
		public ushort Length => (ushort)( Low.Length + High.Length );

		public ISection Select( ushort address ) => address < High.StartAddr ? Low : High;

		public byte this[ushort address]
		{
			get => Select( address )[address];
			set => Select( address )[address] = value;
		}
	}

	public class RemapSection : ISection
	{
		public delegate ushort MapFunc( ushort address );
		public static MapFunc Identity = ( ushort address ) => address;

		public MapFunc Map { get; set; } = Identity;
		public ISection Source { get; set; }

		public RemapSection( MapFunc map, ushort start, ushort len, ISection src )
		{
			Map = map;
			Source = src;
			StartAddr = start;
			Length = len;
		}

		public string Name => $"{StartAddr}->{Map( StartAddr )}:{Source.Name}";
		public ushort StartAddr { get; }
		public ushort Length { get; }
		public byte this[ushort address]
		{
			get => Source[Map( address )];
			set => Source[Map( address )] = value;
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

		public ushort StartAddr { get; }
        public ushort Length => 1;
        public string Name { get; }

		public ByteSection( ushort start, byte val, string name )
		{
			StartAddr = start;
			Value = val;
            Name = name;
		}

        // value read
        public delegate void OnReadByte ( byte val );
        public delegate void OnWriteByte ( byte oldVal, byte newVal );

		public byte this[ushort address] { get => Value; set => Value = value; }

        public static implicit operator byte( ByteSection sec ) { return sec.Value; }
	}
}
