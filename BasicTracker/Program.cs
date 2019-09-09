using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicTracker
{
    class Program
    {
        const int Vmove = 1;
        const string defaultBar = "|-_- -- --- ---";
        const int Hmove = 15; // defaultBar.Length;
        const int depth = 32;
        const int width = 15*8;
        private static bool control;
        private static bool shift;
        private static bool alt;
        static void Main(string[] args)
        {
            //Console.CursorVisible = false;

            Console.BufferHeight = depth+4;
            Console.BufferWidth = width+1;
            Console.SetCursorPosition(0, 3);
            for (int i = 0; i < depth; i++)
            {
                for (int j = 0; j < width/defaultBar.Length; j++)
                {
                    Console.Write(defaultBar);
                }
                Console.WriteLine("");
            }
            Console.SetCursorPosition(1, 3);
            while (true)
            {
                Console.SetWindowSize(width + 1, depth + 4);
                Console.SetWindowPosition(0, 0);
                ConsoleKey key = GetKey();
                if (key == ConsoleKey.UpArrow && Console.CursorTop >= Vmove+3)
                {
                    Console.CursorTop -= Vmove;
                }
                else if (key == ConsoleKey.DownArrow && Console.CursorTop <= depth-Vmove-1+3)
                {
                    Console.CursorTop += Vmove;
                }
                else if (key == ConsoleKey.LeftArrow && Console.CursorLeft >= 2)
                {
                    if (control && Console.CursorLeft >= Hmove)
                    {
                        Console.CursorLeft -= Hmove;
                    }
                    else { Console.CursorLeft -= 1; }
                    if (defaultBar[Console.CursorLeft % defaultBar.Length] != '-')
                    {
                        Console.CursorLeft -= 1;
                    }
                }
                else if (key == ConsoleKey.RightArrow && Console.CursorLeft <= width-2)
                {
                    if (control && Console.CursorLeft <= width -1 - Hmove)
                    {
                        Console.CursorLeft += Hmove;
                    } else { Console.CursorLeft += 1; }
                    if (defaultBar[Console.CursorLeft % defaultBar.Length] != '-')
                    {
                        Console.CursorLeft += 1;
                    }
                } else if (key == ConsoleKey.LeftArrow || key == ConsoleKey.RightArrow || key == ConsoleKey.UpArrow || key == ConsoleKey.DownArrow)
                {
                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop);
                }
                else if (Console.CursorLeft <= width - 1)
                {
                    if (key.ToString().Length == 1)
                    {
                        Console.Write(key);
                    }
                    if (Console.CursorLeft == width)
                    {
                        Console.CursorLeft -= 1;
                    }
                    if (defaultBar[Console.CursorLeft % defaultBar.Length] != '-')
                    {
                        Console.CursorLeft += 1;
                    }
                }
                //Console.WriteLine("Key is " + key);
            }
        }

        private static ConsoleKey GetKey()
        {
            ConsoleKeyInfo keyinfo = Console.ReadKey(true);
            ConsoleKey key = keyinfo.Key;
            control = (keyinfo.Modifiers == ConsoleModifiers.Control);
            shift = (keyinfo.Modifiers == ConsoleModifiers.Shift);
            alt = (keyinfo.Modifiers == ConsoleModifiers.Alt);
            char keychar = keyinfo.KeyChar;
            return key;
        }
    }

    class Pattern
    {

    }
    class Channel
    {

    }
    class Note
    {
        public enum N
        {
            C_,
            Db,
            D_,
            Eb,
            E_,
            F_,
            Gb,
            G_,
            Ab,
            A_,
            Bb,
            B_,
        }
        private N note;
        private byte instrument;
        private Parameter volume;
        private Parameter effect;
    }
    struct Parameter
    {
        public char type;
        public byte value;
    }
}
