namespace dbg
{
	public class Debugger
	{
		public delegate void StateChangedFn(rzr.IEmuState? oldState, rzr.IEmuState newState );

		public event StateChangedFn? StateChanged;

		// TODO: make private
		public rzr.IEmuState CurrentState { get; private set; }
		private rzr.IEmulator m_emu;

		public Debugger(rzr.IEmuPlugin provider)
		{
			m_emu = provider.CreateEmulator( ui.Logger.Instance );
			CurrentState = m_emu.CreateState();//new rzr.State( m_emu.cpu );
			StateChanged?.Invoke(null, CurrentState);
		}

		public void Step()
		{
			m_emu.Step(CurrentState);
		}

		public void Restart() // only reload ROM
		{
			var rom = CurrentState.rom.Save();
			//var boot = CurrentState.SaveBootRom();
			var oldState = CurrentState;
			CurrentState = m_emu.CreateState();
			CurrentState.rom.Load(rom);
			//CurrentState.LoadBootRom(boot);

			StateChanged?.Invoke(oldState, CurrentState);
		}

		public void LoadCpuState( string path )
		{
			ui.Logger.LogMsg( $"Loading CpuState: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.cpu.Load(task.Result));
		}
		public void LoadRom( string path )
		{
			ui.Logger.LogMsg( $"Loading ROM: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.rom.Load( task.Result ) );
		}
		public void LoadBios( string path )
		{
			ui.Logger.LogMsg( $"Loading Bios: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.bios.Load( task.Result ) );
		}
		public void LoadEram( string path )
		{
			ui.Logger.LogMsg( $"Loading ERam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.eram.Load( task.Result ) );
		}
		public void LoadVRam( string path )
		{
			ui.Logger.LogMsg( $"Loading VRam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.vram.Load( task.Result ) );
		}
		public void LoadWRam( string path )
		{
			ui.Logger.LogMsg( $"Loading WRam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.wram.Load( task.Result ) );
		}
		public void LoadIO( string path )
		{
			ui.Logger.LogMsg( $"Loading IO: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.io.Load( task.Result ) );
		}
		public void LoadHRam( string path )
		{
			ui.Logger.LogMsg( $"Loading HRam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.hram.Load( task.Result ) );
		}
		public void LoadOam( string path )
		{
			ui.Logger.LogMsg( $"Loading Oam: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.oam.Load( task.Result ) );
		}
		public void LoadRegs( string path )
		{
			ui.Logger.LogMsg( $"Loading Regs: {Path.GetFileName( path )}" );
			File.ReadAllBytesAsync( path ).ContinueWith( task => CurrentState.reg.Load( task.Result ) );
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
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "cpu.bin" ), CurrentState.cpu.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "regs.bin" ), CurrentState.reg.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "rom.gb" ), CurrentState.rom.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "eram.bin" ), CurrentState.eram.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "wram.bin" ), CurrentState.wram.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "vram.bin" ), CurrentState.vram.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "io.bin" ), CurrentState.io.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "hram.bin" ), CurrentState.hram.Save() );
			File.WriteAllBytesAsync( Path.Combine( stateFolder, "oam.bin" ), CurrentState.oam.Save() );
		}
	}
}
