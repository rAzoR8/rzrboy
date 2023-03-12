using System;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using rzr;
using System.Collections.Generic;
using System.Threading;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using System.Text;
using System.Linq;

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

        private MemoryEditor m_memEdit;
        private ObservableCollection<AsmInstr> m_assembly = new();
        private Callback m_updateDissassembly;

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

        private View Disassembly( int instructions )
        {
            //return new AssemblyView( m_assembly );

            return new Label { FontFamily = Font.Regular }.Update( m_afterStep, lbl =>
            {
                int i = 0;
                StringBuilder sb = new();
                foreach( string instr in Isa.Disassemble( cpu.curInstrPC, (ushort)( cpu.curInstrPC + instructions * 3 ), mem ) )
                {
                    if( i++ > instructions )
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

            InstructionPicker picker = new( Asm.Ld(Asm.A, Asm.D) );

            boy = gb;
            m_memEdit = new MemoryEditor( boy.cart.Mbc.RomBank( 0 ) , offset: 0, columns: 16, rows: 16 );

            mem.WriteCallbacks.Add( ( ISection section, ushort address, byte value ) => m_memEdit.OnSetValue( address, value ) );
            //mem.WriteCallbacks.Add( ( ISection section, ushort address, byte value ) => { if( address < 0x8000 ) m_updateDissassembly(); } );

            //boy.PostStepCallbacks.Add( ( reg, mem ) =>
			//{
            //  refresh m_assembly around reg.PC =- 30
			//});

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
                    (Row.StepAndRunButtons, Auto),
                    (Row.Memory, Auto)
                    ),

                Children =
                {
                    picker.Row(Row.Title),
                    //new HorizontalStackLayout {
                    //    new Label{ FontFamily = Font.Light, FontSize = 40, Text = $"rzr" },
                    //    new Label{ FontFamily = Font.Bold, FontSize = 40, Text = $"Boy" },
                    //    new Label{ FontFamily = Font.Regular, FontSize = 40, Text = $"Studio" },
                    //}.Row(Row.Title),

                    new HorizontalStackLayout {
                        new Button { Text = "Load Boot" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnLoadBootClicked),
                        new Button { Text = "Load Rom" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnLoadRomClicked),
                        new Button { Text = "Save Rom" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnSaveRomClicked),
						new Button { Text = "Debug" }
							.Font(bold: true, size: 20)
							.Invoke(button => button.Clicked += OnDebugClicked)
					}.Row(Row.LoadAndSaveButtons),

                    new HorizontalStackLayout{ Registers(), Disassembly(10) }.Row(Row.RegAndDis),

                    new HorizontalStackLayout {
                        new Button { Text = "Step" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnStepClicked),
                        new Button { Text = "Run" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnRunClicked),
                    }.Row(Row.StepAndRunButtons),

                    m_memEdit.Row(Row.Memory),
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
            StepAndRunButtons,
            Memory,
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
				button.Text = "Run";

				foreach( Callback step in m_afterStep )
				{
					step();
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
                var rom = await System.IO.File.ReadAllBytesAsync( result.FullPath );

                boy.LoadRom( rom );
				m_memEdit.Section = boy.cart.Mbc.RomBank( 0 );
                m_updateDissassembly();
				//m_assembly.Clear();
				//foreach( AsmInstr instr in Asm.Disassemble( rom ) )
				//{
				//  m_assembly.Add( instr );
				//}
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
					m_updateDissassembly();
				}
			}
			catch( rzr.Exception )
			{
				// TODO: log
			}
			catch( System.Exception )
			{
                // TODO: log
			}
		}

        private async void OnDebugClicked( object sender, EventArgs args )
        {
            if( boy.IsRunning )
                cts.Cancel();

			var options = new PickOptions
			{
				PickerTitle = "Please select a assembly load",
			};

            try
            {
				var result = await FilePicker.PickAsync( options );
				if( result != null )
				{
					var assembly = System.Reflection.Assembly.LoadFrom( result.FullPath );

					var types = assembly.GetTypes().Where( t => !t.IsAbstract && t.IsClass && t.IsSubclassOf( typeof( MbcWriter ) ) );
					string[] typeNames = types.Select( t => t.FullName ).ToArray();
					string selected = await DisplayActionSheet( title: "Module to debug:", cancel: "Cancel", destruction: "Destroy", typeNames );
					
                    if(selected == null && typeNames.Length >0)
                        selected = typeNames[0];

                    if( selected != null )
					{
						System.Type mwType = assembly.GetType( selected );
						MbcWriter writer = (MbcWriter)System.Activator.CreateInstance( mwType );

						writer.WriteAll();
						boy.LoadRom( writer.Rom() );
						m_memEdit.Section = boy.cart.Mbc.RomBank( 0 );
						m_updateDissassembly();

						boy.PreStepCallbacks.Add( ( reg, mem ) =>
						{
							writer.WriteAll();
							boy.LoadRom( writer.Rom() );
						} );
					}
				}
			}
            catch( System.Exception e )
            {
                System.Diagnostics.Debug.WriteLine( e.Message );
            }			
		}
	}
}
