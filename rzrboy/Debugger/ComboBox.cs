namespace dbg.ui
{
	public class EnumSelectable<T> : IUiElement where T : struct, System.Enum
	{
		public T Value { get; set; } = default(T);
		public bool Update()
		{
			return ImGuiNET.ImGui.Selectable( System.Enum.GetName<T>( Value ) );
		}

		public override string ToString() => Value.ToString();

		public static IEnumerable<EnumSelectable<T>> Get()
		{
			return Enum.GetValues<T>().Select( e => new EnumSelectable<T> { Value = e } );
		}
	}

	public class ComboBox : ImGuiScopeBase
	{
		public IEnumerable<IUiElement>? Elements { get; set; } // Elements need to call ImGui.Selectable
		public int SelectedIndex { get; private set; } = -1;
		public IUiElement? Selected { get; set; } = null;

		protected override bool BeginFunc( string label )
		{
			return ImGuiNET.ImGui.BeginCombo( label, Selected?.ToString() ?? Elements?.First().ToString() ?? String.Empty );
		}

		public ComboBox( string label, IEnumerable<IUiElement>? elements = null ) : base( ImGuiNET.ImGui.EndCombo, label: label )
		{
			Elements = elements;
		}

		protected override bool BodyFunc()
		{
			bool updated = false;

			if( Elements == null )
				return updated;

			int i = 0;
			foreach( var elem in Elements )
			{
				if(Selected == elem) SelectedIndex = i;

				if( elem.Update() )
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