using ImGuiNET;

namespace dbg.ui
{
	public class GameWindow : Window
	{
		private TextureElement m_framebuffer;
		private Debugger m_dbg;
		private Renderer m_rend;

		private float m_scale = 2f;

		public GameWindow(Debugger dbg, Renderer rend) : base("Game")
		{
			m_dbg = dbg;
			m_rend = rend;
			dbg.StateChanged += OnStateChanged;
			m_framebuffer = new(dbg.CurrentState.pix.FrameBuffer, rend, "Preview");
		}

		private void OnStateChanged(rzr.IEmuState? oldState, rzr.IEmuState newState)
		{
			m_framebuffer = new(newState.pix.FrameBuffer, m_rend, "Preview");
		}

		protected override bool BodyFunc()
		{
			ImGui.SliderFloat("Scale", ref m_scale, 1f, 8f);
			m_framebuffer.Width = m_framebuffer.Height = m_scale;
			m_framebuffer.Update();
			return true;
		}
	}
}