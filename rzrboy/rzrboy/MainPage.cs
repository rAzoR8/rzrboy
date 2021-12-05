using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;
using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using Microsoft.Maui;
using emu;
using System.Collections.Generic;
using System.Text;

namespace rzrboy
{
    using Callbacks = IList<Callback>;
    public delegate void Callback();

    public static class Extensions 
    {
        public delegate void OnUpateLabel( Label label );
        public delegate void OnUpdateGrid( Grid label );

        public static Label Update( this Label label, Callbacks callbacks, OnUpateLabel func )
        {
            callbacks.Add(() => func( label ) );
            return label;
        }

        public static Grid Update( this Grid grid, Callbacks callbacks, OnUpdateGrid func )
        {
            callbacks.Add( () => func( grid ) );
            return grid;
        }
    }

    public class MainPage : ContentPage
    {
        private emu.Gb m_gb;

        private Cpu cpu => m_gb.cpu;
        private Reg reg => m_gb.cpu.reg;
        private Mem mem => m_gb.mem;

        List<Callback> m_beforeStep = new();

        List<Callback> m_afterStep = new();

        private enum RegRows 
        {
            AF, BC, DE, HL, SP, PC, Flags
        }

        private HorizontalStackLayout MakeRow( Reg8 l, Reg8 r )
        {
            return new HorizontalStackLayout
            {
                new Label{ Text = $"{l}  0x" },
                new Label{ }.Update( m_afterStep, lbl => {lbl.Text = $"{reg[l]:X2}"; }),
                //new Label{ }.Bind(Label.TextProperty, nameof(emu.Cpu.reg.PC)),
                new Label{ }.Update( m_afterStep, lbl => {lbl.Text = $"{reg[r]:X2}"; }),
                new Label{ Text = $" {r}" }
            };
        }

        private HorizontalStackLayout MakeRow( Reg16 reg )
        {
            return new HorizontalStackLayout
            {
                new Label{ Text = $"{reg} 0x" },
                new Label{ }.Update( m_afterStep, lbl => {lbl.Text = $"{this.reg[reg]:X4}"; }),
            };
        }

        private Grid Registers()
        {
            int byt( bool b ) => b ? 1 : 0;

            return new Grid
            {
                RowSpacing = 2,
                
                Padding = Device.RuntimePlatform switch
                {
                    Device.iOS => new Thickness( 30, 60, 30, 30 ),
                    _ => new Thickness( 30 )
                },

                RowDefinitions = Rows.Define(
                    (RegRows.AF, Auto),
                    (RegRows.BC, Auto),
                    (RegRows.DE, Auto),
                    (RegRows.HL, Auto),
                    (RegRows.SP, Auto),
                    (RegRows.PC, Auto),
                    (RegRows.Flags, Auto)
                    ),

                Children =
                {
                    MakeRow(Reg8.A, Reg8.F).Row(RegRows.AF),
                    MakeRow(Reg8.B, Reg8.C).Row(RegRows.BC),
                    MakeRow(Reg8.D, Reg8.E).Row(RegRows.DE),
                    MakeRow(Reg8.H, Reg8.L).Row(RegRows.HL),
                    MakeRow(Reg16.SP).Row(RegRows.SP),
                    MakeRow(Reg16.PC).Row(RegRows.PC),
                    new Label{ }.Update(m_afterStep, lbl => lbl.Text = $"Z {byt(reg.Zero)} N {byt(reg.Sub)} H {byt(reg.HalfCarry)} C {byt(reg.Carry)}").Row(RegRows.Flags)
                }
            };
        }

        private Grid Memory(ISection section, int columns, int rows, int offset )
        {
            GridLength height = 10;
            var grid = new Grid
            {
                RowSpacing = 1,                
            };            

            for ( int r = 0; r < rows; r++ )
            {
                grid.AddRowDefinition( new RowDefinition { Height = height } );
            }

            for ( int r = 0; r < rows; r++ )
            {
                var row = new HorizontalStackLayout();
                row.Add( new Label { Text = $"0x{offset + r * columns}:X4" } );

                for ( int c = 0; c < columns; c++ )
                {
                    void OnEdit( object sender, EventArgs e )
                    {
                        var editor = sender as Editor;
                        if ( byte.TryParse( editor.Text, out var val ) )
                        {
                            ushort addr = (ushort)( offset + r * columns + c );
                            section[addr] = val;
                        }
                    }

                    row.Add( new Editor { AutoSize = EditorAutoSizeOption.Disabled }.Invoke( edit => edit.Completed += OnEdit ) );
                }

                grid.Add( row );
            }            

            return grid;
        }

        private Label Disassembly( int instructions )
        {
            return new Label { }.Update( m_afterStep, lbl =>
            {
                int i = 0;
                StringBuilder sb = new();
                foreach ( string instr in Cpu.isa.Disassemble( cpu.curInstrPC, (ushort)( cpu.curInstrPC + instructions * 3 ), mem ) )
                {
                    if ( i++ > instructions )                    
                    {
                        break;
                    }

                    sb.AppendLine( instr );
                }

                lbl.Text = sb.ToString();
            } );
        }

        public MainPage( emu.Gb gb )
        {
            m_gb = gb;

            Content = new Grid
            {
                RowSpacing = 10,

                RowDefinitions = Rows.Define(
                    (Row.Step, Auto),
                    (Row.RegAndMem, Auto),
                    (Row.Disassembly, Auto)
                    ),

                Children =
                {
                    new Button { Text = "Step" }
                        .Row(Row.Step)
                        .Font(bold: true)
                        //.CenterHorizontal()
                        .Invoke(button => button.Clicked += OnStepClicked),

                    new HorizontalStackLayout{ Registers(), Memory(mem, 8, 16, 0) }.Row(Row.RegAndMem),
                    Disassembly(10).Row(Row.Disassembly)
                }
            };

            // init
            foreach ( Callback step in m_beforeStep )
            {
                step();
            }

            foreach ( Callback step in m_afterStep )
            {
                step();
            }
        }

        //Children =
        //        {
        //            new Label { Text = "Hello World" }
        //                .Row( Row.HelloWorld).Font( size: 32)
        //                .CenterHorizontal().TextCenter(),

        //            new Label { Text = "Welcome to .NET MAUI Markup Community Toolkit Sample" }
        //                .Row( Row.Welcome ).Font( size: 18 )
        //                .CenterHorizontal().TextCenter(),

        //            new HorizontalStackLayout
        //            {
        //                new Label { Text = "PC: " }
        //                    .Font(bold: true)
        //                    .FillHorizontal().TextEnd()
        //                    .Assign(out CounterLabel),

        //                new Label()
        //                    .Font(bold: true)
        //                    .FillHorizontal().TextStart()
        //                    /*.Assign(out CounterLabel)*/,

        //            }.Row( Row.Count ).CenterHorizontal(),

        //            new Button { Text = "Step" }
        //                .Row( Row.ClickMeButton )
        //                .Font( bold: true )
        //                .CenterHorizontal()
        //                .Invoke( button => button.Clicked += OnStepClicked )
        //                //.BindCommand(nameof(ViewModel.ClickMeButtonCommand))
        //            ,

        //            new Image { Source = "dotnet_bot.png", WidthRequest = 250, HeightRequest = 310 }
        //                .Row( Row.Image )
        //                .CenterHorizontal()
        //        }


        enum Row { Step, RegAndMem, Disassembly }


        private void OnStepClicked(object sender, EventArgs e)
        {
            foreach ( Callback step in m_beforeStep )
            {
                step();
            }

            m_gb.Step();

            foreach ( Callback step in m_afterStep )
            {
                step();
            }

            //SemanticScreenReader.Announce(CounterLabel.Text);
        }
    }
}
