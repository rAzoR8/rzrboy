using System.Numerics;
using ImGuiNET;

namespace dbg.ui
{
	public class AssemblyWindow : Window
	{
		private rzr.State m_state;
		public int Range = 32;
		public int Offset = -1;

		public AssemblyWindow(rzr.State state) : base(label: "Assembly")
		{
			m_state = state;
		}

		public void SetState(rzr.State state)
		{
			m_state = state;
		}

		// bank idx -> sorted list of PCs we've seen
		private Dictionary<int, List<ushort>> m_bankPCs = new();

		protected override bool BodyFunc()
		{
			ImGui.SameLine();
			if( ImGui.Button("Clear Bank-Cache"))
				m_bankPCs.Clear();

			ImGui.SameLine();
			if( ImGui.Button( $"Goto Current 0x{m_state.curInstrPC}" ) )
				Offset = m_state.curInstrPC;

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

			if( !m_bankPCs.TryGetValue( m_state.mbc.SelectedRomBank, out var knownPCs ) ) // we haven't seen this bank yet
			{
				pc = m_state.curInstrPC;
				knownPCs = new();
				if( m_state.prevInstrPC < m_state.curInstrPC )
				{
					pc = m_state.prevInstrPC; // set start PC to prev if valid
					knownPCs.Add( m_state.prevInstrPC );
				}
				knownPCs.Add( m_state.curInstrPC );
				if( m_state.prevInstrPC > m_state.curInstrPC )
					knownPCs.Add( m_state.prevInstrPC );
				m_bankPCs.Add( m_state.mbc.SelectedRomBank, knownPCs );
			}
			else
			{
				int idx = knownPCs.BinarySearch( m_state.curInstrPC );
				if( idx < 0 ) // PC we havent seen yet for this bank
				{
					idx = ~idx;
					knownPCs.Insert( idx, m_state.curInstrPC );
				}

				// TODO: we can use BinarySearch to get the previous idx still in range
				pc = m_state.curInstrPC;
				for( int i = idx, r = 0; i > -1 && r < Range; ++r, --i )
				{
					ushort prev = knownPCs[i];
					if( ( m_state.curInstrPC - prev ) <= Range * 2 ) // assume average 2 bytes per instr
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
				rzr.ISection mem = m_state.mem;
				if( m_state.mem.Booting && pc >= m_state.mem.boot.Length )
					mem = m_state.mem.mbc;

				if( !mem.Accepts( pc ) )
					return false;

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

				bool isCur = _pc == m_state.curInstrPC;

				if( isCur ) ImGui.PushStyleColor( ImGuiCol.Text, new Vector4( 0.75f, 0, 0, 1 ) );
				ImGui.Selectable( $"[0x{_pc:X4}:0x{op0:X2}{op1}{op2}] {instr.ToString( _pc ).ToUpper()}" );
				if( isCur ) ImGui.PopStyleColor();
				return true;
			}

			if( Offset > -1 && Offset < 0x8000 )
				pc = (ushort)Offset;

			// draw instruction up until current PC
			while( pc < m_state.curInstrPC )
			{
				Element();
			}

			// set focus to instruction at current PC
			ImGui.SetItemDefaultFocus();

			// positive / progressive range forward from m_state.curInstrPC
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