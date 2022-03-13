using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using static rzr.ExtensionMethods;

namespace rzrboy
{
	public class InstructionPicker : StackLayout
	{
		private Button m_Instruction = new() { Text = "Instr" };
		private Button m_Lhs = new() { Text = "Lhs" };
		private Button m_Rhs = new() { Text = "Rhs" };

		private HorizontalStackLayout m_Buttons;

		private Picker m_Picker = new(); // fixed list picking
		private Entry m_Entry = new(); // value / number picking

		private HorizontalStackLayout m_ValueSelector;

		private List<rzr.InstrType> m_InstrTypes = new ( rzr.InstrType.Nop.EnumValues( rzr.InstrType.Jr ) );

		public InstructionPicker()
		{
			m_Buttons = new HorizontalStackLayout { m_Instruction, m_Lhs, m_Rhs };
			m_ValueSelector = new HorizontalStackLayout { m_Picker, m_Entry };

			Children.Add( m_Buttons );
			Children.Add( m_ValueSelector );

			m_Instruction.Clicked += OnInstrButtonPressed;
			m_Lhs.Clicked += OnLhsButtonPressed;
			m_Rhs.Clicked += OnRhsButtonPressed;

			m_Picker.ItemsSource = m_InstrTypes;
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
