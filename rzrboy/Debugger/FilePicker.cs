using ImGuiNET;

namespace dbg.ui
{
	// https://github.com/mellinoe/synthapp/blob/master/src/synthapp/Widgets/FilePicker.cs
	// https://gist.github.com/prime31/91d1582624eb2635395417393018016e
	public class FilePicker : ImGuiScopeBase
	{
		static readonly Dictionary<object, FilePicker> _filePickers = new Dictionary<object, FilePicker>();

		public static FilePicker GetFilePicker(object o, string startingPath, string searchFilter = null, bool onlyAllowFolders = false)
		{
			if (File.Exists(startingPath))
			{
				startingPath = new FileInfo(startingPath).DirectoryName;
			}
			else if (string.IsNullOrEmpty(startingPath) || !Directory.Exists(startingPath))
			{
				startingPath = Environment.CurrentDirectory;
				if (string.IsNullOrEmpty(startingPath))
					startingPath = AppContext.BaseDirectory;
			}

			if (!_filePickers.TryGetValue(o, out FilePicker fp))
			{
				fp = new FilePicker();
				fp.RootFolder = startingPath;
				fp.CurrentFolder = startingPath;
				fp.OnlyAllowFolders = onlyAllowFolders;

				if (searchFilter != null)
				{
					if (fp.AllowedExtensions != null)
						fp.AllowedExtensions.Clear();
					else
						fp.AllowedExtensions = new List<string>();

					fp.AllowedExtensions.AddRange(searchFilter.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries));
				}

				_filePickers.Add(o, fp);
			}

			return fp;
		}

		public static void RemoveFilePicker(object o) => _filePickers.Remove(o);
		public static FilePicker GetFolderPicker(object o, string startingPath)
				=> GetFilePicker(o, startingPath, null, true);

		private string m_path;

		public string RootFolder;
		public string CurrentFolder;
		public string SelectedFile;
		public List<string> AllowedExtensions;
		public bool OnlyAllowFolders;

		private bool m_isOpen = true;

		protected override bool BeginFunc( string label )
		{
			//bool open = ImGui.BeginPopup( label );
			//open &=  ImGui.BeginPopupModal( label, ref m_isOpen );
			//return open;
			return false;
		}

		//public FilePicker() : base( begin: ImGui.BeginPopupModal, end: ImGui.EndPopup, label: "filer-picker")
		//{
		//	//m_path = path;
		//}

		public FilePicker() : base( begin: ImGui.Begin, end: ImGui.End, label: "filer-picker" )
		{
			//m_path = path;
		}

		public System.Numerics.Vector4 AccentColor = new(0f, 0.75f, 0.75f, 1f);

		protected override bool BodyFunc()
		{
			ImGui.Text("Current Folder: " + Path.GetFileName(RootFolder) + CurrentFolder.Replace(RootFolder, ""));
			bool result = false;

			if (ImGui.BeginChildFrame(1, new System.Numerics.Vector2(400, 400)))
			{
				var di = new DirectoryInfo(CurrentFolder);
				if (di.Exists)
				{
					if (di.Parent != null && CurrentFolder != RootFolder)
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
				ImGui.CloseCurrentPopup();
			}

			if (OnlyAllowFolders)
			{
				ImGui.SameLine();
				if (ImGui.Button("Open"))
				{
					result = true;
					SelectedFile = CurrentFolder;
					ImGui.CloseCurrentPopup();
				}
			}
			else if (SelectedFile != null)
			{
				ImGui.SameLine();
				if (ImGui.Button("Open"))
				{
					result = true;
					ImGui.CloseCurrentPopup();
				}
			}

			return result;
		}

		private bool TryGetFileInfo(string fileName, out FileInfo realFile)
		{
			try
			{
				realFile = new FileInfo(fileName);
				return true;
			}
			catch
			{
				realFile = null;
				return false;
			}
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
				else if (!OnlyAllowFolders)
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