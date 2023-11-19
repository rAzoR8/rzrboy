using System.Numerics;
using ImGuiNET;

namespace dbg.ui
{
	public class AssemblyWindow : Window
	{
		private rzr.State m_state;
		public int Range { get; set; } = 32;

		public AssemblyWindow(rzr.State state) : base(label: "Assembly")
		{
			m_state = state;
		}

		public void SetState(rzr.State state)
		{
			m_state = state;
		}

		private class Instruction : IUiElement
		{
			public ushort PC = 0xFFFF;
			public Instruction? Prev = null;
			public rzr.State State;

			public Instruction( Instruction prev, rzr.State state )
			{
				Prev = prev;
				State = state;
			}

			public Instruction( ushort pc, rzr.State state )
			{
				PC = pc;
				State = state;
			}

			public ushort PCnext;
			public bool Update()
			{
				ushort pc = Prev?.PCnext ?? PC;
				ushort _pc = pc;

				rzr.ISection mem = State.mem;
				if( State.mem.Booting && pc >= State.mem.boot.Length )
					mem = State.mem.mbc;

				rzr.AsmInstr instr;
				try
				{
					instr = rzr.Asm.DisassembleInstr(ref pc, mem, unknownOp: rzr.UnknownOpHandling.AsDb);
				}
				catch( rzr.Exception e )
				{
					Logger.LogException( e );
					return false;
				}
	
				byte op0 = mem[_pc];
				string op1 = pc > _pc + 1 ? $"{mem[(ushort)( _pc + 1 )]:X2}" : "__";
				string op2 = pc > _pc + 2 ? $"{mem[(ushort)( _pc + 2 )]:X2}" : "__";

				bool isCur = _pc == State.curInstrPC;
				
				if(isCur) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.75f, 0,0, 1));
				ImGui.Selectable( $"[0x{_pc:X4}:0x{op0:X2}{op1}{op2}] {instr.ToString( _pc ).ToUpper()}" );
				if(isCur) ImGui.PopStyleColor();
				
				PCnext = pc;
				return true;
			}
		}

		protected override bool BodyFunc()
		{
			List<Instruction> instructions = new();
			ListBox listBox = new("Instructions", instructions);

			// only do this on start up, just need to update the PC
			instructions.Add( new( m_state.curInstrPC, m_state ) );
			for (int i = 1; i < Range; ++i)
			{
				instructions.Add(new(prev: instructions[i - 1], m_state));
			}

			return listBox.Update();
		}
	}
}