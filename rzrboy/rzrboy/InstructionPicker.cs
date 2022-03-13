using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using static rzr.ExtensionMethods;

namespace rzrboy
{
	public class InstructionPicker : StackLayout
	{
		private Picker m_InstrPicker = new Picker { ItemsSource = new List<rzr.InstrType>(rzr.InstrType.Db.EnumValues()) }; // fixed list picking
		private Button m_Lhs = new() { Text = "Lhs" };
		private Button m_Rhs = new() { Text = "Rhs" };

		private HorizontalStackLayout m_Buttons;

		private Entry m_Entry = new(); // value / number picking

		public InstructionPicker()
		{
			m_Buttons = new HorizontalStackLayout { m_InstrPicker, m_Lhs, m_Rhs };

			Children.Add( m_Buttons );

			m_Lhs.Clicked += OnLhsButtonPressed;
			m_Rhs.Clicked += OnRhsButtonPressed;
		}

		void OnInstrButtonPressed( object sender, EventArgs args )
		{
			//m_Picker.ItemsSource = 
		}

		void OnLhsButtonPressed( object sender, EventArgs args )
		{
		}

		void OnRhsButtonPressed( object sender, EventArgs args )
		{
		}

	}
}
