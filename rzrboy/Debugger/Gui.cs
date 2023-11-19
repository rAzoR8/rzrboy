using ImGuiNET;

namespace dbg.ui
{
	public class Gui : IUiElement
	{
		private Debugger m_debugger;

		//UI
		private Logger m_logger = Logger.Instance;
		private ViewMenu m_viewMenu = new();
		private RegisterWindow m_registers;
		private AssemblyWindow m_assembly; // main/central window
		private MemoryWindow m_memory;
		private FilePicker m_romLoadPicker;
		private FilePicker m_biosLoadPicker;

		private float m_scaleFactor = 0.5f;

		public Gui(Debugger debugger)
		{
			m_debugger = debugger;
			m_registers = new RegisterWindow(m_debugger.CurrentState);
			m_assembly = new AssemblyWindow(m_debugger.CurrentState);
			m_memory = new MemoryWindow();
			m_romLoadPicker = new( onSelect: m_debugger.LoadRom, startFolder: Environment.CurrentDirectory, allowedExtensions: ".gb|.gbc");
			m_biosLoadPicker = new( onSelect: m_debugger.LoadBios, startFolder: Environment.CurrentDirectory, ".bin");
		}

		public void Init()
		{
			//ImGui.SetWindowFontScale(2);
			Fonts.MonaspaceNeon.FontSize *= 2f;
			Logger.LogMsg("Welcome to rzrBoy Studio");
		}

		private void Step()
		{
			m_debugger.Step( );
		}

		// Update UI state
		public bool Update()
		{
			Fonts.MonaspaceNeon.Push();

			if( ImGui.BeginMainMenuBar())
            {
				if(ImGui.BeginMenu("File"))
				{
					if( ImGui.Selectable( "Load ROM" ))
						m_romLoadPicker.Visible = true;
					if( ImGui.Selectable( "Load Bios" ) )
						m_biosLoadPicker.Visible = true;
					ImGui.EndMenu();
				}

                if (ImGui.BeginMenu("Themes"))
                {
                    if (ImGui.Selectable("Dark")) ImGui.StyleColorsDark();
                    if (ImGui.Selectable("Light")) ImGui.StyleColorsLight();
                    if (ImGui.Selectable("Classic")) ImGui.StyleColorsClassic();

					if (ImGui.Selectable("+Size"))
					{
						ImGui.GetStyle().ScaleAllSizes(1f + m_scaleFactor);
					}
					else if(ImGui.Selectable("-Size"))
					{
						ImGui.GetStyle().ScaleAllSizes(1f - m_scaleFactor);
					}

                    ImGui.EndMenu();
                }

				m_viewMenu.Update();

				if(ImGui.BeginMenu( IconFonts.FontAwesome6.Play ) )
				{
					if(ImGui.IsItemClicked())
						Step();

					ImGui.Text("F11");
					ImGui.EndMenu();
				}

				if( ImGui.IsKeyPressed( ImGuiKey.F11 ) ) Step();

                ImGui.EndMainMenuBar();
            }
			
			if( m_romLoadPicker.Visible )
				m_romLoadPicker.Update();

			if( m_biosLoadPicker.Visible )
				m_biosLoadPicker.Update();

			m_registers.Update();
			m_assembly.SetState(m_debugger.CurrentState);
			m_assembly.Update();
			m_memory.Update();
			m_logger.Update();

			ImGui.ShowDemoWindow();

			Fonts.Pop();

			return true;
		}
	}
}