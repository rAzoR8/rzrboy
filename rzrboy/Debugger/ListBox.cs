using System.Numerics;

namespace dbg.ui
{
	public class ListBox : ImGuiScopeBase
	{
		public IEnumerable<IUiElement>? Elements{get; set;}
		public int SelectedIndex {get; private set;} = -1;
		public IUiElement? Selected {get; private set;} = null;

		protected override bool BeginFunc(string label)
		{
			return ImGuiNET.ImGui.BeginListBox(label, new Vector2(-1, -1));
		}

		public ListBox(string label, IEnumerable<IUiElement>? elements = null) : base( ImGuiNET.ImGui.EndListBox, label: label)
		{
			Elements = elements;
		}

		protected override bool BodyFunc()
		{
			bool updated = false;

			if(Elements == null)
				return updated;

			int i = 0;
			foreach (var elem in Elements)
			{
				if(elem.Update())
				{
					SelectedIndex = i;
					Selected = elem;
					updated = true;
					ImGuiNET.ImGui.SetItemDefaultFocus();
				}
				++i;
			}

			return updated;
		}
	}
}