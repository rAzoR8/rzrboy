using ImGuiNET;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace dbg.ui
{
	public class Logger : Window, rzr.ILogger
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

		public GuiState.LoggerState? State { get; set; } = new();

		// Default logger window
		public Logger(): base( label: "Logger")
		{
			m_listBox = new(label: "Messages", m_messages);
		}

		public static void LogMsg(string msg, Action? action = null)
		{
			Instance.Log(msg, action);
		}

		public static void LogException( System.Exception e )
		{
			Instance.Log( e );
		}

		protected override bool BodyFunc()
		{
			if( State != null )
			{
				bool rethrow = State.ReThrow;
				bool autoscroll = State.AutoScroll;
				if( ImGui.Checkbox( "Re-Throw Exceptions", ref rethrow ) )
					State.ReThrow = rethrow;
				ImGui.SameLine();
				if( ImGui.Checkbox( "Auto Scroll", ref autoscroll ) )
					m_listBox.ScrollToEnd = State.AutoScroll = autoscroll;
			}

			var ret = m_listBox.Update();
			return ret;
		}

		public void Log( System.Exception e )
		{
			Log( e.Message );
			var stack = e.StackTrace?.Clone();
			if( State?.ReThrow ?? false ) ExceptionDispatchInfo.Capture( e ).Throw(); ;
		}

		public void Log( string msg )
		{
			Log( msg, action: null );
		}

		public void Log(string msg, Action? action)
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