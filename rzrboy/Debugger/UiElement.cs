namespace dbg.ui
{
	public delegate void UpdateFn();

	public interface IUiElement
	{
		public void Update();
	}

	public class FuncUiElement : IUiElement
	{
		public UpdateFn Func {get;set;}
		public FuncUiElement(UpdateFn fn)
		{
			Func = fn;
		}

		public void Update()
		{
			if(Func != null)
				Func();
		}
	}

	public static class UiElementExtensions
	{
		public static FuncUiElement From(this UpdateFn fn) => new FuncUiElement(fn);
	}

	public class ImGuiScope : IUiElement
	{
		public delegate bool BeginFn(string label);
		public delegate void EndFn();

		private BeginFn m_begin;
		private EndFn m_end;

		private string m_label;

		public IUiElement? Body {get; set;}

		public ImGuiScope(BeginFn begin, EndFn end, string label)
		{
			m_begin = begin;
			m_end = end;
			m_label = label;
		}

		public void Update()
		{
			if(m_begin(m_label))
			{
				Body?.Update();
				m_end();
			}
		}

		public static ImGuiScope Window(string title) => new ImGuiScope(ImGuiNET.ImGui.Begin, ImGuiNET.ImGui.End, label: title);
	}
}