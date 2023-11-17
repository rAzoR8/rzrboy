namespace dbg
{
	public class Debugger
	{
		public rzr.Boy Emu {get;} = new();

		public void LoadRom(string path) {
			ui.Logger.Log( $"Loading ROM: {Path.GetFileName(path)}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => Emu.LoadRom(task.Result) );
		}
		public void LoadBios(string path){
			ui.Logger.Log( $"Loading Bios: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => Emu.LoadBootRom(task.Result) );
		}
	}
}
