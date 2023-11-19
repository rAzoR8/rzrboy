namespace dbg.ui
{
	public class RegisterWindow : Window
	{
		private rzr.State m_state;
		public RegisterWindow(rzr.State state) : base(label: "Registers")
		{
			m_state = state;
		}

		protected override bool BodyFunc()
		{
			return true;
		}
	}
}