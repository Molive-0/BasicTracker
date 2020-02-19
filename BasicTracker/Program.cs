using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Threading;

[assembly: AssemblyVersion("0.1.*")] //define the auto incrementing build version


//! The main namespace for Basic Tracker. All code should be in this namespace.
namespace BasicTracker
{
    //! Global constants
    /*! 
     * The G class includes a pile of constants and readonly values
     * which can be accessed from anywhere in the project. It allows for
     * easy editing of constants.
     */
    public static class G
    {
        public const int Vmove = 4; //!< Lines that are moved when the up or down arrows are pressed whilst holding ALT
        public const int VmoveLarge = 8; //!< Lines that are moved when the up or down arrows are pressed whilst holding CTRL ALT
        public const string defaultBar = "|-_- -- --- ---"; //!< The starting value for each note
        public const int Hmove = 15; //!<  Lines that are moved when the left or right arrows are pressed whilst holding CTRL. Is defaultBar.Length
        public const int depth = 32; //!< How many rows are in a pattern
        public const int channels = 8; //!< How many channels there are, duh
        public const int width = 15 * 8; //!< defaultBar.Length * channels, the columns needed for the screen
        public static readonly char[] signature = { 'B', 'S', 'C', 'M' }; //!< The four characters at the very start of the file format. Used for file recognition, so that you can quickly see if a file is a Basic Tracker file.
        //! A ridiculous string which contains the starting state of the header for the GUI. It's really long because it if it reaches the end of a line it automatically goes to the next line and so I can't put a new line there. So it's just a really long line. It's a mess really, don't look at it.
        public const string header =
            "  +---------------+---------------+------------------------------+   +-----------------+---------------+                      " +
            "  | BASIC TRACKER | Version: {0:D1}.{1:D2} | Made by John \"Molive\" Hunter |   | EDITING PATTERN | {2:D5} / {3:D5} |   PRESS F1 FOR HELP  " +
            "  +---------------+---------------+------------------------------+   +-----------------+---------------+                      " +
            "  Song: {4} Author: {5} Octave: {6:D1} Tempo: {7:X2} Speed: {8:X2}  Instrument: {9:X2}   ";
        public static readonly Version version = typeof(Program).Assembly.GetName().Version; //!< Gets the autoincrementing version of the app. Used in checking the file formats.
        public const string helpString =
            "Hello, and welcome to Basic Tracker, a music tracker program I have created for my A-Level.\n" +
            "Trackers are known for being able to render on a text only display - to this end, Basic Tracker\n" +
            "Is rendered only using the default C# console (There are a few parts in Windows Forms, due to\n" +
            "time constraints). This is meant to be a simplish implementation of a tracker program based on\n" +
            " the hardware of the Super Nintendo Entertainment System and the effects of Impulse Tracker.\n" +
            "As opposed to most trackers, the cursor moves over the pattern rather than the pattern moving\n" +
            "under the cursor.\n" +
            "This is not a tutorial on how to use a tracker, and so this help window will assume you know\n" +
            "how to use one already. For this tracker, all patterns are 32 lines long. The tempo is in BPM\n" +
            "and the speed is how many ticks happen per row. There are 5 preset instruments to choose from,\n" +
            "from 1-5: a sine wave, a square wave, a triangle wave, a sawtooth wave, and white noise. Instruments\n" +
            "6-A are LFO versions of instruments 1-5.\n" +
            "To move the cursor around the screen, use the arrow keys. Holding CTRL while moving allows you to\n" +
            "move whole columns at a time.\n" +
            "To start playback, press space. To stop, press space again. Pressing the enter key moves the playback\n" +
            "cursor to the position of the edit cursor, and plays that row by itself. To stop the sound system\n" +
            "outputting sound press Tab.\n" +
            "If you are on a pattern where the playback cursor is not, and you press space to start playback you\n" +
            "will be moved to the pattern where the cursor is. If you wanted to play that pattern pressing enter\n" +
            "first will try to move the cursor to you, and failing that move you to the cursor. The playback head\n" +
            "cannot reach patterns that are not in the ordering list.\n" +
            "The columns for each channel feature (from left to right) note + octave (red), instrument (blue),\n" +
            "volume (green) and effect (yellow).\n" +
            "The tracker can play any note from C_0 to B_8. This is inputted ProTracker style with the keyboard\n" +
            "mapped like a piano, with the first octave at Z to M and a second, higher octave at Q to U,\n" +
            "up to P as a third above the second octave. If you want to change octave simply use the numerical\n" +
            "keypad to get to a new one. If you don't have a keypad \";\" and \"'\" increment and decrement it.\n" +
            "Instrument is auto inserted for every note inserted. It is stored in hexadecimal and can be\n" +
            "over-written by simply selecting it using the cursor and pressing the desired hex digit on\n" +
            "the keyboard. Instrument zero is silence. The instrument that is inserted when a note is input\n" +
            "can be seen on the header bar, and changed with \"[\" and \"]\".\n" +
            "The volume was meant to have more purpose, but has since been delegated to only setting the\n" +
            "volume. To set the volume on a note, move the cursor to the left most green character and\n" +
            "press \"V\". The volume is stored in hexadecimal like the instrument; to set it move the cursor\n" +
            "to the hex digit you want to edit and press the desired new hex digit. To remove a volume set\n" +
            "press backspace on any of the green characters that makes it up.\n" +
            "The effect performs many different functions. The effects available are the same as that in\n" +
            "Impulse Tracker, except that Oxx has been replaced with OpenMPT's parameter extension. They\n" +
            "are set in the same way that the volume is set, the effect letter at the front and the\n" +
            "parameter in hexadecimal afterwards. Backspace deletes the effect.\n" +
            "To create more patterns make sure the cursor is in the pattern area and press \"CTRL-N\".\n" +
            "This creates a pattern and the end of the pattern list. If you have more than one pattern\n" +
            "you can view the others by pressing \"CTRL+[\" and \"CTRL+]\". A pattern can not be played\n" +
            "unless it exists somewhere within the ordering list. If you wish to delete a pattern, press\n" +
            "\"CTRL-Backspace\".\n" +
            "The ordering list is the order at which the playback cursor moves between patterns, a pattern\n" +
            "may appear more than once in the ordering list. To change to editing the ordering list instead\n" +
            "of the pattern, press F6. Pressing F6 again moves the cursor back.\n" +
            "Whilst editing the ordering list pressing left and right moves you forward and backward through\n" +
            "the list. Pressing Enter on any entry allows you to set it to a pattern number between 1 and\n" +
            "65535. If you want to add another item to the list pressing \"CTRL-N\" inserts one at the\n" +
            "current position.\n" +
            "If you wish to remove an item pressing backspace removes it from the list. If you wish to\n" +
            "remove the pattern as well (as long as it appears nowhere else) pressing \"CTRL-Backspace\"\n" +
            "removes both.\n" +
            "Whilst on the pattern area, pressing F1 brings up this help menu. pressing F2 allows you to\n" +
            "change the title of the song (max 30 characters), pressing F3 allows you to change the author\n" +
            "or musician of the song (max 30 characters), pressing F4 allows you to edit the tempo (first\n" +
            "hex character then second hex character, cannot be zero) and pressing F5 allows you to edit\n" +
            "the speed (same restrictions as tempo). I advise against high speeds as it significatly slows\n" +
            "down the playback rate.\n" +
            "If you wish to save your work, pressing \"CTRL-S\" opens a save dialog. Similarly pressing\n" +
            "\"CTRL-O\" allows you to open a file you have previously saved. NOTE: opening a file will\n" +
            "overwrite the currently opened song and any unsaved work will be lost!\n" +
            "To exit the program at any point press ESC. Be careful to not press it accidentally.\n" +
            "I'd appreciate feedback! You can reach me by email at moliveofscratch@gmail.com. I need\n" +
            "people to give advice on how to make this program better for the A-Level write-up so it'd\n" +
            "be very useful.\n" +
            "Thanks!\n" +
            "    ~Molive";
        public const int MF_BYCOMMAND = 0x00000000; //!< Flags for following commands
        public const int SC_CLOSE = 0xF060; //!< Code to remove the ability to close the app >:) Unused
        public const int SC_MINIMIZE = 0xF020; //!< Code to remove the ability to minimize the app. Unused
        public const int SC_MAXIMIZE = 0xF030; //!< Code to remove the ability to maximise the app
        public const int SC_SIZE = 0xF000; //!< Code to remove the ability to resize the app


    }
    //! Main program class
    /*! It doesn't do much.
     */
    class Program
    {
        //! The main function
        /*! Contains the main loop for the program. It's inside a "while not escape" so if you press escape the program should just exit
         *  
         *  @param args Command line arguments
         */
        [STAThread]
        static void Main(string[] args)
        {
            Console.CancelKeyPress += closing;
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest; // Make ourselves really important and better than all your other programs which aren't as cool as this one.
            AudioSubsystem.init(); // We call the constructor on the Audio section after we've initialised other things.
            //Driver.test();
            Driver.init();
            bool exit = false;
            while (!exit)
            {
                do
                {
                    Consolex.RefreshKeys();
                    Consolex.handleMovement();
                    Consolex.handleScreen();
                    Application.DoEvents();
                    Driver.ExecuteRow();
                    Consolex.waitOnTick();
                } while (Consolex.GetKey() != ConsoleKey.Escape);
                DialogResult d = MessageBox.Show("Do you wish to save your work before quitting?", "Quit", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (d == DialogResult.Yes)
                {
                    DialogResult x = Consolex.SaveSong();
                    exit = x == DialogResult.OK;
                }
                else if (d == DialogResult.No) { exit = true; }
            }
            AudioSubsystem.Shutdown();
            Console.Clear();
        }
        static void closing(object sender, ConsoleCancelEventArgs e)
        {
            DialogResult d = MessageBox.Show("Do you wish to save your work before quitting?", "Quit", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (d == DialogResult.Yes)
            {
                DialogResult x = Consolex.SaveSong();
                e.Cancel = x != DialogResult.OK;
            }
            else if (d == DialogResult.Cancel) { e.Cancel = true; }
            if (!e.Cancel)
            {
                AudioSubsystem.Shutdown();
                Console.Clear();
            }
        }
    }

    //! Gui handling code and rudimentary API
    /*! It helps the MainWindow create the GUI for the program.
     * It might not be totally needed but it helps organise the code.
     * I think.
     * It was originally meant to handle a lot of the GUI code for multiple
     * windows, like clipping and spawning and such. As there is only one
     * window this is redundant to an extent.
     */
    static class Consolex
    {
        public static bool control { get; private set; } //!< Cached bool for if the Control key is pressed. The setter is private.
        public static bool shift { get; private set; } //!< Cached bool for if the Shift key is pressed. The setter is private.
        public static bool alt { get; private set; } //!< Cached bool for if the Alt key is pressed. The setter is private.
        private static ConsoleKey? lastkey; //!< Cached value for last key pressed. It's private, but there's a public GetKey that returns it.

        private static int octave; //!< The current octave for inputting notes. It's also shown in the GUI
        private static int currentPattern; //!< The current pattern shown on screen
        private static int instrument; //!< The current instrument for inputting notes. It's also shown in the GUI
        private static int orderPosition; //!< How far scrolled over the order row is. It's also shown at the left of the bar.
        private static int currentRow; //!< Where the playback cursor is, used for rendering it to the screen.
        private static string screenMessage; //!< Thee messagee used for the message bar.
        private static Stopwatch screenTime = new Stopwatch(); //!< How long the message bar has been up, used to remove it after 3 seconds.
        private static Stopwatch frameTimer = new Stopwatch(); //!< How long the tick has been processing for, used to keep time in playback and to regulate the input processing.

        private static bool helpOpen = false; //!< Is the help window open? Used so that it doesn't open twice.

        //! Empty static console constructor
        /*! init run automatically before anything else.
         * Contains code to set up the screen for the first time, such as resising
         * and removing the ability to maximise. It also writes out the default 
         * starting screen.
         */
        static Consolex()
        {
            Console.Clear();
            //Console.CursorVisible = false;

            //Console.Write(Console.ReadKey().Key);
            /*for (int i = 0; i < G.depth; i++)
            {
                Console.SetCursorPosition(2, 9 + i);
                for (int j = 0; j < 8; j++)
                {
                    Console.Write(G.defaultBar);
                }
                Console.Write("|");
            }*/
            //Console.SetCursorPosition(0, 0);
            Console.SetWindowSize(126, 42);
            Console.BufferHeight = 42;
            Console.BufferWidth = 126;
            Console.SetWindowPosition(0, 0);
            Console.CursorSize = 100;

            /*Console.Write(G.header);

            Console.SetCursorPosition(29, 1);
            Console.Write("{0}.{1}", G.version.Major.ToString("0"), G.version.Minor.ToString("00"));
            */
            Console.SetCursorPosition(4, 10);
            Console.Title = "Basic Tracker :)";
            //Console.TreatControlCAsInput = true;

            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero) //remove resizing
            {
                DeleteMenu(sysMenu, G.SC_CLOSE, G.MF_BYCOMMAND);
                DeleteMenu(sysMenu, G.SC_MAXIMIZE, G.MF_BYCOMMAND);
                DeleteMenu(sysMenu, G.SC_SIZE, G.MF_BYCOMMAND);
            }

            octave = 4;
            currentPattern = 1;
            instrument = 1;
            screenMessage = "All Right!";
            RefreshScreen();
        }
        //! Converts a key to the ascii equivalent.
        /*! This is to try and fix a weird OEM bug in handling the keys pressed by the user, in that
         * sometimes the manufacturer of the keyboard changes up what all the keycodes are. We instead
         * call the windows API for converting it over because that fixes it in all cases. Hopefully.
         */
        public static char ToAscii(Keys key, Keys modifiers)
        {
            var outputBuilder = new StringBuilder(2);
            int result = ToAscii((uint)key, 0, GetKeyState(modifiers),
                                 outputBuilder, 0);
            if (result == 1)
                return outputBuilder[0];
            else
                return ' ';
        }

        //! Highest bit mask.
        private const byte HighBit = 0x80;
        //! Returns modifiers on a key, like shift or control. Used for ascii conversion.
        private static byte[] GetKeyState(Keys modifiers)
        {
            var keyState = new byte[256];
            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if ((modifiers & key) == key)
                {
                    keyState[(int)key] = HighBit;
                }
            }
            return keyState;
        }
        //! Windows API ascii conversion. See the other "ToAscii" function.
        [DllImport("user32.dll")]
        private static extern int ToAscii(uint uVirtKey, uint uScanCode,
                                          byte[] lpKeyState,
                                          [Out] StringBuilder lpChar,
                                          uint uFlags);
        //! For removing abilities
        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
        //! For getting handles
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        //! For getting handles
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        //! Handles the cursor movement
        /*! This handles basically everything in terms of keyboard input from the user. It's not a switch case because a lot of the
         * tests require multiple checks for things likee modifiers or other scenarios. There's extra comments in the code as to
         * what each specific section handles.
         */
        public static void handleMovement()
        {
            ConsoleKey? keyornull = Consolex.GetKey();
            if (keyornull.HasValue)
            {
                ConsoleKey key = keyornull.Value;
                if (key == ConsoleKey.Delete) key = ConsoleKey.Backspace;
                char oemFixer = ToAscii((Keys)key, Keys.None); //Ha, haha, haaaa... fixes a problem with keyboard manufacturers being awful. See the comment on the function for more details.
                if (Console.CursorTop == 5)
                {
                    // We're in the ordering, oh no. We sense this by looking how high the cursor is.
                    if (key == ConsoleKey.RightArrow)
                    {
                        // Moving fast along the list, to the right
                        if (control)
                        {
                            if (orderPosition >= Driver.orders.Count() - 10)
                            {
                                // We clamp the movement, so you still move to the end, just not 10.
                                orderPosition = Driver.orders.Count() - 1;
                                SetMessage("Cannot go further right along the ordering list");
                            }
                            else
                            {
                                orderPosition += 10;
                                RefreshScreen();
                            }
                        }
                        // Moving at normal speed to the right.
                        else
                        {
                            if (orderPosition == Driver.orders.Count() - 1)
                            {
                                SetMessage("Cannot go further right along the ordering list");
                            }
                            else
                            {
                                orderPosition++;
                                RefreshScreen();
                            }
                        }
                    }
                    if (key == ConsoleKey.LeftArrow)
                    {
                        // Moving fast along the list to the left.
                        if (control)
                        {
                            if (orderPosition <= 9)
                            {
                                orderPosition = 0;
                                SetMessage("Cannot go further left along the ordering list");
                            }
                            else
                            {
                                orderPosition -= 10;
                                RefreshScreen();
                            }
                        }
                        // Moving at normal speed to the left.
                        else
                        {
                            if (orderPosition == 0)
                            {
                                SetMessage("Cannot go further left along the ordering list");
                            }
                            else
                            {
                                orderPosition--;
                                RefreshScreen();
                            }
                        }
                    }
                    // Deleting orders.
                    if (key == ConsoleKey.Backspace)
                    {
                        // There can never be no orders, or the playback cursor can't be somewhere.
                        if (Driver.orders.Count() == 1)
                        {
                            SetMessage("Cannot delete the last order");
                        }
                        // If you delete an order whilst it is playing, the playback cursor will jump one to the right. So, you can't delete the last one whilst it is playing.
                        else if (Driver.playbackStarted && currentPattern == Driver.GetPatternCount())
                        {
                            SetMessage("Cannot delete the last order whilst the driver is playing it. Stop the playback first.");
                        }
                        else
                        {
                            // Control makes it so the pattern is deleted as well.
                            if (control)
                            {
                                // If we deleted a pattern that is in the ordering more than once we'd end up with dangling pointers.
                                if (Driver.orders.Where(x => x == Driver.orders[orderPosition]).Count() > 1)
                                {
                                    SetMessage("Cannot delete pattern that is referenced in the ordering more than once.");
                                }
                                else
                                {
                                    DialogResult d = MessageBox.Show("Are you SURE you want to delete BOTH this order and the related pattern?", "Delete order and pattern",
                                        MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                                    if (d == DialogResult.OK)
                                    {
                                        Driver.RemovePattern(Driver.orders[orderPosition]);
                                        Driver.orders.RemoveAt(orderPosition);
                                        // If we deleted from under ourselves, move us over.
                                        if (orderPosition >= Driver.orders.Count()) orderPosition--;
                                        if (currentPattern > Driver.GetPatternCount()) currentPattern--;
                                        RefreshScreen();
                                    }
                                }
                            }
                            else
                            {
                                Driver.orders.RemoveAt(orderPosition);
                                if (orderPosition >= Driver.orders.Count()) orderPosition--;
                                RefreshScreen();
                            }
                        }
                    }
                    // Switch back to normal mode.
                    if (key == ConsoleKey.F6)
                    {
                        Console.SetCursorPosition(4, 10);
                    }
                    // Make a new order. Inserts it at the current position
                    if (key == ConsoleKey.N && control)
                    {
                        Driver.orders.Insert(orderPosition, 1);
                        RefreshScreen();
                    }
                    // Edits the current order value.
                    if (key == ConsoleKey.Enter)
                    {
                        int posx = Console.CursorLeft;
                        int posy = Console.CursorTop;
                        ushort temp = Driver.orders[orderPosition];
                        // Set the value to the largest value temporarily to try avoiding rendering errors.
                        Driver.orders[orderPosition] = 10000;
                        SetMessage("Changing order at index " + orderPosition.ToString());
                        string input = Console.ReadLine();
                        // TryParse does most of the error checking for us.
                        if (ushort.TryParse(input, out ushort tryvalue))
                        {
                            if (tryvalue > Driver.GetPatternCount())
                            {
                                Driver.orders[orderPosition] = temp;
                                SetMessage("Pattern " + tryvalue + " does not exist.");
                            }
                            else
                            {
                                Driver.orders[orderPosition] = tryvalue;
                                SetMessage("Success");
                            }
                        }
                        else
                        {
                            Driver.orders[orderPosition] = temp;
                            SetMessage(input + " is not a number between 0 and 65535");
                        }
                        Console.SetCursorPosition(posx, posy);
                    }
                    // The spacebar still works whilst in the order mode :)
                    if (key == ConsoleKey.Spacebar)
                    {
                        Driver.TogglePlayback();
                    }
                    // As does tab.
                    if (key == ConsoleKey.Tab)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            AudioSubsystem.Stop(i);
                        }
                    }
                }
                // We're not in the ordering.
                else
                {
                    // This is the section for moving the cursor about the screen. It's entirely redone 
                    // from the console basic one, because you can only move to very certain places.
                    if (key == ConsoleKey.UpArrow && Console.CursorTop >= 11)
                    {
                        // For some reason the code here just doesn't work; pressing control and
                        // the up or down arrows does nothing at all. Thanks, Windows.
                        if (Consolex.control && alt && Console.CursorTop >= G.VmoveLarge + 10)
                        {
                            Console.CursorTop -= G.VmoveLarge;
                        }
                        else
                        if (Consolex.alt && Console.CursorTop >= G.Vmove + 10)
                        {
                            Console.CursorTop -= G.Vmove;
                        }
                        else
                            Console.CursorTop -= 1;
                    }
                    // Down arrow
                    else if (key == ConsoleKey.DownArrow && Console.CursorTop <= G.depth + 8)
                    {
                        if (Consolex.control && alt && Console.CursorTop >= G.depth - G.VmoveLarge + 9)
                        {
                            Console.CursorTop += G.VmoveLarge;
                        }
                        else
                        if (Consolex.alt && Console.CursorTop <= G.depth - G.Vmove + 9)
                        {
                            Console.CursorTop += G.Vmove;
                        }
                        else
                            Console.CursorTop += 1;
                    }
                    // Left arrow
                    else if (key == ConsoleKey.LeftArrow && Console.CursorLeft > 4)
                    {
                        // If the control key is held we move entire channels at once. If we're at the edge of the screen we ignore this and move normally.
                        if (Consolex.control && Console.CursorLeft >= G.Hmove+4)
                        {
                            Console.CursorLeft -= G.Hmove;
                        }
                        else { Console.CursorLeft -= 1; }
                        if (G.defaultBar[(Console.CursorLeft - 3) % G.defaultBar.Length] != '-')
                        {
                            Console.CursorLeft -= 1;
                        }
                    }
                    // Right arrow
                    else if (key == ConsoleKey.RightArrow && Console.CursorLeft <= G.width + 1)
                    {
                        if (Consolex.control && Console.CursorLeft < G.width - G.Hmove+4)
                        {
                            Console.CursorLeft += G.Hmove;
                        }
                        else { Console.CursorLeft += 1; }
                        if (G.defaultBar[(Console.CursorLeft - 3) % G.defaultBar.Length] != '-')
                        {
                            Console.CursorLeft += 1;
                        }
                    }
                    // These keys require that no modifiers are pressed.
                    if (!(control || shift || alt))
                    {
                        ProcessKey(key);

                        if (Console.CursorLeft == G.width + 3)
                        {
                            Console.CursorLeft -= 1;
                        }
                        if (Console.CursorTop >= 42)
                        {
                            Console.CursorTop -= 1;
                        }
                        if (G.defaultBar[(Console.CursorLeft - 3) % G.defaultBar.Length] != '-')
                        {
                            Console.CursorLeft += 1;
                        }

                        if (oemFixer == ';') // ;
                        {
                            octave--;
                            if (octave < 0) octave = 0;
                            RefreshScreen();
                        }
                        if (oemFixer == '\'') // ' (~?)
                        {
                            octave++;
                            if (octave > 8) octave = 8;
                            RefreshScreen();
                        }
                        if (oemFixer == '[') // [
                        {
                            instrument--;
                            if (instrument < 0) instrument = 0;
                            RefreshScreen();
                        }
                        if (oemFixer == ']') //  ]
                        {
                            instrument++;
                            if (instrument > 10) instrument = 10;
                            RefreshScreen();
                        }
                        ConsoleKey[] numkeys =
                        {
                    ConsoleKey.NumPad0,
                    ConsoleKey.NumPad1,
                    ConsoleKey.NumPad2,
                    ConsoleKey.NumPad3,
                    ConsoleKey.NumPad4,
                    ConsoleKey.NumPad5,
                    ConsoleKey.NumPad6,
                    ConsoleKey.NumPad7,
                    ConsoleKey.NumPad8,
                    };
                        if (numkeys.Contains(key))
                        {
                            octave = Array.IndexOf(numkeys, key);
                            RefreshScreen();
                        }
                        if (key == ConsoleKey.F3)
                        {
                            if (Driver.playbackStarted == false)
                            {
                                SetMessage("Editing the author...");
                                int x = Console.CursorLeft;
                                int y = Console.CursorTop;
                                Console.SetCursorPosition(47, 3);
                                string au = Console.ReadLine();
                                //Console.WriteLine(au);
                                //Console.ReadLine();
                                if (au.Length > 30) au = au.Substring(0, 30);
                                Driver.Author = au;
                                Console.CursorLeft = x;
                                Console.CursorTop = y;
                                RefreshScreen();
                            }
                            else
                            {
                                SetMessage("Cannot edit author whilst song is playing.");
                            }
                        }
                        if (key == ConsoleKey.F2)
                        {
                            if (Driver.playbackStarted == false)
                            {
                                SetMessage("Editing the title...");
                                int x = Console.CursorLeft;
                                int y = Console.CursorTop;
                                Console.SetCursorPosition(8, 3);
                                string au = Console.ReadLine();
                                //Console.WriteLine(au);
                                //Console.ReadLine();
                                if (au.Length > 30) au = au.Substring(0, 30);
                                Driver.Songname = au;
                                Console.CursorLeft = x;
                                Console.CursorTop = y;
                                RefreshScreen();
                            }
                            else
                            {
                                SetMessage("Cannot edit title whilst song is playing.");
                            }
                        }
                        if (key == ConsoleKey.F5)
                        {
                            if (Driver.playbackStarted == false)
                            {
                                SetMessage("Editing the starting speed...");
                                int x = Console.CursorLeft;
                                int y = Console.CursorTop;
                                Console.SetCursorPosition(105, 3);
                                Driver.Speed = (byte)GetHexInput(2);
                                if (Driver.Speed == 0) Driver.Speed++;
                                Console.CursorLeft = x;
                                Console.CursorTop = y;
                                if (Driver.Speed > 30)
                                {
                                    SetMessage("Warning: Speed is set rather high. This might induce slowdown on most computers.");
                                }
                                else RefreshScreen();
                            }
                            else
                            {
                                SetMessage("Cannot edit starting speed whilst song is playing.");
                            }
                        }
                        if (key == ConsoleKey.F4)
                        {
                            if (Driver.playbackStarted == false)
                            {
                                SetMessage("Editing the starting tempo...");
                                int x = Console.CursorLeft;
                                int y = Console.CursorTop;
                                Console.SetCursorPosition(95, 3);
                                Driver.Tempo = (byte)GetHexInput(2);
                                if (Driver.Tempo == 0) Driver.Tempo++;
                                Console.CursorLeft = x;
                                Console.CursorTop = y;
                                RefreshScreen();
                            }
                            else
                            {
                                SetMessage("Cannot edit starting tempo whilst song is playing.");
                            }
                        }
                        if (key == ConsoleKey.Enter)
                        {
                            Driver.RunOneLine(Console.CursorTop - 10, (ushort)currentPattern);
                            currentRow = Console.CursorTop - 10;
                            Consolex.RefreshScreen();
                        }
                        if (key == ConsoleKey.Spacebar)
                        {
                            Driver.TogglePlayback();
                        }
                        if (key == ConsoleKey.Tab)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                AudioSubsystem.Stop(i);
                            }
                        }
                        if (key == ConsoleKey.F6)
                        {
                            Console.SetCursorPosition(31, 5);
                        }
                    }
                    // These keys require that only control is pressed.
                    else if (!(shift || alt) && control)
                    {
                        if (key == ConsoleKey.N) //Ctrl+N
                        {
                            if (Driver.GetPatternCount() == 65536)
                            {
                                SetMessage("Cannot add more than 65536 total patterns, calm tf down.");
                            }
                            else
                            {
                                Driver.NewPattern();
                                SetMessage("Created a new pattern");
                            }
                        }
                        if (key == ConsoleKey.D) //Ctrl+D
                        {
                            if (Driver.GetPatternCount() == 65536)
                            {
                                SetMessage("Cannot add more than 65536 total patterns, calm tf down.");
                            }
                            else
                            {
                                int finalPattern = Driver.CopyPattern(currentPattern);
                                SetMessage("Copied pattern " + currentPattern.ToString() + " to pattern " + finalPattern);
                            }
                        }
                        if (key == ConsoleKey.Z) //Ctrl+Z
                        {
                            Driver.CopyChannel(currentPattern, (Console.CursorLeft - 3) / G.defaultBar.Length);
                            SetMessage("Copied channel to clipboard");
                        }
                        if (key == ConsoleKey.X)  //Ctrl+X
                        {
                            Driver.PasteChannel(currentPattern, (Console.CursorLeft - 3) / G.defaultBar.Length);
                            SetMessage("Pasted channel from clipboard");
                        }

                        if ((oemFixer == '[' | oemFixer == ']'))
                        {
                            if (Driver.playbackStarted)
                            {
                                SetMessage("Cannot change pattern whilst playback is started");
                            }
                            else
                            {
                                if (oemFixer == '[') //Ctrl+[
                                {
                                    currentPattern--;
                                }
                                if (oemFixer == ']') //Ctrl+]
                                {
                                    currentPattern++;
                                }
                                if (currentPattern == 0)
                                {
                                    currentPattern++;
                                    SetMessage("Cannot go lower than pattern zero");
                                }
                                else if (currentPattern > Driver.GetPatternCount())
                                {
                                    currentPattern--;
                                    SetMessage("Pattern " + (currentPattern + 1).ToString() + " does not exist");
                                }
                                else
                                {
                                    SetMessage("Pattern changed to pattern " + currentPattern.ToString());
                                }
                                Driver.AskForRow(currentPattern);
                            }
                        }
                        if (key == ConsoleKey.Backspace && control)
                        {
                            if (Driver.GetPatternCount() == 1)
                            {
                                SetMessage("Cannot delete the last pattern");
                            }
                            else if (Driver.playbackStarted)
                            {
                                SetMessage("Cannot delete a pattern whilst the driver is playing it. Stop the playback first.");
                            }
                            else
                            {
                                if (Driver.orders.Where(x => x == currentPattern).Count() > 0)
                                {
                                    SetMessage("Cannot delete pattern that is referenced in the ordering.");
                                }
                                else
                                {
                                    DialogResult d = MessageBox.Show("Are you SURE you want to delete this pattern?", "Delete pattern",
                                        MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                                    if (d == DialogResult.OK)
                                    {
                                        Driver.RemovePattern(currentPattern);
                                        if (currentPattern > Driver.GetPatternCount()) currentPattern--;
                                        RefreshScreen();
                                    }
                                }
                            }
                            //Console.WriteLine("Key is " + key);
                        }
                        if (key == ConsoleKey.O && control)
                        {
                            if (Driver.playbackStarted)
                            {
                                SetMessage("Cannot open song while playback is started. Stop playing the song first.");
                            }
                            else
                            {
                                OpenFileDialog f = new OpenFileDialog();
                                f.Filter = "Basic Tracker Files (*.bsctrk) | *.bsctrk";
                                f.Title = "Open a Basic Tracker song";
                                f.AddExtension = true;
                                f.CheckFileExists = true;
                                f.CheckPathExists = true;
                                f.DereferenceLinks = true;
                                DialogResult d = f.ShowDialog();
                                if (d == DialogResult.OK)
                                {
                                    currentPattern = 1;
                                    currentRow = 0;
                                    orderPosition = 0;
                                    Driver.LoadFile(f.OpenFile());
                                    RefreshScreen();
                                }
                            }
                        }
                        if (key == ConsoleKey.S && control)
                        {
                            SaveSong();
                        }
                    }
                    else if (shift && key == ConsoleKey.Z && !control && !alt)  //Shift+Z
                    {
                        Driver.RotateChannel(currentPattern, (Console.CursorLeft - 3) / G.defaultBar.Length);
                        SetMessage("Rotated channel");
                    }
                }
                if (key == ConsoleKey.F1)
                {
                    if (!helpOpen)
                        new Thread(() =>
                        {
                            helpOpen = true;
                            Thread.CurrentThread.IsBackground = true;
                            MessageBox.Show(G.helpString, "Basic Tracker Help", MessageBoxButtons.OK, MessageBoxIcon.None);
                            helpOpen = false;
                        }).Start();
                    else SetMessage("Help is already open");
                }
            }
        }

        public static DialogResult SaveSong()
        {
            SaveFileDialog f = new SaveFileDialog();
            f.Filter = "Basic Tracker Files (*.bsctrk) | *.bsctrk";
            f.Title = "Save a Basic Tracker song";
            f.AddExtension = true;
            f.CheckPathExists = true;
            f.DereferenceLinks = true;
            f.OverwritePrompt = true;
            f.RestoreDirectory = true;
            DialogResult d = f.ShowDialog();
            if (d == DialogResult.OK)
            {
                Driver.SaveFile(f.OpenFile());
            }
            return d;
        }

        private static int GetHexInput(int letters)
        {
            int input = 0;
            for (int i = letters - 1; i >= 0; i--)
            {
                ConsoleKey hexkey = Console.ReadKey().Key;
                if (hex.Contains(hexkey))
                {
                    input += Array.IndexOf(hex, hexkey) * (int)Math.Pow(16, i);
                }
            }
            return input;
        }

        public static void rowTo(int row)
        {
            currentRow = row;
            RefreshScreen();
        }
        public static void patternTo(int pattern)
        {
            currentPattern = pattern;
            RefreshScreen();
        }

        public static void SetMessage(string v)
        {
            screenMessage = v;
            screenTime.Restart();
            RefreshScreen();
        }

        private static readonly ConsoleKey[] keymap =
            {
                ConsoleKey.Z,
                ConsoleKey.S,
                ConsoleKey.X,
                ConsoleKey.D,
                ConsoleKey.C,
                ConsoleKey.V,
                ConsoleKey.G,
                ConsoleKey.B,
                ConsoleKey.H,
                ConsoleKey.N,
                ConsoleKey.J,
                ConsoleKey.M,
                ConsoleKey.Q,
                ConsoleKey.D2,
                ConsoleKey.W,
                ConsoleKey.D3,
                ConsoleKey.E,
                ConsoleKey.R,
                ConsoleKey.D5,
                ConsoleKey.T,
                ConsoleKey.D6,
                ConsoleKey.Y,
                ConsoleKey.D7,
                ConsoleKey.U,
                ConsoleKey.I,
                ConsoleKey.D9,
                ConsoleKey.O,
                ConsoleKey.D0,
                ConsoleKey.P,
            };

        private static readonly ConsoleKey[] numbers =
        {
                ConsoleKey.D0,
                ConsoleKey.D1,
                ConsoleKey.D2,
                ConsoleKey.D3,
                ConsoleKey.D4,
                ConsoleKey.D5,
                ConsoleKey.D6,
                ConsoleKey.D7,
                ConsoleKey.D8,
                ConsoleKey.D9,
            };

        private static readonly ConsoleKey[] hex =
        {
                ConsoleKey.D0,
                ConsoleKey.D1,
                ConsoleKey.D2,
                ConsoleKey.D3,
                ConsoleKey.D4,
                ConsoleKey.D5,
                ConsoleKey.D6,
                ConsoleKey.D7,
                ConsoleKey.D8,
                ConsoleKey.D9,
                ConsoleKey.A,
                ConsoleKey.B,
                ConsoleKey.C,
                ConsoleKey.D,
                ConsoleKey.E,
                ConsoleKey.F,
            };

        private static readonly ConsoleKey[] alpha =
        {
                ConsoleKey.A,
                ConsoleKey.B,
                ConsoleKey.C,
                ConsoleKey.D,
                ConsoleKey.E,
                ConsoleKey.F,
                ConsoleKey.G,
                ConsoleKey.H,
                ConsoleKey.I,
                ConsoleKey.J,
                ConsoleKey.K,
                ConsoleKey.L,
                ConsoleKey.M,
                ConsoleKey.N,
                ConsoleKey.O,
                ConsoleKey.P,
                ConsoleKey.Q,
                ConsoleKey.R,
                ConsoleKey.S,
                ConsoleKey.T,
                ConsoleKey.U,
                ConsoleKey.V,
                ConsoleKey.W,
                ConsoleKey.X,
                ConsoleKey.Y,
                ConsoleKey.Z,
            };

        private static void ProcessKey(ConsoleKey key)
        {

            int channel = (Console.CursorLeft - 3) / G.defaultBar.Length;
            int row = (Console.CursorTop - 10);
            int interchannel = (Console.CursorLeft - 3) % G.defaultBar.Length;
            bool refresh = false;
            switch (interchannel)
            {
                case 1:
                    if (keymap.Contains(key))
                    {
                        Driver.ChangeNoteAt(currentPattern, channel, row, (byte)(Array.IndexOf(keymap, key) + 12 * octave)); refresh = true;
                        Driver.ChangeInstrumentAt(currentPattern, channel, row, false, (byte)instrument);
                    }
                    else if (key == ConsoleKey.OemPlus)
                    {
                        Driver.EndNoteAt(currentPattern, channel, row); refresh = true;
                    }
                    break;
                case 3:
                    if (numbers.Contains(key))
                    {
                        Driver.ChangeOctaveAt(currentPattern, channel, row, Array.IndexOf(numbers, key)); refresh = true;
                    }
                    break;
                case 5:
                    if (hex.Contains(key))
                    {
                        Driver.ChangeInstrumentAt(currentPattern, channel, row, true, (byte)Array.IndexOf(hex, key)); refresh = true;
                    }
                    break;
                case 6:
                    if (hex.Contains(key))
                    {
                        Driver.ChangeInstrumentAt(currentPattern, channel, row, false, (byte)Array.IndexOf(hex, key)); refresh = true;
                    }
                    break;
                case 8:
                    if (key == ConsoleKey.V)
                    {
                        Driver.SetVolumeAt(currentPattern, channel, row); refresh = true;
                    }
                    break;
                case 9:
                    if (hex.Contains(key))
                    {
                        Driver.ChangeVolumeAt(currentPattern, channel, row, true, (byte)Array.IndexOf(hex, key)); refresh = true;
                    }
                    break;
                case 10:
                    if (hex.Contains(key))
                    {
                        Driver.ChangeVolumeAt(currentPattern, channel, row, false, (byte)Array.IndexOf(hex, key)); refresh = true;
                    }
                    break;
                case 12:
                    if (alpha.Contains(key))
                    {
                        Driver.ChangeEffectTypeAt(currentPattern, channel, row, key.ToString()[0]); refresh = true;
                    }
                    break;
                case 13:
                    if (hex.Contains(key))
                    {
                        Driver.ChangeEffectParamAt(currentPattern, channel, row, true, (byte)Array.IndexOf(hex, key)); refresh = true;
                    }
                    break;
                case 14:
                    if (hex.Contains(key))
                    {
                        Driver.ChangeEffectParamAt(currentPattern, channel, row, false, (byte)Array.IndexOf(hex, key)); refresh = true;
                    }
                    break;
            }

            if (key == ConsoleKey.Backspace)
            {
                switch (interchannel)
                {
                    case 1:
                    case 2:
                    case 3:
                        Driver.ClearNoteAt(currentPattern, channel, row);
                        refresh = true;
                        break;
                    case 8:
                    case 9:
                    case 10:
                        Driver.ClearVolumeAt(currentPattern, channel, row);
                        refresh = true;
                        break;
                    case 12:
                    case 13:
                    case 14:
                        Driver.ClearEffectAt(currentPattern, channel, row);
                        refresh = true;
                        break;
                }
            }
            if (refresh)
            {
                RefreshScreen();
                //Console.CursorTop += 1;
            }
        }

        private static void RefreshScreen()
        {
            string screen = String.Format(G.header,
                G.version.Major, G.version.Minor,
                currentPattern,
                Driver.GetPatternCount(),
                Driver.Songname.PadRight(30, '_'), Driver.Author.PadRight(30, '_'),
                octave,
                Driver.Tempo, Driver.Speed,
                instrument);

            List<string> orders = new List<string>();
            int len = 0;
            int pos = orderPosition;
            int ordersCount = Driver.orders.Count();
            while (len < 92)
            {
                if (pos == ordersCount) break;
                string order = Driver.orders[pos].ToString();
                orders.Add(order);
                pos++;
                len += 3 + order.Length;
            }
            if (len > 92)
                orders.RemoveAt(orders.Count() - 1);

            string ordersTemp = "+----------------------------+";
            foreach (string s in orders)
            {
                ordersTemp += new string('-', s.Length + 2) + "+";
            }
            ordersTemp = ordersTemp.PadRight(125, '-') + '+';
            screen += ordersTemp;

            string ordersCenter = String.Format("| ORDERING (scroll at {0:D5}) |", orderPosition);
            foreach (string s in orders)
            {
                ordersCenter += String.Format(" {0} |", s);
            }
            if (pos != ordersCount)
                ordersCenter += new string(' ', (125 - ordersCenter.Length - 3) / 2) + "...";

            ordersCenter = ordersCenter.PadRight(125);
            ordersCenter += '|';
            screen += ordersCenter + ordersTemp;

            screen += "[ " + screenMessage.PadRight(123) + "]";
            screen += new string(' ', 126);
            screen += "   |  Channel  0  |  Channel  1  |  Channel  2  |  Channel  3  |  Channel  4  |  Channel  5  |  Channel  6  |  Channel  7  |  ";
            string[] patterns = Driver.GetPatternAsString(currentPattern);
            string screen2 = "";
            for (int i = 0; i < patterns.Length; i++)
            {
                screen2 += i.ToString("d2") + " " + patterns[i] + "  ";
            }

            int tempx = Console.CursorLeft;
            int tempy = Console.CursorTop;

            //Console.OpenStandardOutput().Write(Encoding.ASCII.GetBytes(screen), 0, screen.Length);

            //Console.Clear();
            //Console.SetWindowSize(126, 42);
            //Console.SetWindowPosition(0, 0);
            //Console.Write(screen);
            //Console.SetCursorPosition(tempx, tempy);

            SafeFileHandle h = CreateFile("CONOUT$", 0x40000000, 2, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            if (!h.IsInvalid)
            {
                CharInfo[] buf = new CharInfo[126 * 42];
                SmallRect rect = new SmallRect() { Left = 0, Top = 0, Right = 126, Bottom = 42 };
                short[] noteColours = {
                    (int)ConsoleColor.White,
                    (int)ConsoleColor.Red,
                    (int)ConsoleColor.Red,
                    (int)ConsoleColor.Red,
                    (int)ConsoleColor.White,
                    (int)ConsoleColor.Blue,
                    (int)ConsoleColor.Blue,
                    (int)ConsoleColor.White,
                    (int)ConsoleColor.Green,
                    (int)ConsoleColor.Green,
                    (int)ConsoleColor.Green,
                    (int)ConsoleColor.White,
                    (int)ConsoleColor.Yellow,
                    (int)ConsoleColor.Yellow,
                    (int)ConsoleColor.Yellow,
                };

                short[] lineColours = new short[126];
                int i;
                for (i = 3; i < 124; i++)
                {
                    lineColours[i] = (short)(noteColours[(i - 3) % (noteColours.Length)] | 0x0000);
                }
                lineColours[0] = (int)ConsoleColor.Blue;
                lineColours[1] = (int)ConsoleColor.Blue;

                for (i = 0; i < screen.Length; i++)
                {
                    buf[i].Attributes = 15;
                    buf[i].Char.UnicodeChar = screen[i];
                }
                for (i = 0; i < screen2.Length; i++)
                {
                    buf[i + screen.Length].Attributes =
                        (short)(((i % 126 <= 1 && (i / 126) % 4 == 0) ?
                        (i / 126) % 16 == 0 ? (int)ConsoleColor.Green :
                        (int)ConsoleColor.Red : lineColours[i % 126])
                        | (i / 126 == currentRow ? 0x80 : 0));

                    buf[i + screen.Length].Char.UnicodeChar = screen2[i];
                }

                bool b = WriteConsoleOutput(h, buf,
                    new Coord() { X = 126, Y = 42 },
                    new Coord() { X = 0, Y = 0 },
                    ref rect);
            }
        }

        //! returns the last key from the user
        /*! It's written this way so that for each loop of the mainloop the same key is given to everything.
         * Otherwise some keys are missed or are read by the wrong function.
         * 
         * @return Either the ConsoleKey of the latest key pressed, or null from lastkey.
         */
        public static ConsoleKey? GetKey()
        {
            return lastkey;
        }

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string fileName,
            [MarshalAs(UnmanagedType.U4)] uint fileAccess,
            [MarshalAs(UnmanagedType.U4)] uint fileShare,
            IntPtr securityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
            [MarshalAs(UnmanagedType.U4)] int flags,
            IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteConsoleOutput(
            SafeFileHandle hConsoleOutput,
            CharInfo[] lpBuffer,
            Coord dwBufferSize,
            Coord dwBufferCoord,
            ref SmallRect lpWriteRegion);

        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;

            public Coord(short X, short Y)
            {
                this.X = X;
                this.Y = Y;
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct CharUnion
        {
            [FieldOffset(0)] public char UnicodeChar;
            [FieldOffset(0)] public byte AsciiChar;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharInfo
        {
            [FieldOffset(0)] public CharUnion Char;
            [FieldOffset(2)] public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        //! Gets a key from the user and sets lastkey to it.
        /* It wraps the Console.ReadKey instruction so that keys can do various things
        * they can't do in the original console, such as control moving far and F1 loading
        * help. If there is no key available it is non blocking, and will set it to null instead.
        */
        public static void RefreshKeys()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyinfo = Console.ReadKey(true);
                ConsoleKey key = keyinfo.Key;
                control = ((keyinfo.Modifiers & ConsoleModifiers.Control) > 0);
                shift = ((keyinfo.Modifiers & ConsoleModifiers.Shift) > 0);
                alt = ((keyinfo.Modifiers & ConsoleModifiers.Alt) > 0);
                char keychar = keyinfo.KeyChar;
                lastkey = key;
            }
            else lastkey = null;
        }

        //! Handles things to do with the screen that need to be done every tick that aren't to do with movement.
        public static void handleScreen()
        {
            if (screenTime.ElapsedMilliseconds > 3000)
            {
                screenMessage = "All Right!";
                RefreshScreen();
                screenTime.Reset();
            }
            if (Console.BufferWidth != 126 | Console.BufferHeight != 42) // Auto fix the screen size if the user tries to mess with it.
            {
                Console.SetWindowSize(126, 42);
                Console.SetBufferSize(126, 42);
                Console.SetWindowPosition(0, 0);
            }
        }

        //! Waits for the next tick. 
        /*! Pauses the entire program until the next tick should happen on the audio driver.
         * This keeps both the processing and the driver in time and not using all of the CPU
         * time - it does have the slight side effect that if you set the speed and tick rate
         * really low the program grinds to a halt and is basically unusable, but just don't
         * do that.
         */
        public static void waitOnTick()
        {
            frameTimer.Stop();
            TimeSpan timeForOne = TimeSpan.FromSeconds((15.0 / Driver.Tempo) / Driver.Speed);
            int rest = (timeForOne - frameTimer.Elapsed).Milliseconds;
            if (rest > 0)
                System.Threading.Thread.Sleep(rest);
            else
                System.Threading.Thread.Sleep(0);
            frameTimer.Restart();
        }
    }
}
