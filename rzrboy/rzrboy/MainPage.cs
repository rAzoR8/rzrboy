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

		public static T Spacing<T>( this T view, double spacing ) where T: StackBase
		{
			view.Spacing = spacing;
			return view;
		}

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

        private ISection GetCurRomBank() => boy.cart.Mbc.RomBank( bankIndex: boy.cart.Mbc.SelectedRamBank );

        private List<Callback> m_beforeStep = new();
        private List<Callback> m_afterStep = new();

        private MemoryEditor m_mainMemEdit;
		private MemoryEditor m_stackMemEdit;

		private ObservableCollection<AsmInstr> m_assembly = new();
        private Callback m_updateRamAssembly;
		private Callback m_updateRomAssembly;

		private enum RegRows 
        {
            AF, BC, DE, HL, SP, PC, Flags
        }

        private HorizontalStackLayout MakeRow( Reg16 reg, double cellHeight = 26 )
        {
            return new HorizontalStackLayout
            {
                new Label{ FontFamily = Font.Regular, Text = $"{reg} " },
                new Entry{ FontFamily = Font.Regular, Text = $"0x{this.reg[reg]:X4}", MinimumHeightRequest = cellHeight, MaximumHeightRequest = cellHeight }
				.Update( m_afterStep, lbl => {lbl.Text = $"0x{this.reg[reg]:X4}"; })
				.Invoke( edit => edit.Completed += ( object sender, EventArgs e) =>
				{
                    string text = (edit.Text.StartsWith("0x") || edit.Text.StartsWith("0X")) ? edit.Text.Substring(2) : edit.Text;
					if( ushort.TryParse( text, System.Globalization.NumberStyles.HexNumber, null, out var val ) )
					{
						this.reg[reg] = val;
					}
				} )
			};
        }

        private Grid Registers()
        {
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
                    MakeRow(Reg16.AF).Row(RegRows.AF),
                    MakeRow(Reg16.BC).Row(RegRows.BC),
                    MakeRow(Reg16.DE).Row(RegRows.DE),
                    MakeRow(Reg16.HL).Row(RegRows.HL),
                    MakeRow(Reg16.SP).Row(RegRows.SP),
                    MakeRow(Reg16.PC).Row(RegRows.PC),
                    new HorizontalStackLayout
                    {
						new Label{ FontFamily = Font.Regular, Text = "Z" },
						new CheckBox{ IsChecked = reg.Zero }.Update(m_afterStep, box => box.IsChecked = reg.Zero ).Invoke( box => box.CheckedChanged += (object sender, CheckedChangedEventArgs e) => reg.Zero = e.Value),
						new Label{ FontFamily = Font.Regular, Text = "N" },
						new CheckBox{ IsChecked = reg.Sub }.Update(m_afterStep, box => box.IsChecked = reg.Zero ).Invoke( box => box.CheckedChanged += (object sender, CheckedChangedEventArgs e) => reg.Sub = e.Value),
						new Label{ FontFamily = Font.Regular, Text = "H" },
						new CheckBox{ IsChecked = reg.HalfCarry }.Update(m_afterStep, box => box.IsChecked = reg.HalfCarry ).Invoke( box => box.CheckedChanged += (object sender, CheckedChangedEventArgs e) => reg.HalfCarry = e.Value),
						new Label{ FontFamily = Font.Regular, Text = "C" },
						new CheckBox{ IsChecked = reg.Carry }.Update(m_afterStep, box => box.IsChecked = reg.Carry ).Invoke( box => box.CheckedChanged += (object sender, CheckedChangedEventArgs e) => reg.Carry = e.Value),
					}.Row(RegRows.Flags)
                }
            };
        }

        private View Disassembly( int instructions, Func<ISection> source, out Callback callback )
        {
            //return new AssemblyView( m_assembly );

            return new Label { FontFamily = Font.Regular }.Update( m_afterStep, lbl =>
            {
                int i = 0;
                StringBuilder sb = new();

                try
                {
					foreach( string instr in Isa.Disassemble( cpu.curInstrPC, (ushort)( cpu.curInstrPC + instructions * 3 ), mem: source(), unknownOp: UnknownOpHandling.AsDb ) )
					{
						if( i++ > instructions )
						{
							break;
						}

						sb.AppendLine( instr );
					}
				}
                catch( System.Exception e )
                {
                    System.Diagnostics.Debug.WriteLine( e.Message );
                }

                lbl.Text = sb.ToString();
            }, out callback );
        }

        public MainPage( rzr.Boy gb )
        {
            Title = "rzrBoy Studio";

            InstructionPicker picker = new( Asm.Ld( Asm.A, Asm.D ) );

            boy = gb;
            m_mainMemEdit = new MemoryEditor( source: GetCurRomBank, offset: 0, columns: 16, rows: 16 );
            m_stackMemEdit = new MemoryEditor( source: () => mem, offset: 0, columns: 2, rows: 6 );

            // program memory writes
            mem.WriteCallbacks.Add( ( ISection section, ushort address, byte value ) =>
            { 
                m_mainMemEdit.OnSetValue( address, value );
			} ) ;

            // user edit writes
            m_mainMemEdit.WriteCallbacks.Add( ( section, address, value ) =>
            {
                if( address < 0x8000 )
                {
                    m_updateRamAssembly();
                    m_updateRomAssembly();
                }
            } );


			m_afterStep.Add( () => m_stackMemEdit.Offset = reg.SP );
            //boy.PostStepCallbacks.Add( ( reg, mem ) => m_stackMemEdit.Offset = reg.SP );

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
                    (Row.MemoryAndRomDis, Auto)
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

                    new HorizontalStackLayout
                    {
                        Registers(),
                        m_stackMemEdit,
                        Disassembly(10, source: () => mem, out m_updateRamAssembly),
                    }.Row(Row.RegAndDis).Spacing(10),

                    new HorizontalStackLayout {
                        new Button { Text = "Step" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnStepClicked),
                        new Button { Text = "Run" }
                            .Font(bold: true, size: 20)
                            .Invoke(button => button.Clicked += OnRunClicked),
                    }.Row(Row.StepAndRunButtons),

					new HorizontalStackLayout
					{
						m_mainMemEdit, // TODO: add bank selector
						Disassembly(32, source: () => boy.cart.Mbc.RomBank( bankIndex: boy.cart.Mbc.SelectedRomBank ), out m_updateRomAssembly)
					}.Row(Row.MemoryAndRomDis)
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
            MemoryAndRomDis,
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
				m_mainMemEdit.Source = GetCurRomBank;
                m_updateRomAssembly();
                m_updateRamAssembly();
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
					m_updateRamAssembly();
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

        public void Debug( System.Type type ) 
        {
            boy.PreStepCallbacks.Add( ( reg, mem ) =>
            {
                var writer = (rzr.MbcWriter)System.Activator.CreateInstance( type );

				writer.WriteAll();
                boy.LoadRom( writer.Rom() );

				m_mainMemEdit.Source = GetCurRomBank;
				m_updateRomAssembly();
				m_updateRamAssembly();
			} );
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
					string selected = await DisplayActionSheet( title: "Module to debug:", cancel: "Cancel", destruction: "OK", typeNames );
					
                    if( selected == null && typeNames.Length >0)
                        selected = typeNames[0];

                    if( selected != null )
					{
						System.Type mwType = assembly.GetType( selected );
						MbcWriter writer = (MbcWriter)System.Activator.CreateInstance( mwType );

						writer.WriteAll();
						boy.LoadRom( writer.Rom() );
						m_mainMemEdit.Source = GetCurRomBank;
						m_updateRomAssembly();
						m_updateRamAssembly();
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
