using System;
using System.Collections.Generic;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.Controls;

namespace rzrboy
{
    public class MemoryEditor : Grid
    {
        private rzr.ISection m_section;
        private int m_offset;
        private int m_columns;
        private int m_rows;

        private Entry m_prevCell = null;

        private Entry[,] m_cells = null;

        public rzr.ISection Section { get => m_section; set { if ( value != m_section ) { m_section = value; Rebuild(); } } }
        public int Offset { get => m_offset; set { if ( value != m_offset ) { m_offset = value; Rebuild(); } } }
        public int Columns { get => m_columns; set { if ( value != m_columns ) { m_columns = value; Rebuild(); } } }
        public int Rows { get => m_rows; set { if ( value != m_rows ) { m_rows = value; Rebuild(); } } }

        public MemoryEditor( rzr.ISection section, int offset, int columns, int rows )
        {
            m_section = section;
            m_offset = offset;
            m_columns = columns;
            m_rows = rows;

            RowSpacing = 1;

            Rebuild();
        }

        private void Rebuild( )
        {
            // just update the text
            if ( m_cells != null && m_cells.GetLength( 0 ) == m_rows && m_cells.GetLength( 1 ) == m_columns )
            {
                for ( int r = 0; r < m_rows; r++ )
                {
                    for ( int c = 0; c < m_columns; c++ )
                    {
                        ushort addr = (ushort)( m_offset + r * m_columns + c );
                        m_cells[r, c].Text = $"{m_section[addr]:X2}";
                    }
                }
                return;
            }

            Clear();
            RowDefinitions.Clear();
            ColumnDefinitions.Clear();

            const double CellHeight = 26;

            m_cells = new Entry[m_rows, m_columns];

            for ( int r = 0; r < m_rows + 1; r++ )
            {
                AddRowDefinition( new RowDefinition { Height = GridRowsColumns.Auto } );
            }

            for ( int r = 0; r < m_columns + 1; r++ )
            {
                AddColumnDefinition( new ColumnDefinition { Width = GridRowsColumns.Auto } );
            }

            Add( 
                new Entry {
                    Text = $"0x{m_offset:X4}",
                    MinimumHeightRequest = CellHeight,
                    MaximumHeightRequest = CellHeight,
                    FontFamily = Font.Regular,
                    Keyboard = Microsoft.Maui.Keyboard.Numeric
                }
                .Row( 0 ).Column( 0 )
                .Invoke( edit => edit.Completed += OnSetOffset)
            );

            // TODO: row & column add buttons

            for ( int c = 0; c < m_columns; c++ )
            {
                Add( new Label { Text = $"0x{c:X2}", FontFamily = Font.Regular }.Row( 0 ).Column( c + 1 ) );
            }

            for ( int r = 0; r < m_rows; r++ )
            {
                Add( new Label { Text = $"0x{( m_offset + r * m_columns ):X4}", FontFamily = Font.Regular }.Row( r + 1 ).Column( 0 ) );

                for ( int c = 0; c < m_columns; c++ )
                {
                    ushort addr = (ushort)( m_offset + r * m_columns + c );

                    void OnEdit( object sender, EventArgs e )
                    {
                        var cell = sender as Entry;
                        if ( byte.TryParse( cell.Text, System.Globalization.NumberStyles.HexNumber, null, out var val ) )
                        {
                            byte prev = m_section[addr];
                            if ( prev != val )
                            {
                                m_section[addr] = val;
                            }
                        }
                    }

                    byte initVal = m_section[addr];

                    var editor = new Entry
                    { 
                        Text = $"{initVal:X2}",
                        MinimumHeightRequest = CellHeight,
                        MaximumHeightRequest = CellHeight,
                        //AutoSize = EditorAutoSizeOption.Disabled,
                        FontFamily = Font.Regular,
                        Keyboard = Microsoft.Maui.Keyboard.Numeric,
                        MaxLength = 2
                    }
                    .Invoke( edit => edit.Completed += OnEdit )
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
            catch ( Exception )
            {

            }
        }
    }
}
