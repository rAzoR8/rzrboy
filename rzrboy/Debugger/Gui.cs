using ImGuiNET;
//using static ImGuiNET.ImGuiNative;

namespace dbg
{
	public class Gui
	{
		private Debugger m_debugger;
		public Gui(Debugger debugger)
		{
			m_debugger = debugger;
		}

		private float m_f = 0;

		// Update UI state
		public void Update()
		{
			ImGui.Text("");
			ImGui.Text(string.Empty);
			ImGui.Text("Hello, world!");
			ImGui.SliderFloat("float", ref m_f, 0, 1, m_f.ToString("0.000"));  

			ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");
		}
	}
}