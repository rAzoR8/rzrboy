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
        public delegate void OnUpdate<T>( T view );

        public static T Update<T>( this T view, Callbacks callbacks, OnUpdate<T> func  )
        {
            callbacks.Add(() => func( view ) );
            return view;
        }

        public static T Update<T>( this T view, Callbacks callbacks, OnUpdate<T> func, out Callback callback )
        {
            callback = () => func( view );
            callbacks.Add( callback );
            return view;
        }
    }

    public class MainPage : ContentPage
    {
        private rzr.Boy boy;

        private Cpu cpu => boy.cpu;
        private Reg reg => boy.reg;
        private Mem mem => boy.mem;

        private List<Callback> m_beforeStep = new();
        private List<Callback> m_afterStep = new();

        private Callback m_updateDissassembly;

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
            }, out m_updateDissassembly );
        }

        public MainPage( rzr.Boy gb )
        {
            Title = "rzrBoy Studio";

            boy = gb;
            m_memEdit = new MemoryEditor( boy.cart.Mbc.RomBank( 0 ) , 0, 16, 16 );

            mem.WriteCallbacks.Add( ( Section section, ushort address, byte value ) => m_memEdit.OnSetValue( address, value ) );
            mem.WriteCallbacks.Add( ( Section section, ushort address, byte value ) => { if( address < 0x8000 ) m_updateDissassembly(); } );

            //boy.StepCallbacks.Add( ( reg, mem ) =>
            //{
            //    bool stop = reg.PC > 7 && reg.HL == 0;
            //    Debug.Assert( !stop );

            //    return true;
            //} );

            Content = new Grid
            {
                RowSpacing = 10,

                RowDefinitions = Rows.Define(
                    (Row.Title, Auto),
                    (Row.LoadAndSaveButtons, Auto),
                    (Row.RegAndDis, Auto),
                    (Row.Memory, Auto),
                    (Row.StepAndRunButtons, Auto)
                    ),

                Children =
                {
                    new HorizontalStackLayout {
                        new Label{ FontFamily = Font.Light, FontSize = 40, Text = $"rzr" },
                        new Label{ FontFamily = Font.Bold, FontSize = 40, Text = $"Boy" },
                        new Label{ FontFamily = Font.Regular, FontSize = 40, Text = $"Studio" },
                    }.Row(Row.Title),

                    new HorizontalStackLayout {
                        new Button { Text = "Load Boot" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnLoadBootClicked),
                        new Button { Text = "Load Rom" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnLoadRomClicked),
                        new Button { Text = "Save Rom" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnSaveRomClicked)
                    }.Row(Row.LoadAndSaveButtons),

                    new HorizontalStackLayout{ Registers(), Disassembly(10) }.Row(Row.RegAndDis),

                    m_memEdit.Row(Row.Memory),

                    new HorizontalStackLayout {
                        new Button { Text = "Step" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnStepClicked),
                        new Button { Text = "Run" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnRunClicked),
                    }.Row(Row.StepAndRunButtons),
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


        enum Row
        {
            Title,
            LoadAndSaveButtons,
            RegAndDis,
            Memory,
            StepAndRunButtons
        }

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

		private void OnStepClicked( object sender, EventArgs e )
		{
			foreach( Callback step in m_beforeStep )
			{
				step();
			}

			boy.Step( debugPrint: true );

			foreach( Callback step in m_afterStep )
			{
				step();
			}

			//SemanticScreenReader.Announce(CounterLabel.Text);
		}

        private async void OnSaveRomClicked( object sender, EventArgs e ) 
        {
            if( boy.IsRunning )
                cts.Cancel();

			var options = new PickOptions
			{
				PickerTitle = "Please select a rom file to save to",
			};

			var result = await FilePicker.PickAsync( options );
            if( result != null )
            {
                boy.cart.SaveRom( result.FullPath );
            }
            else
            {
                boy.cart.SaveRom( System.IO.Path.Combine( FileSystem.AppDataDirectory, boy.cart.GetFileName() ) );
            }
        }

        private async void OnLoadRomClicked( object sender, EventArgs e )
        {
            if( boy.IsRunning )
                cts.Cancel();

            var extensions = new[] { ".gb", ".gbc", ".rom", ".bin" };
            var options = new PickOptions
            {
                PickerTitle = "Please select a rom file to load",
			};

            var result = await FilePicker.PickAsync( options );
			if( result != null )
			{
				boy.LoadRom( await System.IO.File.ReadAllBytesAsync( result.FullPath ) );
				m_memEdit.Section = boy.cart.Mbc.RomBank( 0 );
                m_updateDissassembly();
			}
		}

        private async void OnLoadBootClicked( object sender, EventArgs e )
        {
            if( boy.IsRunning )
                cts.Cancel();

            var options = new PickOptions
            {
                PickerTitle = "Please select a rom file to load",
            };

			try
			{
                var result = await FilePicker.PickAsync( options );
                if( result != null )
                {
                    boy.LoadBootRom( await System.IO.File.ReadAllBytesAsync( result.FullPath ) );
                    m_memEdit.Section = boy.cart.Mbc.RomBank( 0 );
                }
            }
			catch( Exception )
			{
                // TODO: log
			}
        }
    }
}
