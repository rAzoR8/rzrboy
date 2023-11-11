using ImGuiNET;
//using static ImGuiNET.ImGuiNative;

namespace dbg.ui
{
	public class Gui : IUiElement
	{
		private Debugger m_debugger;
		//private uint m_dockSpaceId = 0;
		private float m_scaleFactor = 0.5f;

		public Gui(Debugger debugger)
		{
			m_debugger = debugger;
		}

		
		public void Init()
		{
			//m_dockSpaceId = ImGui.GetID("MyDockspace");
			//ImGui.DockSpace(m_dockSpaceId, new Vector2(0, 0), ImGuiDockNodeFlags.PassthruCentralNode);
			ImGui.SetWindowFontScale(2);		
		}

		// Update UI state
		public void Update()
		{			
			// ImGui.Text("");
			// ImGui.Text(string.Empty);
			// ImGui.Text("Hello, world!");
			// ImGui.SliderFloat("float", ref m_f, 0, 1, m_f.ToString("0.000"));  

			// ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

			if (ImGui.BeginMainMenuBar())
            {
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
                
                ImGui.EndMainMenuBar();
            }
			
			// Test windows
            for (int i = 0; i < 4; i++)
            {
                // if (ImGui.Begin($"TestWindow{i}"))
                // {
                //     ImGui.Text($"This is window {i}");
                //     ImGui.End();
                // }

				var win = ImGuiScope.Window($"Window {i}");
				win.Body = new FuncUiElement(() => ImGui.Text($"This is window {i}"));
				win.Update();
            }
		}
	}
}