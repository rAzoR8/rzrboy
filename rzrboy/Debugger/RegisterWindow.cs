using ImGuiNET;
using rzr;

namespace dbg.ui
{
	public class RegisterWindow : Window
	{
		private rzr.State m_state;
		public RegisterWindow( rzr.State state ) : base(label: "Registers")
		{
			Scale = 1f;
			m_state = state;
		}

		public void SetState( rzr.State state )
		{
			m_state = state;
		}

		protected override bool BodyFunc()
		{
			var reg = m_state.reg;
			var IO = m_state.mem.io;
			var mem = m_state.mem;

			ImGui.Text($"Halted: {reg.Halted} Booting: {mem.Booting}" );

			ImGui.Text($"A {reg.A:X2}{reg.F:X2} F | Z {reg.F.GetBit(7)} Zero");
			ImGui.Text($"B {reg.B:X2}{reg.C:X2} C | N {reg.F.GetBit(6)} Sub" );
			ImGui.Text($"D {reg.D:X2}{reg.E:X2} E | H {reg.F.GetBit(5)} Half" );
			ImGui.Text($"H {reg.H:X2}{reg.L:X2} L | C {reg.F.GetBit(4)} Carry" );
			ImGui.Text($"SP {reg.SP:X4} PC {reg.PC:X4}" );

			ImGui.Text($"IE {mem.IE.Value}\tIME {reg.IME}" );

			ImGui.Separator();

			for( ushort i = IO.StartAddr; i < IO.StartAddr + IO.Length/2; )
			{
				ImGui.Text( $"{i:X4}: {IO[i++]} | {i:X4}: {IO[i++]}" );
			}

			return true;
		}
	}
}