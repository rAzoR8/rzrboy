using ImGuiNET;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace dbg.ui
{
	public class Logger : ImGuiScopeBase, rzr.ILogger
	{
		public static Logger Instance {get;} = new();
		
		public class Message : IUiElement
		{
			public int Count = 0;
			public string? What;
			public Action? Action;

			public bool Update()
			{
				if(What != null && ImGui.Selectable(Count == 0 ? What : $"({Count}){What}"))
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
		private bool m_throw = false;
		private bool m_autoScroll = false;

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
			Instance.Log(msg, action);
		}

		public static void LogException( rzr.Exception e )
		{
			Instance.Log( e );
		}

		protected override bool BodyFunc()
		{
			ImGui.Checkbox( "Re-Throw Exceptions", ref m_throw );
			ImGui.SameLine();
			if( ImGui.Checkbox( "Auto Scroll", ref m_autoScroll ) )
				m_listBox.ScrollToEnd = m_autoScroll;

			var ret = m_listBox.Update();
			return ret;
		}

		public void Log( rzr.Exception e )
		{
			Log( e.Message );
			var stack = e.StackTrace?.Clone();
			if( m_throw ) ExceptionDispatchInfo.Capture( e ).Throw(); ;
		}

		public void Log(string msg, Action? action = null)
		{
			if( m_messages.Count > 0 && m_messages.Last().What == msg )
			{
				m_messages.Last().Count++;
				return;
			}
			Debug.WriteLine( msg );
			m_messages.Add(new Message{What = msg, Action = action});
		}
	}
}