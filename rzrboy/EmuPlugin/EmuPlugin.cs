namespace rzr
{
	public class EmuPlugin : IEmuPlugin
	{
		public string Name => "rzrBoy";
		public uint Revision => 0;

		public IEmulator CreateEmulator( ILogger logger ) => new Emu( logger );

		public void OnMainMenu() { }
		public void OnWindow() { }
	}
}
