using Microsoft.Maui.Controls;

using System;
using System.Collections.Generic;
using static rzr.ExtensionMethods;

namespace rzrboy
{
	public class InstructionPicker : Grid
	{
		public enum Row { Picker, Value }

		private static readonly rzr.OperandSelector Selector = new();
		private static readonly List<rzr.InstrType> SelectableInstructions = new( Selector );

		private rzr.InstrType CurInstrType => SelectableInstructions[m_InstrPicker.SelectedIndex];
		private rzr.OperandSelector.LhsToRhs CurLhsToRhs => Selector[CurInstrType];

		private List<rzr.OperandType> CurLhsSelecatbles => CurLhsToRhs.Lhs;
		private rzr.OperandType CurLhs => CurLhsSelecatbles[m_Lhs.SelectedIndex];

		private List<rzr.OperandType> CurRhsSelectables => CurLhsToRhs[CurLhs];
		private rzr.OperandType CurRhs => CurRhsSelectables[m_Rhs.SelectedIndex];

		public const uint FontSize = 12;

		private Picker m_InstrPicker = new() { ItemsSource = SelectableInstructions, FontFamily = Font.Regular, FontSize = FontSize, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Start }; // fixed list picking
		private Picker m_Lhs = new() { FontFamily = Font.Regular, FontSize = FontSize, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Start };
		private Picker m_Rhs = new() { FontFamily = Font.Regular, FontSize = FontSize, HorizontalTextAlignment = Microsoft.Maui.TextAlignment.Start };

		private Label m_OpCodeLabel = new Label { Text = "Op", FontFamily = Font.Regular, FontSize = FontSize };
		private Entry m_LhsValue = new() { FontFamily = Font.Regular, FontSize = FontSize }; // value / number picking
		private Entry m_RhsValue = new() { FontFamily = Font.Regular, FontSize = FontSize }; // value / number picking

		private Button m_ButtonFullText = new();

		public rzr.AsmInstr Instruction { get; }

		private RowDefinitionCollection m_EditRowDefs = new ();
		private RowDefinitionCollection m_DisplayRowDefs = new();

		private ColumnDefinitionCollection m_EditColDefs = new();
		private ColumnDefinitionCollection m_DisplayColDefs = new();

		public InstructionPicker( rzr.AsmInstr instr )
		{
			Instruction = instr;

			// edit layout:
			// |Instr^|Lhs^|Rhs^|
			// |OpCode|Lval|Rval|
			{
				m_EditColDefs.Add( new() { Width = 70 } );
				m_EditColDefs.Add( new() { Width = 60 } );
				m_EditColDefs.Add( new() { Width = 60 } );

				m_EditRowDefs.Add( new() { Height = 30 } );
				m_EditRowDefs.Add( new() { Height = 30 } );
			}

			// display layout:
			// text: LD A, B
			{
				m_DisplayColDefs.Add( new() { } );
				m_DisplayRowDefs.Add( new() { Height = 30 } );
			}

			DisplayMode();
			UnderlyingInstrChanged();

			m_InstrPicker.SelectedIndexChanged += OnInstrPicked;
			m_Lhs.SelectedIndexChanged += OnLhsPicked;
			m_Rhs.SelectedIndexChanged += OnRhsPicked;

			m_ButtonFullText.Clicked += ( object sender, EventArgs args ) => EditMode();	
		}

		public enum Mode { Edit, Display }
		public Mode SelectedMode { get; private set; } = Mode.Display;

		public void EditMode()
		{
			SelectedMode = Mode.Edit;

			Clear();
			RowDefinitions = m_EditRowDefs;
			ColumnDefinitions = m_EditColDefs;

			( this as Grid ).Add( m_InstrPicker, column: 0, row: 0 );
			( this as Grid ).Add( m_Lhs, column: 1, row: 0 );
			( this as Grid ).Add( m_Rhs, column: 2, row: 0 );

			( this as Grid ).Add( m_OpCodeLabel, column: 0, row: 1 );
			( this as Grid ).Add( m_LhsValue, column: 1, row: 1 );
			( this as Grid ).Add( m_RhsValue, column: 2, row: 1 );
		}

		public void DisplayMode() 
		{
			SelectedMode = Mode.Display;

			Clear();
			RowDefinitions = m_DisplayRowDefs;
			ColumnDefinitions = m_DisplayColDefs;

			( this as Grid ).Add( m_ButtonFullText, column: 0, row: 0 );
		}

		public void UnderlyingInstrChanged() 
		{
			m_ButtonFullText.Text = Instruction.ToString().ToUpper();

			m_InstrPicker.SelectedIndex = SelectableInstructions.FindIndex( i => i == Instruction.Type );
			m_Lhs.ItemsSource = CurLhsSelecatbles;

			if( Instruction.Count > 0 )
			{
				m_Lhs.SelectedIndex = CurLhsSelecatbles.FindIndex( lhs => lhs == Instruction.Lhs );

				if( m_Lhs.SelectedIndex > -1 )
				{
					m_Rhs.ItemsSource = CurRhsSelectables;

					if( Instruction.Count > 1 )
					{
						m_Rhs.SelectedIndex = CurRhsSelectables.FindIndex( rhs => rhs == Instruction.Rhs );
					}
				}
			}
		}

		void OnInstrPicked( object sender, EventArgs args )
		{
			rzr.InstrType instr = SelectableInstructions[m_InstrPicker.SelectedIndex];
			if( instr != Instruction.Type )
			{
				Instruction.Type = instr;
				m_Lhs.ItemsSource = CurLhsSelecatbles;
				m_Lhs.SelectedIndex = CurLhsSelecatbles.FindIndex( lhs => lhs == Instruction.Lhs );

				m_Lhs.IsVisible = CurLhsSelecatbles.Count > 0;
				if( m_Lhs.IsVisible == false ) m_Rhs.IsVisible = false;

				if( m_Lhs.SelectedIndex < 0)
				{
					if( CurLhsSelecatbles.Count > 0 ) // reset Lhs
					{
						m_Lhs.SelectedIndex = 0;
						Instruction.SetL( CurLhsSelecatbles[0] );
					}
					else { Instruction.Clear();  }
				}
			}
		}

		void OnLhsPicked( object sender, EventArgs args )
		{
			if(m_Lhs.SelectedIndex > -1 )
			{
				Instruction.SetL( CurLhs );
				m_Rhs.ItemsSource = CurRhsSelectables;
				m_Rhs.IsVisible = m_Rhs.ItemsSource.Count > 0;
			}
			else { m_Rhs.IsVisible = false; }
		}

		void OnRhsPicked( object sender, EventArgs args )
		{
			if( m_Rhs.SelectedIndex > -1 )
			{
				Instruction.SetR( CurRhs );			
			}
		}
	}
}
