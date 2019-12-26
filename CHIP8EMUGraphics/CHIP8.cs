using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CHIP8EMUGraphics
{
    class CHIP8
    {
        private readonly Dictionary<byte, Key> keyMap;  // our map from the weird CHIP8 4x4 hex keyboard to actual windows keys 
        private readonly byte[] RAM;  // our emulated system RAM
        private byte[,] GraphicsArray;  // our 2D graphics array
        private readonly byte[] key;  // the weird hex keyboard this thing uses
        private const ushort PROGRAM_START = 0x200;  // usual start location of code in program ROM. (512) in decimal
        private const uint CLOCK_SPEED = 2000;  // our emulated clock speed in Hz
        private readonly CPU emuCPU;  // our emulated CHIP-8 CPU
        private readonly System.Timers.Timer cycleTimer;
        private readonly CHIP8Graphics graphicsAdapter;
       // private BitmapSource graphics;


        // PUT SOME GRAPHICS STUFF HERE (maybe)?

        public CHIP8(CHIP8Graphics graphics)
        {
            keyMap = new Dictionary<byte, Key>()  // only have the directional keys hooked up right now
            {
                [0x1] = Key.D1,
                [0x2] = Key.D2,
                [0x3] = Key.D3,
                [0x4] = Key.Q,
                [0x5] = Key.W,
                [0x6] = Key.E,
                [0x7] = Key.A,
                [0x8] = Key.S,
                [0x9] = Key.D,
                [0xA] = Key.Z,
                [0xB] = Key.C,
                [0xC] = Key.D4,
                [0xD] = Key.R,
                [0xE] = Key.F,
                [0xF] = Key.V,
                [0x0] = Key.X,


            };
            graphicsAdapter = graphics;
            RAM = new byte[4096];  // initialize the byte array that holds our emulated memory 
            GraphicsArray = new byte[64, 32];  // CHIP-8 graphics are a black and white 64x32 grid
            key = new byte[16];  // CHIP-8 has a strange hex based keyboard, this is where we store the keypresses.
            cycleTimer = new System.Timers.Timer();
            cycleTimer.AutoReset = true;
            double intervalTime = 1; //1.0 / CLOCK_SPEED * 1000.0;  // translate from frequency to period and multiply by 1000 to get the interval time in ms
            cycleTimer.Interval = intervalTime;
            cycleTimer.Elapsed += CycleEvent;  // add our event
            cycleTimer.Enabled = true;  // enable the timer
            emuCPU = new CPU(RAM, keyMap,graphicsAdapter);
        }

        public void CycleEvent(object source, ElapsedEventArgs e)
        {
            if (emuCPU.drawflag)
            {
                emuCPU.drawflag = false;
                if(Application.Current == null)
                {
                    return;
                }
                Application.Current.Dispatcher.Invoke(() => {
                    //Console.WriteLine("RENDERING");
                    graphicsAdapter.RenderByteArray(emuCPU.graphicsArray);
                });
            }
            emuCPU.EmulateCycle();
           // Console.WriteLine("CYCLE");
        }

        public void TimedEmulation()
        {
            //loop at 100HZ WHILE()
            // 500 Hz means 500 ops/second. 


            // ENDWHILE
        }

        public void BeginEmulation()
        {
            cycleTimer.Start();
            Console.WriteLine("started timer");
            //Thread.Sleep(2000);
            //while (true)
            //{
            //    emuCPU.EmulateCycle(); // :OOOO
            //    Thread.Sleep(10);
            //}
        }

        /// <summary>
        /// Loads the given CHIP8 program ROM, as a byte array, into our emulated memory (likely at PROGRAM_START)
        /// </summary>
        /// <param name="progROM">The CHIP8 program ROM, as a byte[]</param>
        /// <returns>true if the program was successfully loaded, false if there was an error</returns>
        public bool LoadProgram(byte[] progROM)
        {
            Array.Copy(progROM, 0, RAM, PROGRAM_START,progROM.Length);  // copy the contents of our program ROM into our emulated RAM at the "memory address" PROGRAM_START
            emuCPU.InitializeCPU(PROGRAM_START);  // initialize the CPU, passing in the start address of our program ROM
            return true;  // TODO, implement error checking?
        }
    }
}
