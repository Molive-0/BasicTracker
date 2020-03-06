using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyVersion("0.2.*")] //define the auto incrementing build version.

//! The main namespace for Basic Tracker. All code should be in this namespace.
namespace BasicTracker
{
    //! Global constants.
    /*! 
     * The G class includes a pile of constants and readonly values
     * which can be accessed from anywhere in the project. It allows for
     * easy editing of constants.
     */
    public static class G
    {
        public const int Vmove = 4; //!< Lines that are moved when the up or down arrows are pressed whilst holding ALT.
        public const int VmoveLarge = 8; //!< Lines that are moved when the up or down arrows are pressed whilst holding CTRL ALT.
        public const string defaultBar = "|-_- -- --- ---"; //!< The starting value for each note.
        public const int Hmove = 15; //!<  Lines that are moved when the left or right arrows are pressed whilst holding CTRL. Is defaultBar.Length.
        public const int depth = 32; //!< How many rows are in a pattern.
        public const int channels = 8; //!< How many channels there are, duh.
        public const int width = 15 * 8; //!< defaultBar.Length * channels, the columns needed for the screen.
        public static readonly char[] signature = { 'B', 'S', 'C', 'M' }; //!< The four characters at the very start of the file format. Used for file recognition, so that you can quickly see if a file is a Basic Tracker file.
        //! A ridiculous string which contains the starting state of the header for the GUI. It's really long because it if it reaches the end of a line it automatically goes to the next line and so I can't put a new line there. So it's just a really long line. It's a mess really, don't look at it.
        public const string header =
            "  +---------------+---------------+------------------------------+   +-----------------+---------------+                      " +
            "  | BASIC TRACKER | Version: {0:D1}.{1:D2} | Made by John \"Molive\" Hunter |   | EDITING PATTERN | {2:D5} / {3:D5} |   PRESS F1 FOR HELP  " +
            "  +---------------+---------------+------------------------------+   +-----------------+---------------+                      " +
            "  Song: {4} Author: {5} Octave: {6:D1} Tempo: {7:X2} Speed: {8:X2}  Instrument: {9:X2}   ";
        public static readonly Version version = typeof(Program).Assembly.GetName().Version; //!< Gets the autoincrementing version of the app. Used in checking the file formats.
        public const string helpString =
            "Note: a much better version of this is available at https://gist.github.com/Molive-0/29cebec10672d4e510a8beb47ed63961 \n" +
            "\n" +
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
        public const int MF_BYCOMMAND = 0x00000000; //!< Flags for following commands.
        public const int SC_CLOSE = 0xF060; //!< Code to remove the ability to close the app >:)
        public const int SC_MINIMIZE = 0xF020; //!< Code to remove the ability to minimize the app. Unused.
        public const int SC_MAXIMIZE = 0xF030; //!< Code to remove the ability to maximise the app.
        public const int SC_SIZE = 0xF000; //!< Code to remove the ability to resize the app.


    }
    //! Main program class
    /*! It doesn't do much.
     */
    class Program
    {
        private static Stopwatch frameTimer = new Stopwatch(); //!< How long the tick has been processing for, used to keep time in playback and to regulate the input processing.

        //! The main function.
        /*! Contains the main loop for the program. It's inside a "while not escape" so if you press escape the program should just exit.
         *  
         *  @param args Command line arguments
         */
        [STAThread]
        static void Main(string[] args)
        {
            Console.CancelKeyPress += closing;
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest; // Make ourselves really important and better than all your other programs which aren't as cool as this one.
            AudioSubsystem.init(); // We call the constructor on the Audio section after we've initialised other things.
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
                    waitOnTick();
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
        //! Function called on program exit.
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
