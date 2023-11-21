using System.Runtime.InteropServices;

namespace rzr
{
	[StructLayout(LayoutKind.Sequential)] 
	public record struct Rgba32(byte R, byte G, byte B, byte A = 0xFF)
	{
		public static readonly Rgba32 Red = new(0xFF, 0, 0);
		public static readonly Rgba32 Green = new(0, 0xFF, 0);
		public static readonly Rgba32 Blue = new(0, 0, 0xFF);
		public static readonly Rgba32 Black = new(0, 0, 0);
		public static readonly Rgba32 White = new(0xFF, 0xFF, 0xFF);
	}

	// https://www.c-sharpcorner.com/article/efficiently-working-with-arrays-and-memory-in-c-sharp-using-spant/
	public class ImageBuffer
	{
		public uint Width {get;}
		public uint Height {get;}

		private Rgba32[] m_pixels;

		public Rgba32[] GetPixels() => m_pixels; // Array.Copy ?

		/// <summary>
		/// Indicates if memory has been modified and needs to be changed
		/// </summary>
		public bool Dirty {get; set;} = true;

		public ImageBuffer(uint width, uint height, Rgba32 defaultVal = default)
		{
			Width = width;
			Height = height;
			m_pixels = new Rgba32[width*height];
			Array.Fill(m_pixels, defaultVal);
		}	

		public Rgba32 this[int x, int y] { get => m_pixels[y*Width+x]; set {m_pixels[y*Width+x] = value; Dirty = true;} }
	}

	/// <summary>
	/// State of the PPU
	/// </summary>
	/// 
	public class Pix
	{
		public enum PpuMode : byte
		{
			HBlank = 0,
			VBlank = 1,
			OAMSearch = 2,
			Drawing = 3
		}

		// TODO: count in dots?
		//A “dot” = one 2^22 Hz (≅ 4.194 MHz) time unit. Dots remain the same regardless of whether the CPU is in double speed, so there are 4 dots per single-speed CPU cycle, and 2 per double-speed CPU cycle.
		public uint Tick {get;set;} = 0;
		public PpuMode Mode {get; set;} = PpuMode.VBlank; // TODO: figure out which is the correct starting mode
		public ImageBuffer FrameBuffer {get;} = new(160, 144, Rgba32.Black);

		public struct Pixel
		{
			public byte Color; // [0..3]
			public byte Palette; // [0..7] DMG applies to OBJ only, GGB applies go BG and OBJ
			public byte SpritePrioCGB; // OAM index for the object
			public byte BgPrioCGB; // TODO convert to bool? Priority: 0 = No, 1 = BG and Window colors 1–3 are drawn over this OBJ
		}

		public List<Pixel> BgWinFifo  {get;} = new();
		public List<Pixel> ObjFifo {get;} = new();

		[Flags]
		public enum SpriteAttributes : byte
		{
			Priority = 0b1000,
			FlipY = 0b0100,
			FlipX = 0b0010,
			Palette = 0b0001,
		}

		[StructLayout(LayoutKind.Sequential)] 
		public struct Sprite // https://gbdev.io/pandocs/OAM.html
		{
			public byte X;
			public byte Y;
			public byte Tile; // Tile index
			public SpriteAttributes Attrib;

			public bool FlipY => Attrib.HasFlag(SpriteAttributes.FlipY); // TODO: move to extension function?
			public bool FlipX => Attrib.HasFlag(SpriteAttributes.FlipX);
			public bool Priority => Attrib.HasFlag(SpriteAttributes.Priority);
			public bool Palette => Attrib.HasFlag(SpriteAttributes.Palette);	
		}

		public Pix()
		{
			// TODO: remove
			FrameBuffer[160/2, 144/2] = Rgba32.White;
		}
	}
}