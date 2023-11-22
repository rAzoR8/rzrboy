using ImGuiNET;
using System.Text.Json;

namespace dbg.ui
{
	[Serializable]
	public class GuiState
	{
		public string RomLoadPickerDir = Environment.CurrentDirectory;
		public string BiosLoadPickerDir = Environment.CurrentDirectory;
		public string StateLoadPickerDir = Environment.CurrentDirectory;
		public string StateSavePickerDir = Environment.CurrentDirectory;
	}

	public class Gui : IUiElement
	{
		private Debugger m_debugger;
		private Renderer m_renderer;

		//UI
		private Logger m_logger = Logger.Instance;
		private ViewMenu m_viewMenu = new();
		private RegisterWindow m_registers;
		private AssemblyWindow m_assembly; // main/central window
		private MemoryWindow m_memory;
		private GameWindow m_game;

		private FilePicker m_romLoadPicker;
		private FilePicker m_biosLoadPicker;
		private FilePicker m_stateLoadPicker;
		private FilePicker m_stateSavePicker;

		private GuiState m_guiState = new();

		private float m_scaleFactor = 0.5f;

		private bool m_showMetrics = false;
		private bool m_showStyleEditor = false;
		private bool m_showStackTool = false;
		private bool m_showDebugLog = false;

		public Gui(Debugger debugger, Renderer renderer)
		{
			m_debugger = debugger;
			m_renderer = renderer;

			m_registers = new RegisterWindow( m_debugger );
			m_assembly = new AssemblyWindow( m_debugger );
			m_memory = new MemoryWindow();
			m_game = new GameWindow(m_debugger, m_renderer);

			LoadGuiState();

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
		}

		private void LoadGuiState()
		{
			File.ReadAllTextAsync( "guistate.json" ).ContinueWith( (json) =>
			{
				var state = JsonSerializer.Deserialize<GuiState>( json.Result );
				if( state != null )
				{
					m_guiState = state;
				}
			} );			
		}

		public void SaveGuiState()
		{
			File.WriteAllText("guistate.json", JsonSerializer.Serialize(m_guiState));
		}

		private void Step()
		{
			m_debugger.Step();
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
					ImGui.EndMenu();
				}

				if( ImGui.BeginMenu( "Themes" ) )
				{
					if( ImGui.Selectable( "Dark" ) ) ImGui.StyleColorsDark();
					if( ImGui.Selectable( "Light" ) ) ImGui.StyleColorsLight();
					if( ImGui.Selectable( "Classic" ) ) ImGui.StyleColorsClassic();

					if( ImGui.Selectable( "+Size" ) )
					{
						ImGui.GetStyle().ScaleAllSizes( 1f + m_scaleFactor );
					}
					else if( ImGui.Selectable( "-Size" ) )
					{
						ImGui.GetStyle().ScaleAllSizes( 1f - m_scaleFactor );
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

				m_viewMenu.Update();

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