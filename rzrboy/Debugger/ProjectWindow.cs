using System.Security.Cryptography.X509Certificates;
using ImGuiNET;
using rzr;

namespace dbg.ui
{
	public class ProjectWindow : Window
	{
		private Debugger m_dbg;
		public string Folder {get; private set;} = "Project";

		public ProjectWindow(Debugger dbg) : base("Project")
		{
			m_dbg = dbg;
		}

		public bool Init(string folder, string? rom)
		{
			Folder = folder;
			
			try
			{
				string rev = Path.Combine(folder, $"rev{m_dbg.Provider.Revision}");
				Directory.CreateDirectory(rev);
				Folder = rev;
			}
			catch (System.Exception e)
			{
				Logger.LogException(e);
			}			

			return true;
		}

		protected override bool BodyFunc()
		{
			if(m_dbg.CurrentState.rom.Banks == 0)
				return false;

			var bank0 = m_dbg.CurrentState.rom.GetBank(0);
			HeaderView header = new ( bank0 );

			ushort headerProtionOfRomChecksum = HeaderView.ComputeRomChecksum(bank0.Take((int)HeaderOffsets.HeaderSize), subtractRomCheck: false);
			header.RomChecksum -= headerProtionOfRomChecksum;

			string title = header.Title;
			if(ImGui.InputText(label: "Name",  input: ref title, maxLength: (uint)HeaderOffsets.TitleLength ))
				header.Title = title;

			string manufacturer = header.Manufacturer;
			if(ImGui.InputText(label: "Manufacturer",  input: ref manufacturer, maxLength: (uint)HeaderOffsets.ManufacturerLength ))
				header.Manufacturer = manufacturer;

			int romBanks = header.RomBanks;
			ImGui.InputInt(label: "RomBanks", ref romBanks, step: 1, step_fast: 1, ImGuiInputTextFlags.None);
			if(romBanks!= header.RomBanks)
			{
				//header.RomBanks = romBanks;
				// TODO: resize roms
			}

			int ramBanks = header.RamBanks;
			ImGui.InputInt(label: "RamBanks", ref ramBanks, step: 1, step_fast: 1, ImGuiInputTextFlags.ReadOnly);
			if(ramBanks!= header.RamBanks)
			{
				//header.RamBanks = ramBanks;
				// TODO: resize rams
			}

			header.HeaderChecksum = HeaderView.ComputeHeaderChecksum(bank0);
			headerProtionOfRomChecksum = HeaderView.ComputeRomChecksum(bank0.Take((int)HeaderOffsets.HeaderSize), subtractRomCheck: false);
			header.RomChecksum += headerProtionOfRomChecksum;

			ImGui.Text($"Rom Checksum: 0x{header.RomChecksum:X4}");
			ImGui.Text($"Header Checksum: 0x{header.HeaderChecksum:X2}");

			return true;
		}
	}
}