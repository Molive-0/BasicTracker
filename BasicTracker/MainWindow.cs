using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicTracker
{
    static class MainWindow {
        private static Song song;
        static public void HandleMovement()
        {
            ConsoleKey? keyornull = Consolex.GetKey();
            if (keyornull.HasValue)
            {
                ConsoleKey key = keyornull.Value;
                if (key == ConsoleKey.UpArrow && Console.CursorTop >= G.Vmove + 9)
                {
                    Console.CursorTop -= G.Vmove;
                }
                else if (key == ConsoleKey.DownArrow && Console.CursorTop <= G.depth - G.Vmove + 8)
                {
                    Console.CursorTop += G.Vmove;
                }
                else if (key == ConsoleKey.LeftArrow && Console.CursorLeft > 3)
                {
                    if (Consolex.control && Console.CursorLeft >= G.Hmove)
                    {
                        Console.CursorLeft -= G.Hmove;
                    }
                    else { Console.CursorLeft -= 1; }
                    if (G.defaultBar[Console.CursorLeft % G.defaultBar.Length] != '-')
                    {
                        Console.CursorLeft -= 1;
                    }
                }
                else if (key == ConsoleKey.RightArrow && Console.CursorLeft <= G.width)
                {
                    if (Consolex.control && Console.CursorLeft <= G.width - 1 - G.Hmove)
                    {
                        Console.CursorLeft += G.Hmove;
                    }
                    else { Console.CursorLeft += 1; }
                    if (G.defaultBar[(Console.CursorLeft - 2) % G.defaultBar.Length] != '-')
                    {
                        Console.CursorLeft += 1;
                    }
                }
                else if (key == ConsoleKey.LeftArrow || key == ConsoleKey.RightArrow || key == ConsoleKey.UpArrow || key == ConsoleKey.DownArrow)
                {
                    Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop);
                }
                else if (Console.CursorLeft <= G.width - 1)
                {
                    //if (key == ConsoleKey.Enter)
                    //{
                    //    snap = CreateSnapshot();
                    //}
                    //if (key == ConsoleKey.Backspace)
                    //{
                    //    RestoreSnapshot(snap);
                    //}
                    if (key.ToString().Length == 1)
                    {
                        Console.Write(key);
                    }
                    if (Console.CursorLeft == G.width)
                    {
                        Console.CursorLeft -= 1;
                    }
                    if (G.defaultBar[(Console.CursorLeft - 2) % G.defaultBar.Length] != '-')
                    {
                        Console.CursorLeft += 1;
                    }
                }
            }
            //Console.WriteLine("Key is " + key);
        }
        static MainWindow()
        {
            song = new Song();
        }
    }
}
