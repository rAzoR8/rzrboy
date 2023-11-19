namespace dbg
{
	public class Debugger
	{
		public rzr.State CurrentState { get; } = new();
		public rzr.Emu Emu {get;} = new();

		public void Step()
		{
			Emu.Step(CurrentState, debugPrint: true);
		}

		public void LoadRom(string path) {
			ui.Logger.Log( $"Loading ROM: {Path.GetFileName(path)}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadRom(task.Result) );
		}
		public void LoadBios(string path){
			ui.Logger.Log( $"Loading Bios: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadBootRom(task.Result) );
		}
	}
}
