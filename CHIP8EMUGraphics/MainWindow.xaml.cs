// Copyright Maurice Montag 2019
// All Rights Reserved
// See LICENSE file for more information

using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace CHIP8EMUGraphics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CHIP8Graphics graphicsStuff;
        private readonly CHIP8 chip8;
        public MainWindow()
        {
            InitializeComponent();
            graphicsStuff = new CHIP8Graphics();
            ScreenImage.Source = graphicsStuff.Screen;  // hopefully that will update as needed
            chip8 = new CHIP8(graphicsStuff);  // temp for now
            Console.ReadLine();  // stop when user presses a key
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            //graphicsStuff.GraphicsTestDriver();
            // was for debugging, now unused
        }

        private void Open_ROM_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".ch8",
                //|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif  <- general style of filters
                Filter = "CHIP8 files (*.ch8)|*.ch8|All files (*.*)|*.*"  // filter shows txt files and other files if selected
            };  // open a file dialog
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                byte[] romToLoad = File.ReadAllBytes(dlg.FileName);
                chip8.LoadProgram(romToLoad);
                chip8.BeginEmulation();
            }
        }
    }
}
