using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private const string ROM_PATH = @"C:\Users\night\Downloads\DivisionTest.ch8";  // path to rom image to load
        public MainWindow()
        {
            InitializeComponent();
            CHIP8 chip8 = new CHIP8();  // temp for now
            byte[] romToLoad = File.ReadAllBytes(ROM_PATH);
            chip8.LoadProgram(romToLoad);
            chip8.BeginEmulation();
            Console.WriteLine("press enter to exit at any time");
            Console.ReadLine();  // stop when user presses a key
        }
    }
}
