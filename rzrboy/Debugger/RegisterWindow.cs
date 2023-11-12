namespace dbg.ui
{
	public class RegisterWindow : Window
	{
		private rzr.Boy m_emu;
		public RegisterWindow(rzr.Boy emu) : base(label: "Registers")
		{
			m_emu = emu;
		}

		protected override bool BodyFunc()
		{
			return true;
		}
	}
}