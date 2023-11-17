using ImGuiNET;

namespace dbg.ui
{
	// https://github.com/mellinoe/synthapp/blob/master/src/synthapp/Widgets/FilePicker.cs
	// https://gist.github.com/prime31/91d1582624eb2635395417393018016e
	public class FilePicker : ImGuiScopeBase
	{
		public string CurrentFolder { get; private set; }
		public string? SelectedFile { get; private set; }
		public bool FoldersOnly { get; }
		public List<string>? AllowedExtensions { get; }

		private System.Numerics.Vector4 AccentColor = new( 0f, 0.75f, 0.75f, 1f );
		public bool WantsToClose = false;

		//protected override bool BeginFunc( string label )
		//{
		//	bool open = ImGui.BeginPopup( label );
		//	open &= ImGui.BeginPopupModal( label, ref m_isOpen );
		//	return open;
		//}

		public FilePicker(string startFolder, string allowedExtensions) : base( begin: ImGui.Begin, end: ImGui.End, label: "filer-picker" )
		{
			CurrentFolder = startFolder;
			FoldersOnly = false;
			AllowedExtensions = allowedExtensions.Split('|').ToList();
		}

		public FilePicker( string startFolder ) : base( begin: ImGui.Begin, end: ImGui.End, label: "filer-picker" )
		{
			FoldersOnly = true;
			CurrentFolder = startFolder;
		}

		protected override bool BodyFunc()
		{
			ImGui.Text(CurrentFolder);
			bool result = false;

			if (ImGui.BeginChildFrame(1, new System.Numerics.Vector2(400, 400)))
			{
				var di = new DirectoryInfo(CurrentFolder);
				if (di.Exists)
				{
					if (di.Parent != null)
					{
						ImGui.PushStyleColor(ImGuiCol.Text, AccentColor);
						if (ImGui.Selectable("../", false, ImGuiSelectableFlags.DontClosePopups))
							CurrentFolder = di.Parent.FullName;

						ImGui.PopStyleColor();
					}

					var fileSystemEntries = GetFileSystemEntries(di.FullName);
					foreach (var fse in fileSystemEntries)
					{
						if (Directory.Exists(fse))
						{
							var name = Path.GetFileName(fse);
							ImGui.PushStyleColor(ImGuiCol.Text, AccentColor);
							if (ImGui.Selectable(name + "/", false, ImGuiSelectableFlags.DontClosePopups))
								CurrentFolder = fse;
							ImGui.PopStyleColor();
						}
						else
						{
							var name = Path.GetFileName(fse);
							bool isSelected = SelectedFile == fse;
							if (ImGui.Selectable(name, isSelected, ImGuiSelectableFlags.DontClosePopups))
								SelectedFile = fse;

							if (ImGui.IsMouseDoubleClicked(0))
							{
								result = true;
								ImGui.CloseCurrentPopup();
							}
						}
					}
				}
			}
			ImGui.EndChildFrame();

			if (ImGui.Button("Cancel"))
			{
				result = false;
				WantsToClose = true;
			}

			ImGui.SameLine();
			if( ImGui.Button( "Open" ) )
			{
				result = true;
				SelectedFile = FoldersOnly ? CurrentFolder : SelectedFile;
				WantsToClose = true;
				//ImGui.CloseCurrentPopup();
			}

			return result;
		}

		private List<string> GetFileSystemEntries(string fullName)
		{
			var files = new List<string>();
			var dirs = new List<string>();

			foreach (var fse in Directory.GetFileSystemEntries(fullName, ""))
			{
				if (Directory.Exists(fse))
				{
					dirs.Add(fse);
				}
				else if (!FoldersOnly)
				{
					if (AllowedExtensions != null)
					{
						var ext = Path.GetExtension(fse);
						if (AllowedExtensions.Contains(ext))
							files.Add(fse);
					}
					else
					{
						files.Add(fse);
					}
				}
			}

			var ret = new List<string>(dirs);
			ret.AddRange(files);

			return ret;
		}
	}
}