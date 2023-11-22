using ImGuiNET;
using System.Text.Json;

namespace dbg.ui
{
	[Serializable]
	public class GuiState
	{
		public string RomLoadPickerDir { get; set; } = Environment.CurrentDirectory;
		public string BiosLoadPickerDir { get; set; } = Environment.CurrentDirectory;
		public string StateLoadPickerDir { get; set; } = Environment.CurrentDirectory;
		public string StateSavePickerDir { get; set; } = Environment.CurrentDirectory;

		public bool LoadStateOnStart { get; set; } = false;
		public bool SaveStateOnExit { get; set; } = false;
		public float UIScale { get; set; } = 0.5f;

		[Serializable]
		public class LoggerState
		{
			public bool AutoScroll { get; set; } = false;
			public bool ReThrow { get; set; } = false;
		}

		public LoggerState Logger { get; set; } = new();
	}

	public class Gui : IUiElement
	{
		private Debugger m_debugger;
		private Renderer m_renderer;

		//UI
		private Logger m_logger = Logger.Instance;
		private RegisterWindow m_registers;
		private AssemblyWindow m_assembly; // main/central window
		private MemoryWindow m_memory;
		private GameWindow m_game;
		private SettingsWindow m_settings;

		private FilePicker m_romLoadPicker;
		private FilePicker m_biosLoadPicker;
		private FilePicker m_stateLoadPicker;
		private FilePicker m_stateSavePicker;

		private GuiState m_guiState = new();

		private bool m_showMetrics = false;
		private bool m_showStyleEditor = false;
		private bool m_showStackTool = false;
		private bool m_showDebugLog = false;

		public Gui(Debugger debugger, Renderer renderer)
		{
			m_debugger = debugger;
			m_renderer = renderer;

			LoadGuiState();

			m_logger.State = m_guiState.Logger;

			m_settings = new SettingsWindow( m_guiState );
			m_registers = new RegisterWindow( m_debugger );
			m_assembly = new AssemblyWindow( m_debugger );
			m_memory = new MemoryWindow();
			m_game = new GameWindow(m_debugger, m_renderer);

			// todo: load start folders from file
			m_romLoadPicker = new( onSelect: m_debugger.LoadRom, startFolder: m_guiState.RomLoadPickerDir, allowedExtensions: ".gb|.gbc");
			m_biosLoadPicker = new( onSelect: m_debugger.LoadBios, startFolder: m_guiState.RomLoadPickerDir, ".bin");
			m_stateLoadPicker = new( onSelect: m_debugger.LoadState, startFolder: m_guiState.StateLoadPickerDir );
			m_stateSavePicker = new( onSelect: m_debugger.SaveState, startFolder: m_guiState.StateSavePickerDir );
		}

		public void Init()
		{
			//Fonts.MonaspaceNeon.FontSize *= 2f;
			Logger.LogMsg("Welcome to rzrBoy Studio");

			if( m_guiState.LoadStateOnStart )
			{
				m_debugger.LoadState( m_guiState.StateSavePickerDir );
			}
		}

		public void Exit() 
		{
			if( m_guiState.SaveStateOnExit )
				m_debugger.SaveState( m_guiState.StateSavePickerDir );

			SaveGuiState();
		}

		private void LoadGuiState()
		{
			try
			{
				string json = File.ReadAllText( "guistate.json" );
				var state = JsonSerializer.Deserialize<GuiState>( json );
				if( state != null )
				{
					m_guiState = state;
				}
			}
			catch( System.Exception e )
			{
				Logger.LogException( e );
			}		
		}

		private void SaveGuiState()
		{
			try
			{
				JsonSerializerOptions options = new () { WriteIndented = true };
				string json = JsonSerializer.Serialize( m_guiState, options: options );
				File.WriteAllText( "guistate.json", json );
			}
			catch( System.Exception e )
			{
				Logger.LogException( e );
			}
		}

		private void Step()
		{
			try
			{
				m_debugger.Step();
			}
			catch( rzr.ExecException e )
			{
				m_logger.Log( e );
			}
		}

		private void Restart()
		{
			m_debugger.Restart();
		}

		// Update UI state
		public bool Update()
		{
			ImGui.DockSpaceOverViewport( ImGui.GetMainViewport() );
			Fonts.MonaspaceNeon.Push();

			MainMenu();
			
			if( m_romLoadPicker.Visible )
				m_romLoadPicker.Update();

			if( m_biosLoadPicker.Visible )
				m_biosLoadPicker.Update();

			if( m_stateLoadPicker.Visible )
				m_stateLoadPicker.Update();

			if( m_stateSavePicker.Visible )
				m_stateSavePicker.Update();

			if( ImGui.IsKeyPressed( ImGuiKey.LeftCtrl ) )
			{
				if( ImGui.IsKeyDown( ImGuiKey.S ) )
					m_debugger.LoadState( m_guiState.StateSavePickerDir );
			}

			m_settings.Update();
			m_registers.Update();
			m_assembly.Update();
			m_memory.Update();
			m_game.Update();
			m_logger.Update();

			if(m_showMetrics) ImGui.ShowMetricsWindow();
			if(m_showStyleEditor) ImGui.ShowStyleEditor();
			if(m_showStackTool) ImGui.ShowIDStackToolWindow();
			if(m_showDebugLog) ImGui.ShowDebugLogWindow();

			Fonts.Pop();

			return true;
		}

		public void MainMenu() 
		{
			if( ImGui.BeginMainMenuBar() )
			{
				if( ImGui.BeginMenu( "File" ) )
				{
					if( ImGui.Selectable( "Load ROM" ) )
						m_romLoadPicker.Visible = true;
					if( ImGui.Selectable( "Load Bios" ) )
						m_biosLoadPicker.Visible = true;
					if( ImGui.Selectable( "Load State" ) )
						m_stateLoadPicker.Visible = true;
					if( ImGui.Selectable( "Save State" ) )
						m_stateSavePicker.Visible = true;

					ImGui.EndMenu();
				}

				if( ImGui.BeginMenu( "Themes" ) )
				{
					if( ImGui.Selectable( "Dark" ) ) ImGui.StyleColorsDark();
					if( ImGui.Selectable( "Light" ) ) ImGui.StyleColorsLight();
					if( ImGui.Selectable( "Classic" ) ) ImGui.StyleColorsClassic();

					if( ImGui.Selectable( "+Size" ) )
					{
						ImGui.GetStyle().ScaleAllSizes( 1f + m_guiState.UIScale );
					}
					else if( ImGui.Selectable( "-Size" ) )
					{
						ImGui.GetStyle().ScaleAllSizes( 1f - m_guiState.UIScale );
					}

					ImGui.EndMenu();
				}

				if( ImGui.BeginMenu( "ImGui" ) )
				{
					if( ImGui.Selectable( "Metrics" ) ) m_showMetrics = !m_showMetrics;
					if( ImGui.Selectable( "StackTool" ) ) m_showStackTool = !m_showStackTool;
					if( ImGui.Selectable( "StyleEditor" ) ) m_showStyleEditor = !m_showStyleEditor;
					if( ImGui.Selectable( "DebugLog" ) ) m_showDebugLog = !m_showDebugLog;

					ImGui.EndMenu();
				}

				if( ImGui.BeginMenu( "View" ) )
				{
					if( ImGui.Selectable( "Load Preset" ) )
						ImGui.LoadIniSettingsFromDisk( "preset.ini" );
					if( ImGui.Selectable( "Save Preset" ) )
						ImGui.SaveIniSettingsToDisk( "preset.ini" );
					if( ImGui.Selectable( "Settings" ) )
						m_settings.Visible = true;
					if( ImGui.Selectable( "Registers" ) )
						m_registers.Visible = true;
					if( ImGui.Selectable( "Assembly" ) )
						m_assembly.Visible = true;
					if( ImGui.Selectable( "Memory" ) )
						m_memory.Visible = true;
					if( ImGui.Selectable( "Game" ) )
						m_game.Visible = true;
					if( ImGui.Selectable( "Logger" ) )
						m_logger.Visible = true;

					ImGui.EndMenu();
				}

				if( ImGui.BeginMenu( IconFonts.FontAwesome6.ArrowRightToBracket ) )
				{
					if( ImGui.IsItemClicked() )
						Step();

					ImGui.Text( "F11" );
					ImGui.EndMenu();
				}

				if( ImGui.IsKeyPressed( ImGuiKey.F11 ) ) Step();

				if( ImGui.BeginMenu( IconFonts.FontAwesome6.ArrowRotateRight ) )
				{
					if( ImGui.IsItemClicked() )
						Restart();

					ImGui.Text( "F12" );
					ImGui.EndMenu();
				}

				if( ImGui.IsKeyPressed( ImGuiKey.F12 ) ) Restart();

				ImGui.EndMainMenuBar();
			}
		}
	}
}