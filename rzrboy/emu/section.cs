﻿using System.Collections;

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
        public bool Accepts( ushort address ) => address >= StartAddr && address < StartAddr + Length;
    }

    public class Storage : ISection
    {
        public IList<byte> Data { get; }

		/// <summary>
		/// Wrapper over some storage to allow Section like access
		/// </summary>
		/// <param name="storage"></param>
		/// <param name="storageOffset"></param>
		/// <param name="startAddr"></param>
		/// <param name="len"></param>
		public Storage( IList<byte> storage, int storageOffset = 0, ushort startAddr = 0, ushort len = 0 )
        {
			Data = storage;
            BufferOffset = storageOffset;
            StartAddr = startAddr;
			Length = len > 0 ? len : (ushort)( Data.Count - storageOffset );
		}

        public int BufferOffset { get; set; }
		public byte this[ushort address]
        {
			get => Data[BufferOffset + ( address - StartAddr )];
			set => Data[BufferOffset + ( address - StartAddr )] = value;
		}
        public ushort StartAddr { get; set; }
        public ushort Length { get; set; }
    }


    public static class SectionExtensions
    {
		public static Storage AsStorage( this IList<byte> storage ) { return new Storage( storage ); }
	}

    public class Section : ISection
    {
        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; }

        public byte[]? m_storage = null;

        public Section( ushort start = 0, ushort len = 0, string? name = null, bool alloc = true )
        {
            StartAddr = start;
            Length = len;
            if( alloc ) m_storage = new byte[len];
            Name = $"{start}:{name}";
        }

        public Section( ushort start, ushort len, string name, byte[] init ) : this( start, len, name, alloc: true )
        {
			var size = (ushort)Math.Min( init.Length, len );
            if( m_storage != null )
            {
                Array.Copy( init, m_storage, size );            
            }
		}

        public virtual bool Contains( ushort address )
        {
            return address >= StartAddr && address < ( StartAddr + Length );
        }

        public override string ToString() { return Name; }

		// mapped access for emulator, default impl
		public virtual byte this[ushort address]
        {
            get
            {
                if( m_storage != null )
                    return m_storage[address - StartAddr];
                else
                    throw new SectionReadAccessViolationException( address, this );
            }
            set
            {
                if( m_storage != null )
                    m_storage[address - StartAddr] = value;
                else
                    throw new SectionWriteAccessViolationException( address, this );
            }
        }

        public void Write( byte[] src, int src_offset, ushort dst_offset = 0, ushort len = 0 )
        {
            len = len != 0 ? Math.Min( len, (ushort)src.Length ) : (ushort)src.Length;
            if( m_storage != null )
            {
                Array.Copy( src, src_offset, m_storage, dst_offset, len );            
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

	public delegate byte ReadFunc( ushort address );
    public delegate void WriteFunc( ushort address, byte value );

	public delegate void OnRead( ISection section, ushort address );
	public delegate void OnWrite( ISection section, ushort address, byte value );

	public class RemapSection : ISection
    {
        public delegate ushort MapFunc(ushort address);
        public static MapFunc Identity = (ushort address) => address;

        public MapFunc Map { get; set; } = Identity;
        public ISection Source { get; set; }
		public RemapSection( MapFunc map, ushort start, ushort len, Section src )
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
