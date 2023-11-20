namespace rzr
{
	/// <summary>
	///Bit 7 - Not used
	///Bit 6 - Not used
	///Bit 5 - P15 Select Action buttons(0=Select)
	///Bit 4 - P14 Select Direction buttons(0=Select)
	///Bit 3 - P13 Input: Down or Start(0=Pressed) (Read Only)
	///Bit 2 - P12 Input: Up or Select(0=Pressed) (Read Only)
	///Bit 1 - P11 Input: Left or B(0=Pressed) (Read Only)
	///Bit 0 - P10 Input: Right or A(0=Pressed) (Read Only)
	/// </summary>
	/// 
	public class Joypad : ByteSection
	{
		public Joypad( byte val = 0 ) : base( 0xFF00, val, "P1|JOYPAD" )
		{
		}

		// Action button selected
		public bool Action { get => m_value.IsBitSet( 5 ) == false; set => Binutil.SetBit( ref m_value, 5, !value ); }
		public bool Direction { get => m_value.IsBitSet( 4 ) == false; set => Binutil.SetBit( ref m_value, 4, !value ); }

		public bool Down { get => m_value.IsBitSet( 3 ) == false; set => Binutil.SetBit( ref m_value, 3, !value ); }
		public bool Up { get => m_value.IsBitSet( 2 ) == false; set => Binutil.SetBit( ref m_value, 2, !value ); }
		public bool Left { get => m_value.IsBitSet( 1 ) == false; set => Binutil.SetBit( ref m_value, 1, !value ); }
		public bool Right { get => m_value.IsBitSet( 0 ) == false; set => Binutil.SetBit( ref m_value, 0, !value ); }

		public bool Start => Down;
		public bool Select => Up;
		public bool B => Left;
		public bool A => Right;
	}
}
