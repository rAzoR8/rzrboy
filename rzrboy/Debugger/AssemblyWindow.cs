using System.Numerics;
using ImGuiNET;

namespace dbg.ui
{
	public class AssemblyWindow : Window
	{
		private rzr.State m_state;
		public int Range { get; set; } = 10;

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
			public Instruction? Prev = null;
			public ushort PC;
			public rzr.State State;
			
			public ushort PCnext;
			public bool Update()
			{
				ushort pc = Prev?.PCnext ?? PC;
				ushort _pc = pc;
				
				rzr.AsmInstr instr = rzr.Asm.DisassembleInstr(ref pc, State.mem, unknownOp: rzr.UnknownOpHandling.AsDb);
	
				byte op0 = State.mem[_pc];
				string op1 = PC > _pc + 1 ? $"{State.mem[(ushort)( _pc + 1 )]:X2}" : "__";
				string op2 = PC > _pc + 2 ? $"{State.mem[(ushort)( _pc + 2 )]:X2}" : "__";

				bool isCur = _pc >= State.reg.PC && State.reg.PC < pc;
				
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

			int start = m_state.reg.PC - Range * 3; // 3byte per instr
			start = start < 0 ? 0 : start;
			
			// only do this on start up, just need to update the PC
			for (int i = 0; i < Range * 2; ++i)
			{
				Instruction? prev = i > 0 ? instructions[i-1] : null;
				Instruction instr = new() { Prev = prev, PC = (ushort)start, State = m_state };
				instructions.Add(instr);
			}

			return listBox.Update();
		}
	}
}