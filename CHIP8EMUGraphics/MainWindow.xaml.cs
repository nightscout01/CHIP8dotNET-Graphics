using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CHIP8EMUGraphics
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ROM_PATH = @"C:\Users\night\Downloads\maze.ch8";  // path to rom image to load
        private CHIP8Graphics graphicsStuff;
        private CHIP8 chip8;
        public MainWindow()
        {
            InitializeComponent();
            graphicsStuff = new CHIP8Graphics();
            ScreenImage.Source = graphicsStuff.Screen;  // hopefully that will update as needed
            chip8 = new CHIP8(graphicsStuff);  // temp for now
            //byte[] romToLoad = File.ReadAllBytes(ROM_PATH);
            //chip8.LoadProgram(romToLoad);
            //chip8.BeginEmulation();
            //Console.WriteLine("press enter to exit at any time");
            Console.ReadLine();  // stop when user presses a key
        }

        private void Test_Click(object sender, RoutedEventArgs e)
        {
            //graphicsStuff.GraphicsTestDriver();
        }

        private void Open_ROM_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ".ch8",
                //|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif
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
