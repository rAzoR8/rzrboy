using ImGuiNET;

namespace dbg.ui
{
	public class SettingsWindow : Window
	{
		private GuiState m_state;
		public SettingsWindow( GuiState state  ) : base( "Settings" )
		{
			m_state = state;
		}

		protected override bool BodyFunc()
		{
			bool loadonstart = m_state.LoadStateOnStart;
			if(ImGui.Checkbox( "Load debugger state on start", ref loadonstart ))
				m_state.LoadStateOnStart = loadonstart;
			bool saveonexit = m_state.SaveStateOnExit;
			if( ImGui.Checkbox( "Save debugger state on exit", ref saveonexit ) )
				m_state.SaveStateOnExit = saveonexit;
			return true;
		}
	}
}
