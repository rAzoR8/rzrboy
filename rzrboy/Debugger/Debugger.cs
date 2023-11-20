namespace dbg
{
	public class Debugger
	{
		public rzr.State CurrentState { get; private set; } = new();
		private rzr.Emu m_emu;

		public Debugger()
		{
			m_emu = new(ui.Logger.Instance);
		}

		public void Step()
		{
			m_emu.Step(CurrentState, debugPrint: true);
		}

		public void Restart()
		{
			var rom = CurrentState.mem.mbc.Rom();
			var boot = CurrentState.mem.boot.Data.ToArray();
			CurrentState = new();
			CurrentState.LoadRom(rom);
			CurrentState.LoadBootRom(boot);
		}

		public void LoadRom(string path) {
			ui.Logger.LogMsg( $"Loading ROM: {Path.GetFileName(path)}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadRom(task.Result) );
		}
		public void LoadBios(string path){
			ui.Logger.LogMsg( $"Loading Bios: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadBootRom(task.Result) );
		}
	}
}
