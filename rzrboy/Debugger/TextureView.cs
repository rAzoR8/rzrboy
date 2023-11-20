namespace dbg.ui
{
	public class TextureView : ImGuiScopeBase
	{
		public TextureView( BeginFn begin, EndFn end, string label ) : base( begin, end, label )
		{
			//https://github.com/ImGuiNET/ImGui.NET/issues/141
			//Veldrid.TextureDescription.Texture2D()
		}

		protected override bool BodyFunc()
		{
			throw new NotImplementedException();
		}
	}
}
