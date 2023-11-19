using ImGuiNET;

namespace dbg.ui
{
	public class Logger : ImGuiScopeBase, rzr.ILogger
	{
		public static Logger Instance {get;} = new();

		public class Message : IUiElement
		{
			public string? What;
			public Action? Action;

			public bool Update()
			{
				if(What != null && ImGui.Selectable(What))
				{
					if(Action != null)
						Action();

					return true;
				}
				return false;
			}
		}

		private List<Message> m_messages = new();

		private ListBox m_listBox;

		// Default logger window
		public Logger(): base(ImGuiNET.ImGui.Begin, ImGuiNET.ImGui.End, label: "Logger")
		{
			m_listBox = new(label: "Messages", m_messages);
		}

		public Logger(BeginFn begin, EndFn end, string label) : base(begin, end, label)
		{
			m_listBox = new(label: "Messages", m_messages);
		}

		public static void LogMsg(string msg, Action? action = null)
		{
			Instance.m_messages.Add(new Message{What = msg, Action = action});
		}

		protected override bool BodyFunc()
		{
			return m_listBox.Update();
		}

		public void Log(string msg, Action? action = null)
		{
			m_messages.Add(new Message{What = msg, Action = action});
		}
	}
}