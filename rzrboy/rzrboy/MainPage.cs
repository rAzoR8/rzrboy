using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Essentials;
using CommunityToolkit.Maui.Markup;
using static CommunityToolkit.Maui.Markup.GridRowsColumns;
using Microsoft.Maui;

namespace rzrboy
{
    public class MainPage : ContentPage
    {
        int count = 0;
        Label CounterLabel = new Label() { Text = "0" };
        emu.Gb m_gb;

        public MainPage( emu.Gb gb )
        {
            m_gb = gb;

            Content = new Grid
            {
                RowSpacing = 25,

                Padding = Device.RuntimePlatform switch
                {
                    Device.iOS => new Thickness(30, 60, 30, 30),
                    _ => new Thickness(30)
                },

                RowDefinitions = Rows.Define(
                    (Row.HelloWorld, Auto),
                    (Row.Welcome, Auto),
                    (Row.Count, Auto),
                    (Row.ClickMeButton, Auto),
                    (Row.Image, Auto)),

                Children =
                {
                    new Label { Text = "Hello World" }
                        .Row(Row.HelloWorld).Font(size: 32)
                        .CenterHorizontal().TextCenter(),

                    new Label { Text = "Welcome to .NET MAUI Markup Community Toolkit Sample" }
                        .Row(Row.Welcome).Font(size: 18)
                        .CenterHorizontal().TextCenter(),

                    new HorizontalStackLayout
                    {
                        new Label { Text = "PC: " }
                            .Font(bold: true)
                            .FillHorizontal().TextEnd()
                            .Assign(out CounterLabel),

                        new Label()
                            .Font(bold: true)
                            .FillHorizontal().TextStart()
                            /*.Assign(out CounterLabel)*/,

                    }.Row(Row.Count).CenterHorizontal(),

                    new Button { Text = "Step" }
                        .Row(Row.ClickMeButton)
                        .Font(bold: true)
                        .CenterHorizontal()
                        .Invoke(button => button.Clicked += OnStepClicked)
                        //.BindCommand(nameof(ViewModel.ClickMeButtonCommand))
                    ,

                    new Image { Source = "dotnet_bot.png", WidthRequest = 250, HeightRequest = 310 }
                        .Row(Row.Image)
                        .CenterHorizontal()
                }
            };

        }


        enum Row { HelloWorld, Welcome, Count, ClickMeButton, Image }


        private void OnStepClicked(object sender, EventArgs e)
        {
            m_gb.Step();
            count = m_gb.cpu.reg.PC;

            CounterLabel.Text = $"PC:{count}";

            SemanticScreenReader.Announce(CounterLabel.Text);
        }
    }
}
