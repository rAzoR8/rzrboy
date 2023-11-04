using System;
using System.Collections.Generic;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls;

namespace rzrboy
{
    public class MemoryEditor : Grid
    {
		private Func<rzr.ISection> m_source;
        private int m_offset;
        private int m_columns;
        private int m_rows;

        public double CellHeight { get; set; } = 36;

		private Entry m_offsetEntry = null;
        private Entry m_prevCell = null;
        private Entry[,] m_cells = null;

        public List<rzr.OnWrite> WriteCallbacks { get; } = new();

		public Func<rzr.ISection> Source { get => m_source; set { m_source = value; Rebuild(); } }
		public int Offset { get => m_offset; set { if ( value != m_offset ) { m_offset = value; Rebuild(); } } }
        public int Columns { get => m_columns; set { if ( value != m_columns ) { m_columns = value; Rebuild(); } } }
        public int Rows { get => m_rows; set { if ( value != m_rows ) { m_rows = value; Rebuild(); } } }

        public MemoryEditor( Func<rzr.ISection> source, int offset, int columns, int rows )
        {
			m_source = source;
            m_offset = offset;
            m_columns = columns;
            m_rows = rows;

            RowSpacing = 1;

            Rebuild();
        }

		void OnEdit( object sender, TextChangedEventArgs e )
		{
			if( byte.TryParse( e.NewTextValue, System.Globalization.NumberStyles.HexNumber, null, out var val ) )
			{
                if( sender is Entry editor )
                {
					var r = (int)editor.GetValue( RowProperty ) - 1;
					var c = (int)editor.GetValue( ColumnProperty ) - 1;
					ushort addr = (ushort)( m_offset + r * m_columns + c );

					rzr.ISection section = m_source();
					section[addr] = val;

					foreach( rzr.OnWrite write in WriteCallbacks )
					{
						write( section, addr, val );
					}
				}
			}
		}

		private void Rebuild()
        {
            if(m_offsetEntry != null) 
            {
                m_offsetEntry.Text = $"0x{m_offset:X4}";
			}

			// just update the text
			if ( m_cells != null && m_cells.GetLength( 0 ) == m_rows && m_cells.GetLength( 1 ) == m_columns )
            {
				rzr.ISection section = m_source();

				for( int r = 0; r < m_rows; r++ )
                {
                    for ( int c = 0; c < m_columns; c++ )
                    {
                        ushort addr = (ushort)( m_offset + r * m_columns + c );
                        m_cells[r, c].TextChanged -= OnEdit;
						m_cells[r, c].Text = $"{section[addr]:X2}";
						m_cells[r, c].TextChanged += OnEdit;
					}
				}
                return;
            }

            Clear();
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            m_cells = new Entry[m_rows, m_columns];

            for ( int r = 0; r < m_rows + 1; r++ )
            {
                AddRowDefinition( new RowDefinition { Height = GridRowsColumns.Auto } );
            }

            for ( int r = 0; r < m_columns + 1; r++ )
            {
                AddColumnDefinition( new ColumnDefinition { Width = GridRowsColumns.Auto } );
            }

            m_offsetEntry = new Entry
            {
                Text = $"0x{m_offset:X4}",
                MinimumHeightRequest = CellHeight,
                MaximumHeightRequest = CellHeight,
                FontFamily = Font.Regular,
                FontSize = 12,
                Keyboard = Microsoft.Maui.Keyboard.Default,
            }
            .Row( 0 ).Column( 0 )
            .Invoke( edit => edit.Completed += OnSetOffset );

			Add( m_offsetEntry );

			// TODO: row & column add buttons

			for ( int c = 0; c < m_columns; c++ )
            {
                Add( new Label { Text = $"0x{c:X2}", FontFamily = Font.Regular, FontSize = 12 }.Row( 0 ).Column( c + 1 ) );
            }

			rzr.ISection initSection = m_source();

			for( int r = 0; r < m_rows; r++ )
            {
                Add( new Label { Text = $"0x{( m_offset + r * m_columns ):X4}", FontFamily = Font.Regular, FontSize = 12 }.Row( r + 1 ).Column( 0 ) );

                for ( int c = 0; c < m_columns; c++ )
                {
					ushort addr = (ushort)( m_offset + r * m_columns + c );
					byte initVal = initSection[addr];
                    var editor = new Entry
                    { 
                        Text = $"{initVal:X2}",
                        MinimumHeightRequest = CellHeight,
                        MaximumHeightRequest = CellHeight,                        
                        FontFamily = Font.Regular,
                        FontSize = 12,
                        Keyboard = Microsoft.Maui.Keyboard.Numeric,
                        MaxLength = 2
                    }
                    .Invoke( edit => edit.TextChanged += OnEdit )
                    .Column( c + 1 )
                    .Row( r + 1 );

                    m_cells[r, c] = editor;

                    Add( editor );
                }
            }
        }

        private Entry GetCell( int row, int col )
        {
            if(col >= m_columns || row >= m_rows) 
                return null;

            return m_cells[row, col];
        }

        public void OnSetValue( ushort address, byte value )
        {
            int addr = address - m_offset;

            if ( addr < 0 )
                return; // not in this view

            int row = addr / m_columns;
            int col = addr % m_columns;

            Entry cell = GetCell( row: row, col: col );
            if ( cell != null )
            {
                if ( m_prevCell != null )
                {
                    m_prevCell.TextColor = KnownColor.Default;
                }

                cell.Text = $"{value:X2}";
                cell.TextColor = KnownColor.Accent;

                m_prevCell = cell;
            }
        }

        private void OnSetOffset( object sender, EventArgs e )
        {
            var cell = sender as Entry;
            try
            {
                string text = cell.Text.StartsWith( "0x" ) ? cell.Text.Substring( 2 ) : cell.Text;
                Offset = Convert.ToInt32( text, 16 );
            }
            catch ( System.Exception )
            {

            }
        }
    }
}
