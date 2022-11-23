using Microsoft.Maui.Controls;

using System;
using System.Linq;
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
		private rzr.OperandSelector.ILhsToRhs CurLhsToRhs => Selector[CurInstrType];

		private List<rzr.AsmOperand> CurLhsSelecatbles => CurLhsToRhs.Lhs.Select( o => new rzr.AsmOperand(o) ).ToList();
		private rzr.OperandType CurLhs => CurLhsSelecatbles[m_Lhs.SelectedIndex];

		private List<rzr.AsmOperand> CurRhsSelectables => CurLhsToRhs[CurLhs].Select( o => new rzr.AsmOperand( o ) ).ToList();
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
			m_LhsValue.TextChanged += OnLhsValueChanged;
			m_RhsValue.TextChanged += OnRhsValueChanged;

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


		private static bool ParseD8( string text, out byte val ) => byte.TryParse( text, System.Globalization.NumberStyles.HexNumber, null, out val ) || byte.TryParse( text, System.Globalization.NumberStyles.Number, null, out val );
		private static bool ParseR8( string text, out sbyte val ) => sbyte.TryParse( text, System.Globalization.NumberStyles.HexNumber, null, out val ) || sbyte.TryParse( text, System.Globalization.NumberStyles.Number, null, out val );
		private static bool ParseD16( string text, out ushort val ) => ushort.TryParse( text, System.Globalization.NumberStyles.HexNumber, null, out val ) || ushort.TryParse( text, System.Globalization.NumberStyles.Number, null, out val );

		private static void ParseOpValue( string numText, rzr.AsmOperand target ) 
		{
			switch( target.Type )
			{
				case rzr.OperandType.d8:
				case rzr.OperandType.io8:
				case rzr.OperandType.RstAddr:
					if( ParseD8( numText, out var d8 ) )
						target.d8 = d8;
					break;
				case rzr.OperandType.r8 when ParseR8( numText, out var r8 ):
					target.r8 = r8;
					break;
				case rzr.OperandType.d16 when ParseD16( numText, out var d16 ):
					target.d16 = d16;
					break;
				default:
					break;
			}
		}

		void OnLhsValueChanged( object sender, TextChangedEventArgs args )
		{
			if(Instruction.Count < 1 || m_Lhs.SelectedIndex < 0 )
				return;

			ParseOpValue( args.NewTextValue, Instruction.L );
		}

		void OnRhsValueChanged( object sender, TextChangedEventArgs args )
		{
			if( Instruction.Count < 2 || m_Rhs.SelectedIndex < 0 )
				return;

			ParseOpValue( args.NewTextValue, Instruction.R );
		}

		void OnInstrPicked( object sender, EventArgs args )
		{
			rzr.InstrType instr = SelectableInstructions[m_InstrPicker.SelectedIndex];
			if( instr != Instruction.Type )
			{
				Instruction.Type = instr;

				if( m_Lhs.ItemsSource != CurLhsSelecatbles )
				{
					m_Lhs.ItemsSource = CurLhsSelecatbles;
					m_Lhs.SelectedIndex = Instruction.Count > 0 ? CurLhsSelecatbles.FindIndex( lhs => lhs == Instruction.Lhs ) : -1;

					if( m_Lhs.SelectedIndex < 0 ) // old Lhs operand not valid anymore
					{
						if( CurLhsSelecatbles.Count > 0 ) // reset Lhs
						{
							m_Lhs.SelectedIndex = 0;
						}
						else { Instruction.Clear(); }
					}
				}
			}
		}

		void OnLhsPicked( object sender, EventArgs args )
		{
			if(m_Lhs.SelectedIndex > -1 )
			{
				Instruction.SetL( CurLhs );

				if( m_Rhs.ItemsSource != CurRhsSelectables )
				{
					m_Rhs.ItemsSource = CurRhsSelectables;
					int rhsIdx = Instruction.Count > 1 ? CurRhsSelectables.FindIndex( lhs => lhs == Instruction.Rhs ) : -1;
					if( rhsIdx > -1 )
					{
						m_Rhs.SelectedIndex = rhsIdx;
					}
					else if( CurRhsSelectables.Count > 0 )
					{
						m_Rhs.SelectedIndex = 0;
					}
				}
			}
			else { m_Rhs.ItemsSource = null; }
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
