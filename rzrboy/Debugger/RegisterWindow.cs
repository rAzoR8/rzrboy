using ImGuiNET;

namespace dbg.ui
{
	public class RegisterWindow : Window
	{
		private rzr.State m_state;
		public RegisterWindow(rzr.State state) : base(label: "Registers")
		{
			m_state = state;
		}

		protected override bool BodyFunc()
		{
			var reg = m_state.reg;
			ImGui.Text($"Halted: {reg.Halted}");
			
			ImGui.Text($"A 0x{reg.A:x2}{reg.F:x2} F");
			ImGui.Text($"B 0x{reg.B:x2}{reg.C:x2} C");
			ImGui.Text($"D 0x{reg.D:x2}{reg.E:x2} E");
			ImGui.Text($"H 0x{reg.H:x2}{reg.L:x2} L");
			ImGui.Text($"SP 0x{reg.SP:x4}");
			ImGui.Text($"PC 0x{reg.PC:x4}");
			ImGui.Text($"Z {reg.Zero} Zero");
			ImGui.Text($"N {reg.Sub} Sub"); 
			ImGui.Text($"H {reg.HalfCarry} Half Carry"); 
			ImGui.Text($"C {reg.Carry} Carry"); 
			
			ImGui.Text($"IME {reg.IME}");
			return true;
		}
	}
}