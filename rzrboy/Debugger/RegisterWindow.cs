namespace dbg.ui
{
	public class RegisterWindow : Window
	{
		private rzr.Emu m_emu;
		public RegisterWindow(rzr.Emu emu) : base(label: "Registers")
		{
			m_emu = emu;
		}

		protected override bool BodyFunc()
		{
			return true;
		}
	}
}