using System.Collections.Generic;

namespace rzr
{
	public interface ISection
	{
		byte this[ushort address] { get; set; }
		public ushort StartAddr { get; }
		public ushort Length { get; }

		public string Name => "unnamed";
		public bool Accepts( ushort address ) => address >= StartAddr && address < ( StartAddr + Length );
	}

	public interface IBankedMemory : IState
	{
		public int Banks { get; }
		public int SelectedBank { get; set; }
		IList<byte> this[int bank] { get; }
	}

	public enum IMEState : byte
	{
		Disabled,
		Enabled,
		RequestEnabled
	}

	public interface IState
	{
		public void Load( byte[] data );
		public byte[] Save();
	}

	public interface IRegisters
	{
		public byte A { get; set; }
		public byte F { get; set; }
		public byte B { get; set; }
		public byte C { get; set; }
		public byte D { get; set; }
		public byte E { get; set; }
		public byte H { get; set; }
		public byte L { get; set; }

		public ushort SP { get; set; }
		public ushort PC { get; set; }

		// TODO: move to CPU state
		public IMEState IME { get; set; }
		public bool Halted { get; set; }
	}

	public interface ICpuState : IState
	{
		public ushort CurrentInstrPC { get; set; }
		public byte CurrentInstrCycle { get; set; }
	}

	public interface IPpuState : IState
	{
		public ushort CurrentDot { get; set; }
	}

	public interface IEmuState
	{
		public IRegisters reg { get; }
		public ISection mem { get; } // map complete address space
		public IBankedMemory vram { get; }
		public IBankedMemory rom { get; }
		public IBankedMemory eram { get; }

		// Optional ?
		public ISection oam {  get; }
		public ISection io { get; }
		public ISection hram { get; }

		public ICpuState cpu { get; }
		//public IPpuState ppu { get; }
	}

	public interface IEmulator
	{
		/// <summary>
		/// Execute one M-Cycle
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public bool Tick(IEmuState state);
		public void Step(IEmuState state);

		public IEmuState CreateState();
	}

	public interface IGuiPlugin
	{
		public string Name { get; }
		public uint Revision { get; }
		public void OnMainMenu();
		public void OnWindow();
	}

	public interface ILogger
	{
		public void Log( string msg );
		public void Log( System.Exception exception );
	}

	public interface IEmuPlugin : IGuiPlugin
	{
		public IEmulator CreateEmulator( ILogger logger );
	}
}
