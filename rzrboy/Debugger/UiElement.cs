using ImGuiNET;

namespace dbg.ui
{
	public delegate bool UpdateFn();

	public interface IUiElement
	{
		public bool Update();
	}

	public class FuncUiElement : IUiElement
	{
		public UpdateFn Func {get;set;}
		public FuncUiElement(UpdateFn fn)
		{
			Func = fn;
		}

		public delegate void VoidFn();

		public FuncUiElement(VoidFn fn, bool updated = true)
		{
			Func = () => {fn(); return updated;};
		}

		public bool Update()
		{
			if(Func != null)
				return Func();
			return false;
		}
	}

	public static class UiElementExtensions
	{
		public static FuncUiElement From(this UpdateFn fn) => new FuncUiElement(fn);
		public static FuncUiElement From(this IEnumerable<IUiElement> elements) => new FuncUiElement( () => 
		{
			bool updated = false;
			foreach (var elem in elements) { updated |= elem.Update(); }
			return updated;
		});
	}

	public class ImGuiScope : IUiElement
	{
		public delegate bool BeginFn(string label);
		public delegate void EndFn();

		public BeginFn? Begin {get;set;}
		public EndFn? End{get;set;}
		public string Label {get;set;} = String.Empty;

		public IUiElement? Body {get; set;}

		public ImGuiScope()
		{
		}

		public ImGuiScope(string label)
		{
			Label = label;
		}

		public ImGuiScope(BeginFn begin, EndFn end, string label)
		{
			Begin = begin;
			End = end;
			Label = label;
		}

		public ImGuiScope(BeginFn begin, EndFn end, string label, IUiElement body)
		{
			Begin = begin;
			End = end;
			Label = label;
			Body = body;
		}

		public ImGuiScope(BeginFn begin, EndFn end, string label, UpdateFn body)
		{
			Begin = begin;
			End = end;
			Label = label;
			Body = new FuncUiElement(body);
		}

		public bool Update()
		{
			if( Begin!= null && Begin(Label))
			{
				Body?.Update();
				if(End != null)
					End();
				return true;
			}
			return false;
		}

		public static ImGuiScope Window(string title) => new ImGuiScope(ImGuiNET.ImGui.Begin, ImGuiNET.ImGui.End, label: title);
		public static ImGuiScope TabBar(string title) => new ImGuiScope(ImGuiNET.ImGui.BeginTabBar, ImGuiNET.ImGui.EndTabBar, label: title);
		public static ImGuiScope TabBarItem(string item) => new ImGuiScope(ImGuiNET.ImGui.BeginTabItem, ImGuiNET.ImGui.EndTabItem, label: item);
	}

	public abstract class ImGuiScopeBase : ImGuiScope
	{
		protected ImGuiScopeBase(string label) : base(label: label)
		{
			base.Begin = BeginFunc;
			base.End = EndFunc;
			base.Body = new FuncUiElement( BodyFunc );
		}

		protected ImGuiScopeBase(BeginFn begin, EndFn end, string label) : base(label: label)
		{
			base.Begin = begin;
			base.End = end;
			base.Body = new FuncUiElement( BodyFunc );
		}

		protected virtual bool BeginFunc(string label) {return false;}
		protected virtual void EndFunc() {}
		protected abstract bool BodyFunc();
	}

	public abstract class Window : ImGuiScopeBase
	{
		// TODO: generic settings
		public Window(string label) : base(label: label)
		{
		}

		protected override bool BeginFunc(string label)
		{
			//ImGuiWindowClass test2_flags;
			//test2_flags.DockNodeFlagsOverrideSet = ImGuiDockNodeFlags_CentralNode;
			//ImGui.SetNextWindowClass(test2_flags);
			return ImGuiNET.ImGui.Begin(label);
		}

		protected override void EndFunc()
		{
			ImGuiNET.ImGui.End();
		}
	}
}