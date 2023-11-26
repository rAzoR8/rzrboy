using System.Numerics;
using ImGuiNET;

namespace dbg.ui
{
	public class AssemblyWindow : Window
	{
		private Debugger m_dbg;		

		public int Range = 32;
		public int Offset = -1;

		public AssemblyWindow(Debugger dbg) : base(label: "Assembly")
		{
			m_dbg = dbg;
		}

		// bank idx -> sorted list of PCs we've seen
		private Dictionary<int, List<ushort>> m_bankPCs = new();

		protected override bool BodyFunc()
		{
			rzr.IEmuState state = m_dbg.CurrentState;

			ImGui.SameLine();
			if( ImGui.Button("Clear Bank-Cache"))
				m_bankPCs.Clear();

			ImGui.SameLine();
			if( ImGui.Button( $"Goto Current 0x{state.cpu.CurrentInstrPC}" ) )
				Offset = state.cpu.CurrentInstrPC;

			ImGui.SameLine();
			if( ImGui.Button( "Goto Auto" ) )
				Offset = -1;

			ImGui.PushItemWidth( 120 );
			ImGui.InputInt( "Goto Offset", ref Offset, 1, 16, ImGuiInputTextFlags.CharsHexadecimal );

			const int MaxRange = rzr.Mbc.RomBankSize / 3;
			ImGui.SameLine();
			ImGui.SliderInt( "Range", ref Range, 1, MaxRange );
			ImGui.PopItemWidth();
			
			if( !ImGui.BeginListBox( "Instructions", new Vector2( -1, -1 ) ))
				return false;

			ushort pc;

			if( !m_bankPCs.TryGetValue( state.rom.SelectedBank, out var knownPCs ) ) // we haven't seen this bank yet
			{
				pc = state.cpu.CurrentInstrPC;
				knownPCs = new(){state.cpu.CurrentInstrPC};
				m_bankPCs.Add( state.rom.SelectedBank, knownPCs );
			}
			else
			{
				int idx = knownPCs.BinarySearch( state.cpu.CurrentInstrPC );
				if( idx < 0 ) // PC we havent seen yet for this bank
				{
					idx = ~idx;
					knownPCs.Insert( idx, state.cpu.CurrentInstrPC );
				}

				// TODO: we can use BinarySearch to get the previous idx still in range
				pc = state.cpu.CurrentInstrPC;
				for( int i = idx, r = 0; i > -1 && r < Range; ++r, --i )
				{
					ushort prev = knownPCs[i];
					if( ( state.cpu.CurrentInstrPC - prev ) <= Range * 2 ) // assume average 2 bytes per instr
					{
						pc = prev;
					}
					else { break; }
				}
			}

			bool Element()
			{
				ushort _pc = pc;

				// in case the boot section is smaller than the preview, we read from the rom
				rzr.ISection mem = state.mem;

				//if( !mem.Accepts( pc ) )
				//	return false;

				rzr.AsmInstr instr;
				try
				{
					instr = rzr.Asm.DisassembleInstr( ref pc, mem, unknownOp: rzr.UnknownOpHandling.AsDb );
				}
				catch( rzr.Exception e )
				{
					Logger.LogException( e );
					return false;
				}

				byte op0 = mem[_pc];
				string op1 = pc > _pc + 1 ? $"{mem[(ushort)( _pc + 1 )]:X2}" : "__";
				string op2 = pc > _pc + 2 ? $"{mem[(ushort)( _pc + 2 )]:X2}" : "__";

				bool isCur = _pc == state.cpu.CurrentInstrPC;

				if( isCur ) ImGui.PushStyleColor( ImGuiCol.Text, new Vector4( 0.75f, 0, 0, 1 ) );
				ImGui.Selectable( $"[0x{_pc:X4}:0x{op0:X2}{op1}{op2}] {instr.ToString( _pc ).ToUpper()}" );
				if( isCur ) ImGui.PopStyleColor();
				return true;
			}

			if( Offset > -1 && Offset < 0x8000 )
				pc = (ushort)Offset;

			// draw instruction up until current PC
			while( pc < state.cpu.CurrentInstrPC )
			{
				Element();
			}

			// set focus to instruction at current PC
			ImGui.SetItemDefaultFocus();

			// positive / progressive range forward from state.curInstrPC
			for( int i = 0; i < Range; i++ )
			{
				if( !Element() ) return false;
			}

			// TODO: https://github.com/ocornut/imgui/issues/150
			// https://github.com/ocornut/imgui_club/blob/main/imgui_memory_editor/imgui_memory_editor.h

			ImGui.EndListBox();
			return true;
		}
	}
}