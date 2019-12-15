using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyVersion("0.1.*")] //define the auto incrementing build version

//! The main namespace for Basic Tracker. All code should be in this namespace.
namespace BasicTracker
{
    //! Global constants
    /*! 
     * The G class includes a pile of constants and readonly values
     * which can be accessed from anywhere in the document. It allows for
     * easy editing of constants for the entire document.
     */
    public static class G 
    {
        public const int Vmove = 1; //!< Lines that are moved when the up or down arrows are pressed whilst holding CTRL
        public const string defaultBar = "|-_- -- --- ---"; //!< The starting value for each note
        public const int Hmove = 15; //!<  Lines that are moved when the left or right arrows are pressed whilst holding CTRL. Is defaultBar.Length
        public const int depth = 32; //!< How many rows are in a pattern
        public const int channels = 8; //!< How many channels there are, duh
        public const int width = 15 * 8; //!< defaultBar.Length * channels, the columns needed for the screen
        public static readonly char[] signature = { 'B', 'S', 'C', 'M' }; //!< The four characters at the very start of the file format. Used for file recognition, so that you can quickly see if a file is a Basic Tracker file.
        //! A ridiculous string which contains the starting state of the header for the GUI. It's really long because it if it reaches the end of a line it automatically goes to the next line and so I can't put a new line there. So it's just a really long line. It's a mess really, don't look at it.
        public const string header =  
@"  +---------------+---------------+------------------------------+   +-----------------+---------------+    
  | BASIC TRACKER | Version: 0.01 | Made by John ""Molive"" Hunter |   | EDITING PATTERN | 00001 / 00002 |   PRESS F1 FOR HELP
  +---------------+---------------+------------------------------+   +-----------------+---------------+    
  Song: ______________________________ Author: ______________________________ Octave: 4 Tempo: 00 Speed: 00  Length: 00m 00s
+----------------------------+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+| ORDERING (scroll at 00000) | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 | 1 | 2 |...|+----------------------------+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+¦ All Right!                                                                                                                 ¦";
        public static readonly Version version = typeof(Program).Assembly.GetName().Version; //!< Gets the autoincrementing version of the app. Used in checking the file formats.
        public const int MF_BYCOMMAND = 0x00000000; //!< Command for something, can't remember.
        public const int SC_CLOSE = 0xF060; //!< Code to remove the ability to close the app >:)
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
        static void Main(string[] args)
        {
            do
            {
                Consolex.RefreshKeys();
                Consolex.handleMovement();
                AudioSubsystem.Tick();
            } while (Consolex.GetKey() != ConsoleKey.Escape);
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
        
        //! Empty static console constructor
        /*! init run automatically before anything else.
         * Contains code to set up the screen for the first time, such as resising
         * and removing the ability to maximise. It also writes out the default 
         * starting screen.
         */
        static Consolex()
        {
            //Console.CursorVisible = false;
            Console.BufferHeight = 42;
            Console.BufferWidth = 126;
            for (int i = 0; i < G.depth; i++)
            {
                Console.SetCursorPosition(2, 9 + i);
                for (int j = 0; j < 8; j++)
                {
                    Console.Write(G.defaultBar);
                }
                Console.Write("|");
            }
            Console.SetCursorPosition(0, 0);
            Console.SetWindowSize(126, 42);
            Console.SetWindowPosition(0, 0);
            Console.Write(G.header);

            Console.SetCursorPosition(29, 1);
            Console.Write("{0}.{1}", G.version.Major.ToString("0"), G.version.Minor.ToString("00"));

            Console.SetCursorPosition(3, 9);
            Console.TreatControlCAsInput = true;

            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero) //remove resizing
            {
                DeleteMenu(sysMenu, G.SC_CLOSE, G.MF_BYCOMMAND);
                DeleteMenu(sysMenu, G.SC_MAXIMIZE, G.MF_BYCOMMAND);
                DeleteMenu(sysMenu, G.SC_SIZE, G.MF_BYCOMMAND);
            }
        }

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
        /*! It calls the handle movement code on the frontmost window - 
         * but there's only one window after the hybrid restructure.
         * It's now redundant in this case.
         */
        public static void handleMovement()
        {
            MainWindow.HandleMovement();
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

        //! Gets a key from the user and set lastkey to it.
        /* It wraps the Console.ReadKey instruction so that keys can do various things
         * they can't do in the original console, such at control moving far and F1 loading
         * help. If there is no key available it is non blocking, and will set it to null instead.
         */
        internal static void RefreshKeys()
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyinfo = Console.ReadKey(true);
                ConsoleKey key = keyinfo.Key;
                control = (keyinfo.Modifiers == ConsoleModifiers.Control);
                shift = (keyinfo.Modifiers == ConsoleModifiers.Shift);
                alt = (keyinfo.Modifiers == ConsoleModifiers.Alt);
                char keychar = keyinfo.KeyChar;
                lastkey = key;
            }
            else lastkey = null;
        }
    }

    class Song
    {
        private List<Pattern> patterns;
        private string songname;
        private string authorname;
        private List<ushort> orders;
        private ushort flags;
        private Version createdVersion;
        private Version compatibleVersion;
        private byte globalVol;
        private byte mixVol;
        private byte speed;
        private byte tempo;
        private byte[] chanVol;
        private byte[] chanPan;

        public Song()
        {
            patterns = new List<Pattern>();
            songname = "                              ";
            authorname = "                              ";
            orders = new List<ushort>();
            flags = 9;
            createdVersion = typeof(Program).Assembly.GetName().Version;
            compatibleVersion = createdVersion;
            globalVol = 0x7F;
            mixVol = 0x7F;
            speed = 0x10;
            tempo = 0x10;
            chanVol = new byte[] { 0x7f,0x7f,0x7f,0x7f,0x7f,0x7f,0x7f,0x7f };
            chanPan = new byte[] { 0x7f,0x7f,0x7f,0x7f,0x7f,0x7f,0x7f,0x7f };
        }

        public void loadfromfile(BinaryReader br)
        {
            if (br.ReadChars(4) != G.signature)
            {
                throw new IOException();
            }
            songname = br.ReadChars(30).ToString();
            authorname = br.ReadChars(30).ToString();
            orders = new List<ushort>(br.ReadUInt16());
            patterns = new List<Pattern>(br.ReadUInt16());
            uint[] patternPtr = new uint[patterns.Capacity];
            ushort tempver = br.ReadUInt16();
            createdVersion = new Version(
                tempver & 0x0F00 >> 12,
                tempver & 0x00FF);
            tempver = br.ReadUInt16();
            compatibleVersion = new Version(
                tempver & 0x0F00 >> 12,
                tempver & 0x00FF);
            flags = br.ReadUInt16();
            br.ReadUInt16(); //reserved bytes
            globalVol = br.ReadByte();
            mixVol = br.ReadByte();
            speed = br.ReadByte();
            tempo = br.ReadByte();
            for (int i = 0; i < 8; i++)
            {
                chanPan[i] = br.ReadByte();
            }
            for (int i = 0; i < 8; i++)
            {
                chanVol[i] = br.ReadByte();
            }
            for (int i = 0; i < orders.Capacity; i++)
            {
                orders[i] = br.ReadUInt16();
            }
            for (int i = 0; i < patternPtr.Length; i++)
            {
                patternPtr[i] = br.ReadUInt32();
            }
            foreach (uint i in patternPtr)
            {
                br.BaseStream.Seek(i,SeekOrigin.Begin);
                ushort patlen = br.ReadUInt16();
                ushort rowlen = br.ReadUInt16();
                patterns[(int)i] = decodePattern(patlen, rowlen, br.ReadBytes(patlen));
            }
        }

        private Pattern decodePattern(ushort patlen, ushort rowlen, byte[] data)
        {
            Pattern pattern = new Pattern();
            int[] prevMaskVars = new int[8];
            byte[] prevNote = new byte[8];
            byte[] prevInst = new byte[8];
            volumeParameter[] prevVolume = new volumeParameter[8];
            effectParameter[] prevEffect = new effectParameter[8];
            using (Stream st = new MemoryStream(data))
            {
                for (int row = 0; true; row++)
                {
                    Note note = new Note();
                    int channelVar = st.ReadByte();
                    if (channelVar == 0) break;
                    int channel = (channelVar - 1) & 7;
                    int maskVar;
                    if ((channelVar & 128) != 0)
                    {
                        maskVar = st.ReadByte();
                        prevMaskVars[channel] = maskVar;
                    }
                    else
                    {
                        maskVar = prevMaskVars[channel];
                    }
                    if ((maskVar & 1) != 0)
                    {
                        byte tempnote = (byte)st.ReadByte();
                        note.internal_note = tempnote;
                        prevNote[channel] = tempnote;
                    }
                    if ((maskVar & 2) != 0)
                    {
                        note.instrument = (byte)st.ReadByte();
                        prevInst[channel] = note.instrument;
                    }
                    if ((maskVar & 4) != 0)
                    {
                        note.volume = note.decodeVolume((byte)st.ReadByte());
                        prevVolume[channel] = note.volume;
                    }
                    if ((maskVar & 8) != 0)
                    {
                        effectParameter command = new effectParameter
                        {
                            type = (effectParameter.Type)st.ReadByte(),
                            value = (byte)st.ReadByte()
                        };
                        note.effect = command;
                        prevEffect[channel] = command;
                    }
                    if ((maskVar & 16) != 0)
                    {
                        note.internal_note = prevNote[channel];
                    }
                    if ((maskVar & 32) != 0)
                    {
                        note.instrument = prevInst[channel];
                    }
                    if ((maskVar & 64) != 0)
                    {
                        note.volume = prevVolume[channel];
                    }
                    if ((maskVar & 128) != 0)
                    {
                        note.effect = prevEffect[channel];
                    }
                    pattern.channels[channel].notes[row] = note;
                }
            }
            return pattern;
        }
    }
    class Pattern
    {
        public Channel[] channels = new Channel[G.channels];
        public Pattern()
        {
            for (int i = 0; i < G.channels; i++)
            {
                channels[i] = new Channel();
            }
        }
    }
    class Channel
    {
        public Note[] notes = new Note[G.depth];
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
            END = 254,  //!< end note, stop channel (===)
            EMPTY = 255 //!< undefined, yet to be input (-_-)
        }
        public N note { get; private set; }
        public int octave { get; private set; }
        public byte internal_note
        {
            set {
                note = (N)(value % 12);
                octave = value / 12;
                internal_note = value;
            }
            private get { return internal_note; }
        }
        public byte instrument;
        public volumeParameter volume;
        public effectParameter effect;

        public volumeParameter decodeVolume(byte vol)
        {
            volumeParameter param = new volumeParameter();
            if (vol <= 64)
            {
                param.type = volumeParameter.Type.V;
                param.value = vol;
            }
            else if (vol <= 74)
            {
                param.type = volumeParameter.Type.A;
                param.value = (byte)(vol - 64);
            }
            else if (vol <= 84)
            {
                param.type = volumeParameter.Type.B;
                param.value = (byte)(vol - 74);
            }
            else if (vol <= 94)
            {
                param.type = volumeParameter.Type.C;
                param.value = (byte)(vol - 84);
            }
            else if (vol <= 104)
            {
                param.type = volumeParameter.Type.D;
                param.value = (byte)(vol - 94);
            }
            else if (vol <= 114)
            {
                param.type = volumeParameter.Type.E;
                param.value = (byte)(vol - 104);
            }
            else if (vol <= 124)
            {
                param.type = volumeParameter.Type.F;
                param.value = (byte)(vol - 114);
            }
            else if (vol <= 127)
            {
                throw new Exception("Unknown volume value");
            }
            else if (vol <= 192)
            {
                param.type = volumeParameter.Type.P;
                param.value = (byte)(vol - 128);
            }
            else if (vol <= 202)
            {
                param.type = volumeParameter.Type.H;
                param.value = (byte)(vol - 192);
            }
            else if (vol <= 212)
            {
                param.type = volumeParameter.Type.V;
                param.value = (byte)(vol - 202);
            }
            else
            {
                throw new Exception("Unknown volume value");
            }
            return param;
        }
    }
    struct volumeParameter
    {
        public enum Type
        {
            A, //!< Fine Volume Slide Up
            B, //!< Fine Volume Slide Down
            C, //!< Volume Slide Up 
            D, //!< Volume Slide Down
            E, //!< Portamento Down
            F, //!< Portamento Up
            G, //!< Tone Portamento
            H, //!< Vibrato Depth
            P, //!< Set Panning
            V  //!< Set Volume
        }
        public Type type;
        public byte value;
    }
    struct effectParameter
    {
        public enum Type
        {
            A, //!< Set Speed 
            B, //!< Position Jump
            C, //!< Pattern Break
            D, //!< Volume Slide
            E, //!< Portamento Down
            F, //!< Portamento Up
            G, //!< Tone Portamento
            H, //!< Vibrato
            I, //!< Tremor
            J, //!< Arpeggio
            K, //!< Volume Slide + Vibrato 
            L, //!< Volume Slide + Tone Portamento
            M, //!< Set Channel Volume 
            N, //!< Channel Volume Slide
            O, 
            P, //!< Panning Slide
            Q, //!< Retrigger
            R, //!< Tremolo 
            S, //!< Special
            T, //!< Tempo 
            U, //!< Fine Vibrato 
            V, //!< Set Global Volume 
            W, //!< Global Volume Slide 
            X, //!< Set Panning 
            Y, //!< Panbrello
            Z, //!< Filter coefficients
            _0  //!< Parameter Extension

        }
        public Type type;
        public byte value;
    }
}
