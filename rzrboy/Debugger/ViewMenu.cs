using ImGuiNET;

namespace dbg.ui
{
	public class ViewMenu : ImGuiScopeBase
	{
		public ViewMenu() : base(ImGuiNET.ImGui.BeginMenu, ImGuiNET.ImGui.EndMenu, label: "View")
		{
		}

		protected override bool BodyFunc()
		{
			if(ImGui.Selectable("Load Preset"))
			{
				ImGui.LoadIniSettingsFromDisk("preset.ini");
				return true;
			}
			if(ImGui.Selectable("Save Preset"))
			{
				ImGui.SaveIniSettingsToDisk("preset.ini");
				return true;
			}
			return false;
		}
	}
}