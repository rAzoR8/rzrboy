using ImGuiNET;
using rzr;

namespace dbg.ui
{
	public class RegisterWindow : Window
	{
		private Debugger m_dbg;
		public RegisterWindow( Debugger dbg ) : base(label: "Registers")
		{
			Scale = 1f;
			m_dbg = dbg;
		}

		protected override bool BodyFunc()
		{
			var reg = m_dbg.CurrentState.reg;
			var mem = m_dbg.CurrentState.mem;

			ImGui.Text($"Halted: {reg.Halted} Booting: {mem[0xFF50]}" );

			ImGui.Text($"A {reg.A:X2}{reg.F:X2} F | Z {reg.F.GetBit(7)} Zero");
			ImGui.Text($"B {reg.B:X2}{reg.C:X2} C | N {reg.F.GetBit(6)} Sub" );
			ImGui.Text($"D {reg.D:X2}{reg.E:X2} E | H {reg.F.GetBit(5)} Half" );
			ImGui.Text($"H {reg.H:X2}{reg.L:X2} L | C {reg.F.GetBit(4)} Carry" );
			ImGui.Text($"SP {reg.SP:X4} PC {reg.PC:X4}" );

			ImGui.Text($"IE {mem[0xFFFF]}\tIME {reg.IME}" );

			ImGui.Separator();

			for( ushort i = 0xFF00; i < 0xFF00 + 0x40; )
			{
				ImGui.Text( $"{i:X4}: {mem[i++]} | {i:X4}: {mem[i++]}" );
			}

			return true;
		}
	}
}