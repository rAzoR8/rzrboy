namespace dbg
{
	public class Debugger
	{
		public delegate void StateChangedFn(rzr.State? oldState, rzr.State newState);

		public event StateChangedFn? StateChanged;

		// TODO: make private
		public rzr.State CurrentState { get; private set; } = new();
		private rzr.Emu m_emu;

		public Debugger()
		{
			m_emu = new(ui.Logger.Instance);
			StateChanged?.Invoke(null, CurrentState);
		}

		public void Step()
		{
			m_emu.Step(CurrentState, debugPrint: true);
		}

		public void Restart() // only reload ROM
		{
			var rom = CurrentState.SaveRom();
			var boot = CurrentState.mem.boot.Data.ToArray();
			var oldState = CurrentState;
			CurrentState = new();
			CurrentState.LoadRom(rom);
			CurrentState.LoadBootRom(boot);

			StateChanged?.Invoke(oldState, CurrentState);
		}

		public void LoadCpuState( string path )
		{
			ui.Logger.LogMsg( $"Loading CpuState: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadCpuState( task.Result ) );
		}
		public void LoadRom( string path )
		{
			ui.Logger.LogMsg( $"Loading ROM: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadRom( task.Result ) );
		}
		public void LoadBios( string path )
		{
			ui.Logger.LogMsg( $"Loading Bios: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadBootRom( task.Result ) );
		}
		public void LoadEram( string path )
		{
			ui.Logger.LogMsg( $"Loading ERam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadERam( task.Result ) );
		}
		public void LoadVRam( string path )
		{
			ui.Logger.LogMsg( $"Loading VRam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadVRam( task.Result ) );
		}
		public void LoadWRam( string path )
		{
			ui.Logger.LogMsg( $"Loading WRam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadWRam( task.Result ) );
		}
		public void LoadIO( string path )
		{
			ui.Logger.LogMsg( $"Loading IO: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadIO( task.Result ) );
		}
		public void LoadHRam( string path )
		{
			ui.Logger.LogMsg( $"Loading HRam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadHRam( task.Result ) );
		}
		public void LoadOam( string path )
		{
			ui.Logger.LogMsg( $"Loading Oam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadOam( task.Result ) );
		}
		public void LoadRegs( string path )
		{
			ui.Logger.LogMsg( $"Loading Regs: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.LoadRegs( task.Result ) );
		}

		public void LoadState( string stateFolder ) 
		{
			var cpu = Path.Combine( stateFolder, "cpu.bin" );
			if( File.Exists( cpu ) ) LoadCpuState( cpu );
			var regs = Path.Combine( stateFolder, "regs.bin" );
			if( File.Exists( regs ) ) LoadRegs( regs );
			var rom = Path.Combine( stateFolder, "rom.gb" );
			if( File.Exists( rom ) ) LoadRom( rom );
			var eram = Path.Combine( stateFolder, "eram.bin" );
			if( File.Exists( eram ) ) LoadEram( eram );
			var vram = Path.Combine( stateFolder, "vram.bin" );
			if( File.Exists( vram ) ) LoadVRam( vram );
			var wram = Path.Combine( stateFolder, "wram.bin" );
			if( File.Exists( wram ) ) LoadWRam( wram );
			var oam = Path.Combine( stateFolder, "oam.bin" );
			if( File.Exists( oam ) ) LoadOam( oam );
			var io = Path.Combine( stateFolder, "io.bin" );
			if( File.Exists( io ) ) LoadIO( io );
			var hram = Path.Combine( stateFolder, "hram.bin" );
			if( File.Exists( hram ) ) LoadHRam( hram );
		}

		public void SaveState( string stateFolder )
		{
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "cpu.bin" ), CurrentState.SaveCpuState() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "regs.bin" ), CurrentState.SaveRegs() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "rom.gb" ), CurrentState.SaveRom() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "eram.bin" ), CurrentState.SaveERam() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "wram.bin" ), CurrentState.SaveWRam() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "vram.bin" ), CurrentState.SaveVRam() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "io.bin" ), CurrentState.SaveIO() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "hram.bin" ), CurrentState.SaveHRam() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "oam.bin" ), CurrentState.SaveOam() );
		}
	}
}
