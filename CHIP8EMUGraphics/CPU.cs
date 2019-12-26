﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace CHIP8EMUGraphics
{
    class CPU
    {
        private const bool DEBUG = true;  // when enabled, print out dissasembled OP codes
        public byte[] memory;  // the emulated RAM to use for this emulator
        private byte delay_timer;  // the closest thing to an interupt that a CHIP-8 system has
        private byte sound_timer;  // when this one osn't zero, there's a beep
        private System.Diagnostics.Stopwatch stopWatch;
        private long timestamp;
        private byte[] Vreg;
        public byte[] graphicsArray;
        private Dictionary<byte, Key> keyboardMapping;
        private Dictionary<Key, byte> keyValueMapping;
        public bool drawflag;
        // ushort is an unsigned 16 bit integer (VS complains when using UInt16)
        private ushort I;  // index register, 16 bits  (NOT THE INSTRUCTION POINTER)
        private ushort PC;  // program counter/instruction pointer, 16 bits

        // both timers when set decrease at 60Hz until they
        // are these timers actually CPU things or should they be somewhere else?

        //private ushort[] stack = new ushort[16];  // maximum stack depth of the CHIP-8 specification is 16, we need to have a stack array with enough space for 16
        // 16 bit addresses

        private Stack<ushort> programStack;
        private ushort SP;  // the stack pointer.
        private CHIP8Graphics graphicsAdapter;
        private ushort currentOPCode;  // CHIP-8 opcodes are 16 bits long (maybe we don't need this as a global)
        private readonly Random rnd;  // a random number generator

        //  private Dictionary<int, Func<ushort, bool>> lookupTable = new Dictionary<int, Func<ushort, bool>>();

        private byte[] fontSET;

        public CPU(byte[] RAM, Dictionary<byte, Key> keyboardMapping, CHIP8Graphics graphicsAdapter)
        {
            this.fontSET = new byte[] {
    0xF0, 0x90, 0x90, 0x90, 0xF0, //0
    0x20, 0x60, 0x20, 0x20, 0x70, //1
    0xF0, 0x10, 0xF0, 0x80, 0xF0, //2
    0xF0, 0x10, 0xF0, 0x10, 0xF0, //3
    0x90, 0x90, 0xF0, 0x10, 0x10, //4
    0xF0, 0x80, 0xF0, 0x10, 0xF0, //5
    0xF0, 0x80, 0xF0, 0x90, 0xF0, //6
    0xF0, 0x10, 0x20, 0x40, 0x40, //7
    0xF0, 0x90, 0xF0, 0x90, 0xF0, //8
    0xF0, 0x90, 0xF0, 0x10, 0xF0, //9
    0xF0, 0x90, 0xF0, 0x90, 0x90, //A
    0xE0, 0x90, 0xE0, 0x90, 0xE0, //B
    0xF0, 0x80, 0x80, 0x80, 0xF0, //C
    0xE0, 0x90, 0x90, 0x90, 0xE0, //D
    0xF0, 0x80, 0xF0, 0x80, 0xF0, //E
    0xF0, 0x80, 0xF0, 0x80, 0x80  //F
};
            memory = RAM;  // take our emulated RAM, with our program already loaded in
            //this.clock_speed = clock_speed;  // set our clock speed, so we can know when to decrement our timers
            stopWatch = new System.Diagnostics.Stopwatch();
            this.keyboardMapping = keyboardMapping;  // set our keyboard map to the one passed in.
            keyValueMapping = this.keyboardMapping.ToDictionary(kp => kp.Value, kp => kp.Key);
            delay_timer = 0;
            sound_timer = 0;
            this.graphicsAdapter = graphicsAdapter;
            drawflag = false;
            graphicsArray = new byte[2048];
            timestamp = 0;
            programStack = new Stack<ushort>(16);  // this is implemented as an array under the hood, I hope it's fast enough for our use.
            Vreg = new byte[16];  // there are 16 "V" registers, it's way easier for everyone involved if we just store them in an array.
            rnd = new Random();
        }

        /// <summary>
        /// Initializes the CPU, getting it ready to execute the instructions in program ROM
        /// </summary>
        /// <returns>true if the initialization was successful, false if there was an error</returns>
        public bool InitializeCPU(ushort programStartLocation)
        {
            PC = programStartLocation;
            stopWatch.Start();  // start our stopwatch class 
            currentOPCode = 0;
            I = 0;
            SP = 0;
            for (int i = 0; i < 80; ++i)  // load font
            {
                memory[i] = fontSET[i];
            }

            // clear anything else that needs clearing
            return true;  // TODO
        }

        public void EmulateCycle()  // this function will be called A LOT, for purposes of speed, we're not checking if the program is initialized
        {
            /**
             * taking the bitwise OR of two 8 bit values, with one shifted 8 bits to the left, gives us our 16 bit
             * opcode that we desire. C# casts non-int/non-long data types to int before shifting (for some reason), 
             * we know that we'll only have 16 bits, so we cast it back.
             */
            currentOPCode = (ushort)(memory[PC] << 8 | memory[PC + 1]);
            // now that we have retrieved the current opcode, we need to see what it actually does! (hmmmm a bunch of switch statements or a lookup table?)
            // need to register callbacks for the graphics commands probably, so we can signal the CHIP8 class to update the display.

            DecodeAndExecuteOPCode(currentOPCode);  // hoooo boi here we go!
            if (DEBUG)
            {
                DisassembleOpCode(currentOPCode);
            }
            // We need to decrement the two counters every 60 Hz, that's once every 0.0166666667 seconds or once every 16.6666667 ms
            double difference = stopWatch.ElapsedMilliseconds - timestamp;  // we get the difference in time between the last time we executed a EmulateCycle, and now
            if (difference == 0)  // if we're emulating super quick, just return I guess???
            {
                return;
            }
            double numberOfTimesToDecrment = difference / 16.6666667;   /// ooooo boy
            while (numberOfTimesToDecrment > 0)
            {
                decrementCounters();
                numberOfTimesToDecrment--;
            }
            //stopWatch.Reset();
        }


        private void decrementCounters()
        {
            if (sound_timer > 0)
            {
                sound_timer--;
            }
            if (delay_timer > 0)
            {
                delay_timer--;
            }
        }
        /*
         * Most opcodes we can just look at the first four bits, there are some cases we can not, so we'll have two different lookup attempts, just see the code
         */
        private void DecodeAndExecuteOPCode(ushort opCode)

        {
            if (sound_timer != 0)
            {
                // BEEP SOUND
                //Console.Beep();
                //Console.Beep(2)
                Console.WriteLine("BEEEP");
            }
            // there's getting to be too many of these, once this switch table is done, copy and paste them into their parts, maybe use a function? maybe use a #DEFINE????

            // first four Bits = most significant four bits
            ushort firstFourBits = (ushort)(opCode & 0xF000);  // I found the annoying thing about C#, bitwise operands don't work without a cast when using non int/long data types
            ushort lastTwelveBits = (ushort)(opCode & 0x0FFF);  // usually the address, I just kinda hate all the casting going on, so we just store this here for later use
            byte lastEightBits = (byte)(opCode & 0x00FF);  // too much casting
            byte lastFourBits = (byte)(opCode & 0x000F);
            ushort VRegXIdentifier = (ushort)((opCode & 0x0F00) >> 8);  // 0xKXNN, where X is the register identifier for the register V0-VF (and k is a number)
            ushort VRegYIdentifier = (ushort)((opCode & 0x00F0) >> 4);  // 0xKXYN, where X is the register identifier for the register V0-VF (and k is a number)
            // for the operations where registers X and Y are used, these two below values reference the byte in those registers 
            ref byte Vx = ref Vreg[VRegXIdentifier];  // I don't know if this will work to be honest (I tested it, it does!) [but is it fast enough?]
            ref byte Vy = ref Vreg[VRegYIdentifier];
            // these are the values in V[VregXIdentifier] and V[..
            switch (firstFourBits)
            {
                case 0x0000:  // special case for 0x0 opcodes

                    switch (opCode & 0x000F)
                    {
                        case 0x0000:  //0x00E0, clears the screen
                            for (int i = 0; i < 2048; ++i)
                            {
                                graphicsArray[i] = 0;
                            }
                            drawflag = true;
                            PC += 2;
                            break;
                        case 0x000E:  // 0x00EE, return from subroutine
                            SP--;  // decrement stack pointer
                            PC = programStack.Pop();  // pop the IP/PC
                            break;
                    }
                    break;
                case 0x1000:  // opcode 0x1NNN, unconditional jump to address NNN
                    PC = lastTwelveBits;  // we set the program counter to address NNN
                    break;
                case 0x2000:  // opcode 0x2NNN, call subroutine at NNN
                    programStack.Push(PC);  // push the current PC/Instruction pointer onto the stack
                    ++SP;  // increment the stack pointer (I'll see if we actually need this later)  
                    PC = lastTwelveBits;  // then set the PC/IP to NNN
                    break;
                case 0x3000:  // opcode 0x3XNN, skips the next instruction if VX = NN, since each instruction is 2 bytes, we skip 4 bytes ahead (increment IP/PC by 4)
                    if (Vreg[VRegXIdentifier] == lastEightBits)
                    {
                        PC += 4; // skip the next instruction, usually bypassing a jump statement
                    }
                    else
                    {
                        PC += 2;  // if not, then we just go to the next instruction, incrementing the IP/PC by 2.
                    }
                    break;
                case 0x4000:  // opcode 0x4XNN, skips the next instruction if VX != NN, since each instruction is 2 bytes, we skip 4 bytes ahead (increment IP/PC by 4)
                    if (Vreg[VRegXIdentifier] != lastEightBits)
                    {
                        PC += 4; // skip the next instruction, usually bypassing a jump statement
                    }
                    else
                    {
                        PC += 2;  // if not, then we just go to the next instruction, incrementing the IP/PC by 2.
                    }
                    break;
                case 0x5000:  // opcode 0x5XY0, skips the next instruction if Vx = Vy, since each instruction is 2 bytes, we skip 4 bytes ahead (increment IP/PC by 4)
                    if (Vreg[VRegXIdentifier] == Vreg[VRegYIdentifier])
                    {
                        PC += 4; // skip the next instruction, usually bypassing a jump statement
                    }
                    else
                    {
                        PC += 2;  // if not, then we just go to the next instruction, incrementing the IP/PC by 2.
                    }
                    break;
                case 0x6000:  // opcode 0x6XNN, sets Vx to NN
                    Vreg[VRegXIdentifier] = lastEightBits;
                    PC += 2;  // increment IP/PC
                    break;
                case 0x7000:  // opcode 0x7XNN (Vx+=NN) [adds N to Vx] (carry flag is not changed)
                    Vreg[VRegXIdentifier] += lastEightBits;
                    PC += 2;  // incremtn IP/PC
                    break;
                case 0x8000:  // there's a LOT of opcodes that start with 8, they all use two registers and do compare and bitwise operations
                    switch (lastFourBits)
                    {
                        case 0: // set Vx = Vy
                            Vx = Vy;
                            PC += 2;
                            break;
                        case 1:  // set Vx = Vx | Vy
                            Vx = (byte)(Vx | Vy);  // ew more forced casting
                            PC += 2;
                            break;
                        case 2:  // set Vx = Vx & Vy
                            Vx = (byte)(Vx & Vy);
                            PC += 2;
                            break;
                        case 3:  // set Vx = Vx ^ Vy
                            Vx = (byte)(Vx ^ Vy);
                            PC += 2;
                            break;
                        case 4:  // set Vx = Vx + Vy
                            if (Vy > (0xFF - Vx))
                            {
                                Vreg[0xF] = 1;  // set carry flag true
                            }
                            else
                            {
                                Vreg[0xF] = 0;  // set carry flag false
                            }
                            Vx += Vy;
                            PC += 2;  // increment program counter
                            break;
                        case 5:  // set Vx = Vx - Vy
                            if (Vy <= Vx)
                            {
                                Vreg[0xF] = 1;  // set noBorrow flag true
                            }
                            else
                            {
                                Vreg[0xF] = 0;  // set noBorrow flag false
                            }
                            Vx = Vx -= Vy;
                            PC += 2;
                            break;
                        case 6:  // store LSB of Vx in VF, shift Vx to the right by 1.
                            Vreg[0xF] = (byte)(Vx & 0x0001);
                            Vx = (byte)(Vx >> 1);
                            PC += 2;
                            break;
                        case 7:  // set Vx=Vy-Vx (set flags if noborrow or not)
                            if (Vx <= Vy)
                            {
                                Vreg[0xF] = 1;   // set noborrow flag true
                            }
                            else
                            {
                                Vreg[0xF] = 0;  // set noborrow flag false
                            }
                            Vx -= Vy;  // completely forgot to implement this opcode
                            PC += 2;
                            break;
                        case 0xE:  // store MSB of Vx in VF, shift Vx to the left by 1
                            Vreg[0xF] = (byte)((Vx & 0x80) >> 7);
                            Vx = (byte)(Vx << 1);
                            PC += 2;
                            break;
                        default:
                            throw new InvalidOperationException("Unknown CHIP-8 0x8 prefixed opcode detected");
                    }
                    break;
                case 0x9000:  // opcode 0x9XY0, skips next instruction if Vx != Vy
                    if (Vx != Vy)
                    {
                        PC += 4;
                    }
                    else
                    {
                        PC += 2;
                    }
                    break;
                case 0xA000:  // opcode 0xANNN, set I to NNN
                    I = lastTwelveBits;  // remove the A specifier to get the memory address NNN
                    PC += 2;  // increment the program counter by 2 (2 bytes per opcode) (so we read the next opcode on the next cycle)
                    break;
                case 0xB000:  // opcode 0xBNNN, jumps to the address V_0 + NNN
                    PC = (ushort)(lastTwelveBits + Vreg[0]);
                    break;
                case 0xC000:  // opcode 0xCXNN, sets Vx to the result of a bitwise and operation on a random number and NN.
                    Vx = (byte)(lastEightBits & rnd.Next(255));
                    PC += 2;
                    break;
                case 0xD000:  // opcode 0xDXYN
                    /* Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels. 
                     * Each row of 8 pixels is read as bit-coded starting from memory location I; I value doesn’t change after 
                     * the execution of this instruction. As described above, 
                     * VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, 
                     * and to 0 if that doesn’t happen
                     */
                    // Do some fancy signaling to notify graphics subsystem.
                    //ushort x = V[(opcode & 0x0F00) >> 8];
                    // ushort y = V[(opcode & 0x00F0) >> 4];
                    ushort height = (ushort)(opCode & 0x000F);
                    ushort pixel;

                    Vreg[0xF] = 0;
                    for (int yline = 0; yline < height; yline++)
                    {
                        pixel = memory[I + yline];
                        //   Console.WriteLine("pixel is " + pixel);
                        // Console.WriteLine("coords are " + Vx + " " + Vy);
                        for (int xline = 0; xline < 8; xline++)
                        {
                            if ((pixel & (0x80 >> xline)) != 0)
                            {
                                if (graphicsArray[(Vx + xline + ((Vy + yline) * 64))] == 1)
                                {
                                    Vreg[0xF] = 1;
                                }
                                graphicsArray[Vx + xline + ((Vy + yline) * 64)] ^= 1;
                            }
                        }
                    }
                    drawflag = true;
                    PC += 2;
                    break;
                case 0xE000:  // opcode 0xEXNN
                    Console.WriteLine("LOOKING FOR KEYPRESS");
                    Console.WriteLine(Vx);
                    switch (lastEightBits)
                    {
                        case 0x9E:  // skips the next instruction if key stored in Vx is pressed.
                            // do fancy keypress handling here
                            Key pressedKey;
                            bool validMapping = keyboardMapping.TryGetValue(Vx, out pressedKey);
                            if (!validMapping)  // if we don't have a mapping for the hex key in Vx, we definitely can't have pressed it, so we "return false"
                            {
                                PC += 2;
                                break;
                            }
                            if (Keyboard.IsKeyDown(pressedKey))  // check to see if the requested key is pressed
                            {
                                PC += 4;  // if it is, skip the next instruction
                                Console.WriteLine("KEY PRESS");
                            }
                            else
                            {
                                PC += 2;
                            }
                            break;
                        case 0xA1:  // skips the next instruction if key stored in Vx isn't pressed.
                            Key pressedKey2;  // yes kind of gross with the code duplication but there's a lot of weird scoping issues with these switch statements
                            bool validMapping2 = keyboardMapping.TryGetValue(Vx, out pressedKey2);
                            bool isUp = false;

                            Application.Current.Dispatcher.Invoke(() => { isUp = Keyboard.IsKeyUp(pressedKey2); });

                            if (!validMapping2 || isUp)
                            {
                                PC += 2;
                            }
                            else
                            {
                                Console.WriteLine("KEY PRESS");
                                PC += 4;
                            }
                            break;
                    }
                    break;
                case 0xF000:  // opcode 0xFXNN (a bunch of stuff)
                    switch (lastEightBits)
                    {
                        case 0x07: // gets the display timer value, and stores it in Vx
                            Vx = delay_timer;
                            PC += 2;
                            break;
                        case 0x0A:  // A key press is awaited, and then stored in VX. (Blocking Operation. All instruction halted until next key event)
                            byte keyDown = 0;
                            bool pressed = false;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                byte b = 0;
                                foreach (Key k in keyValueMapping.Keys)
                                {
                                    if (Keyboard.IsKeyDown(k))
                                    {
                                        pressed = true;
                                        keyValueMapping.TryGetValue(k, out b);
                                        keyDown = b;
                                        break;
                                    }
                                }

                            });

                            if (pressed)  // if the key is pressed, then we can increment the program counter and get out of this wait 
                                          // condition.
                            {
                                Vx = keyDown;
                                PC += 2;
                            }
                            break;
                        case 0x15:
                            delay_timer = Vx;  // set the delay timer to VX
                            PC += 2;
                            break;
                        case 0x18:
                            sound_timer = Vx;  // set the sound timer to Vx
                            PC += 2;
                            break;
                        case 0x1E:  // adds Vx to I
                            I += Vx;
                            if (I + Vx > 0xFFF)  // undocumented feature of CHIP-8 Hardware (according to wikipedia)
                            {
                                Vreg[0xF] = 1;
                            }
                            else
                            {
                                Vreg[0xF] = 0;
                            }
                            PC += 2;
                            break;
                        case 0x29:  // 	Sets I to the location of the sprite for the character in VX. Characters 0-F 
                            // (in hexadecimal) are represented by a 4x5 font.
                            I = (ushort)(Vx * 5);  // lets just pray that this works
                            PC += 2;
                            break;
                        case 0x33:  // BCD stuff, i have no idea what this "actually" does, but 
                            /*
                             * Stores the binary-coded decimal representation of VX, 
                             * with the most significant of three digits at the address in I, 
                             * the middle digit at I plus 1, and the least significant digit at I plus 2. 
                             * (In other words, take the decimal representation of VX, place the hundreds digit 
                             * in memory at location in I, the tens digit at location I+1, and the ones digit at 
                             * location I+2.)
                             * 
                             */
                            memory[I] = (byte)(Vx / 100);
                            memory[I + 1] = (byte)((Vx / 10) % 10);
                            memory[I + 2] = (byte)((Vx % 100) % 10);
                            PC += 2;
                            break;
                        case 0x55:  // Stores V0 to VX (including VX) in memory starting at address I. 
                            // The offset from I is increased by 1 for each value written, but I itself is left unmodified.
                            for (int i = 0; i <= VRegXIdentifier; ++i)
                            {
                                memory[I + i] = Vreg[i];
                            }
                            // I += (ushort)(((opCode & 0x0F00) >> 8) + 1);
                            PC += 2;
                            break;
                        case 0x65: //Fills V0 to VX (including VX) with values from memory starting at address I. 
                            // The offset from I is increased by 1 for each value written, but I itself is left unmodified.
                            for (int i = 0; i <= VRegXIdentifier; ++i)
                            {
                                Vreg[i] = memory[I + i];
                            }
                            //  I += (ushort)(((opCode & 0x0F00) >> 8) + 1);
                            PC += 2;
                            break;
                    }
                    break;
                default:
                    throw new InvalidOperationException("Unknown CHIP-8 opcode detected");
            }

        }

        private void DisassembleOpCode(ushort opCode)
        {
            // short[] temp = Encoding.GetBytes()
            // two byte OPCODE for this system
            // this code was kinda haphazardly ported from C++, it's here only as a debugging tool to see which
            // the actual names of the operations instead of just hex.
            byte[] code = new byte[2];
            // byte2 = (byte)(number >> 8);
            // byte1 = (byte)(number & 255);
            code[0] = (byte)(opCode >> 8);  // MSB
            code[1] = (byte)(opCode & 255);  // LSB
            byte firstnib = (byte)(code[0] >> 4);

            switch (firstnib)  // public domain CHIP8 disassembler, ported to C#
            {
                case 0x0:
                    switch (code[1])
                    {
                        case 0xe0: Console.WriteLine("CLS"); break;
                        case 0xee: Console.WriteLine("RTS"); break;  // return
                        default: Console.WriteLine("UNKNOWN 0: {0:X}", opCode); break;
                    }
                    break;
                case 0x1: Console.WriteLine("{0} ${1:X}{2:X}", "JUMP", code[0] & 0xf, code[1]); break;
                case 0x2: Console.WriteLine("{0} ${1:X}{2:X}", "CALL", code[0] & 0xf, code[1]); break;
                case 0x3: Console.WriteLine("{0} V{1:X},#${2:X}", "SKIP.EQ", code[0] & 0xf, code[1]); break;
                case 0x4: Console.WriteLine("{0} V{1:X},#${2:X}", "SKIP.NE", code[0] & 0xf, code[1]); break;
                case 0x5: Console.WriteLine("{0} V{1:X},V{2:X}", "SKIP.EQ", code[0] & 0xf, code[1] >> 4); break;
                case 0x6: Console.WriteLine("{0} V{1:X},#${2:X}", "MVI", code[0] & 0xf, code[1]); break;
                case 0x7: Console.WriteLine("{0} V{1:X},#${2:X}", "ADI", code[0] & 0xf, code[1]); break;
                case 0x8:
                    {
                        byte lastnib = (byte)(opCode & 0x000F);
                        switch (lastnib)
                        {
                            case 0: Console.WriteLine("{0} V{1:X},V{2:X}", "MOV.", code[0] & 0xf, code[1] >> 4); break;
                            case 1: Console.WriteLine("{0} V{1:X},V{2:X}", "OR.", code[0] & 0xf, code[1] >> 4); break;
                            case 2: Console.WriteLine("{0} V{1:X},V{2:X}", "AND.", code[0] & 0xf, code[1] >> 4); break;
                            case 3: Console.WriteLine("{0} V{1:X},V{2:X}", "XOR.", code[0] & 0xf, code[1] >> 4); break;
                            case 4: Console.WriteLine("{0} V{1:X},V{2:X}", "ADD.", code[0] & 0xf, code[1] >> 4); break;
                            case 5: Console.WriteLine("{0} V{1:X},V{2:X},V{3}", "SUB.", code[0] & 0xf, code[0] & 0xf, code[1] >> 4); break;
                            case 6: Console.WriteLine("{0} V{1:X},V{2:X}", "SHR.", code[0] & 0xf, code[1] >> 4); break;
                            case 7: Console.WriteLine("{0} V{1:X},V{2:X},V{3}", "SUB.", code[0] & 0xf, code[1] >> 4, code[1] >> 4); break;
                            case 0xe: Console.WriteLine("{0} V{1:X},V{2:X}", "SHL.", code[0] & 0xf, code[1] >> 4); break;
                            default: Console.WriteLine("UNKNOWN 8"); break;
                        }
                    }
                    break;
                case 0x9: Console.WriteLine("{0} V{1:X},V{2:X}", "SKIP.NE", code[0] & 0xf, code[1] >> 4); break;
                case 0xa: Console.WriteLine("{0} I,#${1:X}{2:X}", "MVI", code[0] & 0xf, code[1]); break;
                case 0xb: Console.WriteLine("{0} ${1:X}{2:X}(V0)", "JUMP", code[0] & 0xf, code[1]); break;
                case 0xc: Console.WriteLine("{0} V{1:X},#${2:X}", "RNDMSK", code[0] & 0xf, code[1]); break;
                case 0xd: Console.WriteLine("{0} V{1:X},V{2:X},#${3:X}", "SPRITE", code[0] & 0xf, code[1] >> 4, code[1] & 0xf); break;
                case 0xe:
                    switch (code[1])
                    {
                        case 0x9E: Console.WriteLine("{0} V{1:X}", "SKIPKEY.Y", code[0] & 0xf); break;
                        case 0xA1: Console.WriteLine("{0} V{1:X}", "SKIPKEY.N", code[0] & 0xf); break;
                        default: Console.WriteLine("UNKNOWN E"); break;
                    }
                    break;
                case 0xf:
                    switch (code[1])
                    {
                        case 0x07: Console.WriteLine("{0} V{1:X},DELAY", "MOV", code[0] & 0xf); break;
                        case 0x0a: Console.WriteLine("{0} V{1:X}", "KEY", code[0] & 0xf); break;
                        case 0x15: Console.WriteLine("{0} DELAY,V{1:X}", "MOV", code[0] & 0xf); break;
                        case 0x18: Console.WriteLine("{0} SOUND,V{1:X}", "MOV", code[0] & 0xf); break;
                        case 0x1e: Console.WriteLine("{0} I,V{1:X}", "ADI", code[0] & 0xf); break;
                        case 0x29: Console.WriteLine("{0} I,V{1:X}", "SPRITECHAR", code[0] & 0xf); break;
                        case 0x33: Console.WriteLine("{0} (I),V{1:X}", "MOVBCD", code[0] & 0xf); break;
                        case 0x55: Console.WriteLine("{0} (I),V0-V{1:X}", "MOVM", code[0] & 0xf); break;
                        case 0x65: Console.WriteLine("{0} V0-V{1:X},(I)", "MOVM", code[0] & 0xf); break;
                        default: Console.WriteLine("UNKNOWN F"); break;
                    }
                    break;
            }
        }
    }
}
