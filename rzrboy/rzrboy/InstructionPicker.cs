using Microsoft.Maui.Controls;

using System;
using System.Collections.Generic;
using static rzr.ExtensionMethods;

namespace rzrboy
{
	public class InstructionPicker : Grid
	{
		public enum Row { Picker, Value }

		private static readonly List<rzr.InstrType> SelectableInstructions = new( rzr.InstrType.Db.EnumValues() );

		public const uint FontSize = 12;

		private Picker m_InstrPicker = new() { ItemsSource = SelectableInstructions, FontFamily = Font.Regular, FontSize = FontSize, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Start }; // fixed list picking
		private Picker m_Lhs = new() { FontFamily = Font.Regular, FontSize = FontSize, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Start };
		private Picker m_Rhs = new() { FontFamily = Font.Regular, FontSize = FontSize, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Start };

		private Label m_ValueLabel = new Label { Text = "Ops:", FontFamily = Font.Regular, FontSize = FontSize };
		private Entry m_LhsValue = new() { FontFamily = Font.Regular, FontSize = FontSize }; // value / number picking
		private Entry m_RhsValue = new() { FontFamily = Font.Regular, FontSize = FontSize }; // value / number picking

		private Button m_ButtonFullText = new();

		public rzr.AsmInstr Instruction { get; }
		public InstructionPicker( rzr.AsmInstr instr )
		{
			Instruction = instr;

			ColumnDefinitions.Add( new() { Width = 70 } );
			ColumnDefinitions.Add( new() { Width = 60 } );
			ColumnDefinitions.Add( new() { Width = 60 } );

			RowDefinitions.Add( new RowDefinition() { Height = 30 } ); 
			RowDefinitions.Add( new RowDefinition() { Height = 30 } );

			( this as Grid ).Add( m_InstrPicker, column: 0, row: 0 );
			( this as Grid ).Add( m_Lhs, column: 1, row: 0 );
			( this as Grid ).Add( m_Rhs, column: 2, row: 0 );

			( this as Grid ).Add( m_ValueLabel, column: 0, row: 1 );
			( this as Grid ).Add( m_LhsValue, column: 1, row: 1 );
			( this as Grid ).Add( m_RhsValue, column: 2, row: 1 );

			m_InstrPicker.SelectedIndex = SelectableInstructions.FindIndex( i => i == instr.Type );
			m_InstrPicker.SelectedIndexChanged += OnInstrPicked;

			//m_Lhs.Clicked += OnLhsButtonPressed;
			//m_Rhs.Clicked += OnRhsButtonPressed;		
		}

		public void UnderlyingInstrChanged() 
		{
			m_ButtonFullText.Text = Instruction.ToString();
			m_InstrPicker.SelectedIndex = SelectableInstructions.FindIndex( i => i == Instruction.Type );
		}

		void OnInstrPicked( object sender, EventArgs args )
		{
			rzr.InstrType instr = SelectableInstructions[m_InstrPicker.SelectedIndex];
			if(instr != Instruction.Type )
			{
				Instruction.Type = instr;
				// TODO: clear operands
			}
			//RowDefinitions.RemoveAt( RowDefinitions.Count -1 );
		}

		void OnLhsButtonPressed( object sender, EventArgs args )
		{
		}

		void OnRhsButtonPressed( object sender, EventArgs args )
		{
		}

	}
}
