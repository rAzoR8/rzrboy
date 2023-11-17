using ImGuiNET;
//using static ImGuiNET.ImGuiNative;

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
		private float m_scaleFactor = 0.5f;

		public Gui(Debugger debugger)
		{
			m_debugger = debugger;
			m_registers = new RegisterWindow(m_debugger.Emu);
			m_assembly = new AssemblyWindow();
			m_memory = new MemoryWindow();
		}
		
		public void Init()
		{
			//private uint m_dockSpaceId = 0;
			//m_dockSpaceId = ImGui.GetID("MyDockspace");
			//ImGui.DockSpace(m_dockSpaceId, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);
			ImGui.SetWindowFontScale(2);
			Logger.Log("Welcome to rzrBoy Studio");
		}

		FilePicker? romLoadPicker = null;
		FilePicker? biosLoadPicker = null;

		// Update UI state
		public bool Update()
		{
			if( ImGui.BeginPopup( "popup" ) )
			{
				ImGui.Text( "POPUP" );
				ImGui.EndPopup();
			}

			if( ImGui.BeginMainMenuBar())
            {
				if(ImGui.BeginMenu("File"))
				{
					if( ImGui.Selectable( "Load ROM" ) && romLoadPicker == null)
					{
						romLoadPicker = FilePicker.GetFolderPicker( this, Path.Combine( Environment.CurrentDirectory ) );
					}
					else if( ImGui.Selectable( "Load Bios" ) && biosLoadPicker == null )
					{
						biosLoadPicker = FilePicker.GetFolderPicker( this, Path.Combine( Environment.CurrentDirectory ) );
					}
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
				// TODO: load rom/bios

                ImGui.EndMainMenuBar();
            }



			// modal must be on top level
			if( romLoadPicker != null )
			{
				romLoadPicker?.Update();
				//if(romLoadPicker.Update() && romLoadPicker.SelectedFile != null)
				//m_debugger.LoadRom( romLoadPicker.SelectedFile );
			}

			if( biosLoadPicker != null && biosLoadPicker.Update() && biosLoadPicker.SelectedFile != null )
				m_debugger.LoadBios( biosLoadPicker.SelectedFile );

			m_registers.Update();
			m_assembly.Update();
			m_memory.Update();
			m_logger.Update();

			return true;
		}
	}
}