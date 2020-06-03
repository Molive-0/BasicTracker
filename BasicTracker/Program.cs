using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyVersion("0.3.*")] //define the auto incrementing build version.

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
        //! A ridiculous string which contains a replaceable version of the header for the GUI. Used during rendering.
        public const string header =
            "  +---------------+---------------+------------------------------+   +-----------------+---------------+                      " +
            "  | BASIC TRACKER | Version: {0:D1}.{1:D2} | Made by John \"Molive\" Hunter |   | EDITING PATTERN | {2:D5} / {3:D5} |   PRESS F1 FOR HELP  " +
            "  +---------------+---------------+------------------------------+   +-----------------+---------------+                      " +
            "  Song: {4} Author: {5} Octave: {6:D1} Tempo: {7:X2} Speed: {8:X2}  Instrument: {9:X2}   ";
        public static readonly Version version = typeof(Program).Assembly.GetName().Version; //!< Gets the autoincrementing version of the app. Used in checking the file formats.
        public static readonly Version lastcompatible = new Version("0.3"); //!< The earliest version of the format this tracker can open.
        public const int MF_BYCOMMAND = 0x00000000; //!< Flags for following commands.
        public const int SC_CLOSE = 0xF060; //!< Code to remove the ability to close the app >:)
        public const int SC_MINIMIZE = 0xF020; //!< Code to remove the ability to minimize the app. Unused.
        public const int SC_MAXIMIZE = 0xF030; //!< Code to remove the ability to maximise the app.
        public const int SC_SIZE = 0xF000; //!< Code to remove the ability to resize the app.


    }
    //! Main program class
    /*! It handles making the other three sections run at the same time in step with the music.
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
                    Application.DoEvents();
                    Consolex.RefreshKeys();
                    Consolex.handleMovement();
                    Consolex.handleScreen();
                    Driver.ExecuteRow();
                    waitOnTick();
                } while (Consolex.GetKey() != ConsoleKey.Escape);

                DialogResult d = MessageBox.Show("Do you wish to save your work before quitting?", "Quit", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (d == DialogResult.Yes)
                {
                    DialogResult x = Consolex.SaveSong();
                    exit = x == DialogResult.OK;
                }
                else if (d == DialogResult.No) 
                { 
                    exit = true; 
                }
            }
            AudioSubsystem.Shutdown();
            Console.Clear();
        }
        //! Function called on program exit.
        /*!
         * @param sender The source of the event.
         * @param e ConsoleCancelEventArgs object that contains the event data.
         */
        static void closing(object sender, ConsoleCancelEventArgs e)
        {
            DialogResult d = MessageBox.Show("Do you wish to save your work before quitting?", "Quit", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (d == DialogResult.Yes)
            {
                DialogResult x = Consolex.SaveSong();
                e.Cancel = x != DialogResult.OK;
            }
            else if (d == DialogResult.Cancel) 
            {
                e.Cancel = true;
            }
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
            {
                System.Threading.Thread.Sleep(rest);
            }
            else
            {
                System.Threading.Thread.Sleep(0);
            }
            frameTimer.Restart();
        }
    }
}
