using ImGuiNET;
using rzr;
using static rzr.HeaderView;

namespace dbg.ui
{
	public class ProjectWindow : Window
	{
		private Debugger m_dbg;
		private static readonly EnumSelectable<CartridgeType>[] CartTypes = EnumSelectable<CartridgeType>.Get().ToArray();
		private static readonly EnumSelectable<CGBFlag>[] CGBFlags = EnumSelectable<CGBFlag>.GetSorted().ToArray();

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

			int version = header.Version;
			ImGui.InputInt( label: "Version", ref version, step: 1, step_fast: 1, ImGuiInputTextFlags.None );
			if( version != header.Version && version <= byte.MaxValue && version > -1 )
			{
				header.Version = (byte)version;
			}

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

			{
				CartridgeType type = header.Type;
				ComboBox cartType = new( "MBC Type", CartTypes );
				cartType.Selected = CartTypes.Where( e => e.Value == type ).First();

				if( cartType.Update() )
				{
					if( cartType.Selected is EnumSelectable<CartridgeType> s && s.Value != type )
					{
						// TODO: figure out why this does not write through to the rom
						header.Type = s.Value;

						// TODO: abstract! own window???
						if( m_dbg.CurrentState is rzr.State state )
						{
							state.LoadRom( state.mbc.Rom.Save() );
						}
					}
				}
			}

			{
				byte cgb = header.CGBSupport;
				ComboBox cgbMode = new( "CGB Support", CGBFlags );
				if( ( cgb & (byte)CGBFlag.PGB1 ) == (byte)CGBFlag.PGB1 )
					cgbMode.Selected = CGBFlags[2];
				else if( ( cgb & (byte)CGBFlag.PGB2 ) == (byte)CGBFlag.PGB2 )
					cgbMode.Selected = CGBFlags[3];
				else if( cgb == (byte)CGBFlag.CGBMonochrome )
					cgbMode.Selected = CGBFlags[0];
				else if( cgb == (byte)CGBFlag.CGBOnly )
					cgbMode.Selected = CGBFlags[1];

				if( cgbMode.Update() )
				{
					if( cgbMode.Selected is EnumSelectable<CGBFlag> s && (byte)s.Value != cgb )
					{
						header.CGBSupport = (byte)s.Value;
					}
				}
			}

			bool japan = header.Japan;
			if( ImGui.Checkbox( "Japan", ref japan ) )
				header.Japan = japan;

			ImGui.SameLine();
			bool sgbSupport = header.SGBSupport;
			if(ImGui.Checkbox("SGB Support", ref sgbSupport ) )
				header.SGBSupport = sgbSupport;

			header.HeaderChecksum = HeaderView.ComputeHeaderChecksum(bank0);
			headerProtionOfRomChecksum = HeaderView.ComputeRomChecksum(bank0.Take((int)HeaderOffsets.HeaderSize), subtractRomCheck: false);
			header.RomChecksum += headerProtionOfRomChecksum;

			ImGui.Text($"Rom Checksum: 0x{header.RomChecksum:X4}");
			ImGui.Text($"Header Checksum: 0x{header.HeaderChecksum:X2}");

			return true;
		}
	}
}