using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;
using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using Microsoft.Maui;
using rzr;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

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
        private rzr.Boy boy;

        private Cpu cpu => boy.cpu;
        private Reg reg => boy.reg;
        private Mem mem => boy.mem;

        List<Callback> m_beforeStep = new();
        List<Callback> m_afterStep = new();

        private MemoryEditor m_memEdit;

        private enum RegRows 
        {
            AF, BC, DE, HL, SP, PC, Flags
        }

        private HorizontalStackLayout MakeRow( Reg8 l, Reg8 r )
        {
            return new HorizontalStackLayout
            {
                new Label{ FontFamily = Font.Regular, Text = $"{l}  0x" },
                new Label{ FontFamily = Font.Regular, }.Update( m_afterStep, lbl => {lbl.Text = $"{reg[l]:X2}"; }),
                //new Label{ }.Bind(Label.TextProperty, nameof(emu.Cpu.reg.PC)),
                new Label{ FontFamily = Font.Regular, }.Update( m_afterStep, lbl => {lbl.Text = $"{reg[r]:X2}"; }),
                new Label{ FontFamily = Font.Regular, Text = $" {r}" }
            };
        }

        private HorizontalStackLayout MakeRow( Reg16 reg )
        {
            return new HorizontalStackLayout
            {
                new Label{ FontFamily = Font.Regular , Text = $"{reg} 0x" },
                new Label{ FontFamily = Font.Regular }.Update( m_afterStep, lbl => {lbl.Text = $"{this.reg[reg]:X4}"; }),
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
                    new Label{ FontFamily = Font.Regular }.Update(m_afterStep, lbl => lbl.Text = $"Z {byt(reg.Zero)} N {byt(reg.Sub)} H {byt(reg.HalfCarry)} C {byt(reg.Carry)}").Row(RegRows.Flags)
                }
            };
        }

        private Label Disassembly( int instructions )
        {
            return new Label { FontFamily = Font.Regular }.Update( m_afterStep, lbl =>
            {
                int i = 0;
                StringBuilder sb = new();
                foreach ( string instr in boy.isa.Disassemble( cpu.curInstrPC, (ushort)( cpu.curInstrPC + instructions * 3 ), mem ) )
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

        public MainPage( rzr.Boy gb )
        {
            boy = gb;
            m_memEdit = new MemoryEditor( boy.cart.Mbc.Rom() , 0, 16, 16 );

            mem.WriteCallbacks.Add( ( Section section, ushort address, byte value ) => m_memEdit.OnSetValue( address, value ) );

            boy.StepCallbacks.Add( ( reg, mem ) =>
            {
                bool stop = reg.PC > 7 && reg.HL == 0;
                Debug.Assert( !stop );

                return true;
            } );

            Content = new Grid
            {
                RowSpacing = 10,

                RowDefinitions = Rows.Define(
                    (Row.ControlButtons, Auto),
                    (Row.RegAndMem, Auto),
                    (Row.Disassembly, Auto)
                    ),

                Children =
                {
                    new HorizontalStackLayout {
                        new Button { Text = "Step" }
                            .Font(bold: true, size: 20)
                            //.CenterHorizontal()
                            .Invoke(button => button.Clicked += OnStepClicked),

                        new Button { Text = "Run" }
                            .Row(Row.ControlButtons)
                            .Font(bold: true, size: 20)
                            //.CenterHorizontal()
                            .Invoke(button => button.Clicked += OnRunClicked)                            
                    }.Row(Row.ControlButtons),

                    new HorizontalStackLayout{ Registers(), m_memEdit }.Row(Row.RegAndMem),
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


        enum Row { ControlButtons, RegAndMem, Disassembly }

        private CancellationTokenSource cts = new();

        private async void OnRunClicked( object sender, EventArgs e ) 
        {
            Button button = sender as Button;

            if( boy.IsRunning == false )
            {
                button.Text = "Stop";

                foreach( Callback step in m_beforeStep )
                {
                    step();
                }

                await boy.Execute( cts.Token );
                {
                    button.Text = "Run";

                    foreach( Callback step in m_afterStep )
                    {
                        step();
                    }
                }
            }
            else
            {
                cts.Cancel();
                button.Text = "Run";
            }
        }

        private void OnStepClicked(object sender, EventArgs e)
        {
            foreach ( Callback step in m_beforeStep )
            {
                step();
            }

            boy.Step(debugPrint: true);

            foreach ( Callback step in m_afterStep )
            {
                step();
            }

            //SemanticScreenReader.Announce(CounterLabel.Text);
        }
    }
}
