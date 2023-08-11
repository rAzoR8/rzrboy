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

    /// <summary>
    /// Storage is a Non-execution ISection implementation that doesnt throw on out-of-bounds access and directly maps to a buffer
	/// Ignores ISection.Accepts
    /// </summary>
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

    /// <summary>
    /// Section is the execution implementation of ISection that is backed by memory
    /// </summary>
    public class Section : ISection
    {
        // ISection
        public string Name { get; }
        public ushort StartAddr { get; }
        public ushort Length { get; }

        public bool ReadOnly { get; set; } = false;
		public bool WriteOnly { get; set; } = false;

		public IList<byte> Data { get; }

		// this constructor allocates a byte array of length len
        public Section( ushort start, ushort len, string name )
        {
            StartAddr = start;
            Length = len;
            Data = new byte[len];
            Name = $"{start}:{name}";
        }

        public Section( ushort start, ushort len, string name, IList<byte> init )
        {
			StartAddr = start;
			Length = (ushort)Math.Min( init.Count, len );
			Name = $"{start}:{name}";
			Data = init;
		}

        public override string ToString() { return Name; }

		// mapped access for emulator, default impl
		public byte this[ushort address]
        {
			get
			{
				if( !WriteOnly && ( (ISection)this ).Accepts( address ) )
					return Data[address - StartAddr];
				else
					throw new SectionReadAccessViolationException( address, this );
			}
			set
            {
                if( !ReadOnly && ( (ISection)this ).Accepts( address ) )
                    Data[address - StartAddr] = value;
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
