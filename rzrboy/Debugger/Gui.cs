using ImGuiNET;
using System;
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
		private FilePicker m_romLoadPicker;
		private FilePicker m_biosLoadPicker;

		private float m_scaleFactor = 0.5f;

		public Gui(Debugger debugger)
		{
			m_debugger = debugger;
			m_registers = new RegisterWindow(m_debugger.Emu);
			m_assembly = new AssemblyWindow();
			m_memory = new MemoryWindow();
			m_romLoadPicker = new( onSelect: m_debugger.LoadRom, startFolder: Environment.CurrentDirectory, allowedExtensions: ".gb|.gbc");
			m_biosLoadPicker = new( onSelect: m_debugger.LoadBios, startFolder: Environment.CurrentDirectory, ".bin");
		}

		private unsafe void LoadFonts() 
		{
			byte[] buffer = EmbeddedResource.Load( "monaspace-neon.ttf" );
			fixed( byte* p = buffer )
			{
				IntPtr ptr = (IntPtr)p;
				//var font = ImGui.GetIO().Fonts.AddFontFromMemoryTTF( ptr, buffer.Length, 20f );
				var font = ImGui.GetIO().Fonts.AddFontFromFileTTF( "C:\\Users\\razor\\Projects\\rzrboy\\rzrboy\\Debugger\\Fontsmonaspace-neon.ttf", 20f );
				//ImGui.GetIO().FontDefault
				ImGui.PushFont( font );
				// Do your stuff here
			}
		}

		public void Init()
		{
			//ImGui.GetIO().KeysDown.Data[(int)ImGuiKey.F10];
			//private uint m_dockSpaceId = 0;
			//m_dockSpaceId = ImGui.GetID("MyDockspace");
			//ImGui.DockSpace(m_dockSpaceId, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);
			ImGui.SetWindowFontScale(2);
			LoadFonts();
			Logger.Log("Welcome to rzrBoy Studio");
		}

		private void Step()
		{
			m_debugger.Emu.Step( debugPrint: true );
		}

		// Update UI state
		public bool Update()
		{
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
				//"⏮⏭⏵⏸⏹"
				if(ImGui.BeginMenu( "⏵" ) )
				{
					Step();
					ImGui.EndMenu();
				}

				if( ImGui.IsKeyPressed( ImGuiKey.F11 ) ) Step();

				m_viewMenu.Update();

                ImGui.EndMainMenuBar();
            }
			
			if( m_romLoadPicker.Visible )
				m_romLoadPicker.Update();

			if( m_biosLoadPicker.Visible )
				m_biosLoadPicker.Update();

			m_registers.Update();
			m_assembly.Update();
			m_memory.Update();
			m_logger.Update();

			return true;
		}
	}
}