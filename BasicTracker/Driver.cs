using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BasicTracker
{
    static public class Driver
    {
        static Song song;
        static int order;
        static int row;
        static int PlayRows;
        static int tickCounter;
        private static Channel clipboardChannel = new Channel();

        static double[] chanPreGain = new double[] { 255, 255, 255, 255, 255, 255, 255, 255 };
        static double[] chanPostGain = new double[] { 1, 1, 1, 1, 1, 1, 1, 1 };
        static double[] chanPitch = new double[] { 440, 440, 440, 440, 440, 440, 440, 440 };
        static double[] chanPan = new double[] { 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f, 0x7f };
        static double masterGain = 255;
        static bool[] surround = new bool[8];

        private static byte[] previousEffects = new byte[26];
        private static double[] previousNote = new double[] { 440, 440, 440, 440, 440, 440, 440, 440 };
        private static double[] previousVolume = new double[] { 1, 1, 1, 1, 1, 1, 1, 1 };
        public static bool playbackStarted = false;

        private static int[] waveformTablePointer = new int[8];
        private static int[] vibratoWaveform = new int[8];
        private static int[] tremoloWaveform = new int[8];
        private static int[] panbrelloWaveform = new int[8];
        private static int nonResetTickCounter;

        private static int patternLoop;
        private static int loopPoint;
        private static int rowLoop;

        private static double[,] waveformTables = new double[4, 256];
        static readonly double[] pitches =
        {
        16.35,
        17.32,
        18.35,
        19.45,
        20.6,
        21.83,
        23.12,
        24.5,
        25.96,
        27.5,
        29.14,
        30.87,
        32.7,
        34.65,
        36.71,
        38.89,
        41.2,
        43.65,
        46.25,
        49,
        51.91,
        55,
        58.27,
        61.74,
        65.41,
        69.3,
        73.42,
        77.78,
        82.41,
        87.31,
        92.5,
        98,
        103.83,
        110,
        116.54,
        123.47,
        130.81,
        138.59,
        146.83,
        155.56,
        164.81,
        174.61,
        185,
        196,
        207.65,
        220,
        233.08,
        246.94,
        261.63,
        277.18,
        293.66,
        311.13,
        329.63,
        349.23,
        369.99,
        392,
        415.3,
        440,
        466.16,
        493.88,
        523.25,
        554.37,
        587.33,
        622.25,
        659.25,
        698.46,
        739.99,
        783.99,
        830.61,
        880,
        932.33,
        987.77,
        1046.5,
        1108.73,
        1174.66,
        1244.51,
        1318.51,
        1396.91,
        1479.98,
        1567.98,
        1661.22,
        1760,
        1864.66,
        1975.53,
        2093,
        2217.46,
        2349.32,
        2489.02,
        2637.02,
        2793.83,
        2959.96,
        3135.96,
        3322.44,
        3520,
        3729.31,
        3951.07,
        4186.01,
        4434.92,
        4698.63,
        4978.03,
        5274.04,
        5587.65,
        5919.91,
        6271.93,
        6644.88,
        7040,
        7458.62,
        7902.13,
        };
        public static void init()
        {
            song = new Song();
            for (int i = 0; i < 256; i++)
            {
                waveformTables[0, i] = Math.Sin((i / 128.0) * Math.PI) / 8.0;
            }
            for (int i = 0; i < 256; i++)
            {
                waveformTables[1, i] = (i / 256.0) * 0.125;
            }
            for (int i = 0; i < 256; i++)
            {
                waveformTables[2, i] = i < 128 ? 0.125 : 0;
            }
            Random r = new Random();
            for (int i = 0; i < 256; i++)
            {
                waveformTables[3, i] = r.NextDouble() / 8.0;
            }
        }
        public static void test()
        {
            Console.WriteLine("test");
        }

        public static void ChangeNoteAt(int pattern, int channel, int row, byte note)
        {
            if (note < pitches.Length)
            {
                song.patterns[pattern - 1].channels[channel].notes[row].internal_note = note;
                //AudioSubsystem.SetPitch(channel, pitches[note]);

                //AudioSubsystem.Start(channel);
            }
        }
        public static void ClearNoteAt(int pattern, int channel, int row)
        {
            song.patterns[pattern - 1].channels[channel].notes[row].internal_note = (byte)Note.N.EMPTY;
        }
        public static void EndNoteAt(int pattern, int channel, int row)
        {
            song.patterns[pattern - 1].channels[channel].notes[row].internal_note = (byte)Note.N.END;
            //AudioSubsystem.Stop(channel);
        }
        public static void ChangeOctaveAt(int pattern, int channel, int row, int octave)
        {
            Note note = song.patterns[pattern - 1].channels[channel].notes[row];
            if ((int)note.note < 250)
            {
                note.internal_note = (byte)((int)note.note + (12 * octave));
            }
        }
        public static void ChangeVolumeAt(int pattern, int channel, int row, bool upper, byte value)
        {
            if (upper)
            {
                song.patterns[pattern - 1].channels[channel].notes[row].volume.value &= 0x0F;
                song.patterns[pattern - 1].channels[channel].notes[row].volume.value |= (byte)(value << 4);
            }
            else
            {
                song.patterns[pattern - 1].channels[channel].notes[row].volume.value &= 0xF0;
                song.patterns[pattern - 1].channels[channel].notes[row].volume.value |= value;
            }
        }
        public static void ClearVolumeAt(int pattern, int channel, int row)
        {
            song.patterns[pattern - 1].channels[channel].notes[row].volume.type = volumeParameter.Type.N;
        }
        public static void SetVolumeAt(int pattern, int channel, int row)
        {
            song.patterns[pattern - 1].channels[channel].notes[row].volume.type = volumeParameter.Type.V;
        }
        public static void ChangeEffectParamAt(int pattern, int channel, int row, bool upper, byte value)
        {
            if (upper)
            {
                song.patterns[pattern - 1].channels[channel].notes[row].effect.value &= 0x0F;
                song.patterns[pattern - 1].channels[channel].notes[row].effect.value |= (byte)(value << 4);
            }
            else
            {
                song.patterns[pattern - 1].channels[channel].notes[row].effect.value &= 0xF0;
                song.patterns[pattern - 1].channels[channel].notes[row].effect.value |= value;
            }
        }
        public static void ChangeInstrumentAt(int pattern, int channel, int row, bool upper, byte value)
        {
            if (upper)
            {
                song.patterns[pattern - 1].channels[channel].notes[row].instrument &= 0x0F;
                song.patterns[pattern - 1].channels[channel].notes[row].instrument |= (byte)(value << 4);
            }
            else
            {
                //AudioSubsystem.SetInstrument(channel, value);
                song.patterns[pattern - 1].channels[channel].notes[row].instrument &= (byte)((value < 16) ? 0xF0 : 0x00);
                song.patterns[pattern - 1].channels[channel].notes[row].instrument |= value;
            }
        }
        public static void ChangeEffectTypeAt(int pattern, int channel, int row, char effect)
        {
            song.patterns[pattern - 1].channels[channel].notes[row].effect.type = (effectParameter.Type)(effect - 'A' + 1);
        }
        public static void ClearEffectAt(int pattern, int channel, int row)
        {
            song.patterns[pattern - 1].channels[channel].notes[row].effect.type = effectParameter.Type.NONE;
        }
        public static void NewPattern()
        {
            song.patterns.Add(new Pattern());
        }
        public static int GetPatternCount()
        {
            return song.patterns.Count();
        }

        public static string Songname
        {
            set { song.songname = value; }
            get { return song.songname; }
        }
        public static string Author
        {
            set { song.authorname = value; }
            get { return song.authorname; }
        }
        public static byte Speed
        {
            set { song.speed = value; }
            get { return song.speed; }
        }
        public static byte Tempo
        {
            set { song.tempo = value; }
            get { return song.tempo; }
        }
        public static List<ushort> orders
        {
            set { song.orders = value; }
            get { return song.orders; }
        }


        public static string[] GetPatternAsString(int pattern)
        {
            Pattern pat = song.patterns[pattern - 1];
            string[] output = new string[32];
            for (int i = 0; i < 32; i++)
            {
                string channelTemp = "";
                for (int x = 0; x < 8; x++)
                {
                    Channel chan = pat.channels[x];
                    Note note = chan.notes[i];
                    string noteTemp = "|";
                    if (note.note == Note.N.END)
                    {
                        noteTemp = noteTemp.Insert(1, "=== --");
                    }
                    else if (note.note == Note.N.EMPTY)
                    {
                        noteTemp += "-_- --";
                    }
                    else
                    {
                        noteTemp += note.note.ToString() + note.octave // encode the note
                            + " " + note.instrument.ToString("X2"); // encode the instrument
                    }
                    if (note.volume.type != volumeParameter.Type.N)
                    {
                        noteTemp += " " + note.volume.type.ToString() + note.volume.value.ToString("X2"); //encode the volume
                    }
                    else
                    {
                        noteTemp += " ---";
                    }
                    if (note.effect.type != effectParameter.Type.NONE)
                    {
                        noteTemp += " " + note.effect.type.ToString() + note.effect.value.ToString("X2"); // encode the effect
                    }
                    else
                    {
                        noteTemp += " ---";
                    }
                    channelTemp += noteTemp;
                }
                output[i] = channelTemp + "|";
            }
            return output;
        }

        internal static void TogglePlayback()
        {
            if (PlayRows > 0 | PlayRows == -1)
            {
                PlayRows = 0;
                for (int i = 0; i < 8; i++)
                {
                    AudioSubsystem.Stop(i);
                }
            }
            else
            {
                PlayRows = -1;
                resetVariables();
                Consolex.patternTo(orders[order]);
            }
        }

        private static void resetVariables()
        {
            Array.Fill(chanPreGain, 255);
            Array.Fill(chanPostGain, 1);
            Array.Fill(chanPitch, 440);
            Array.Fill(chanPan, 0x7f);
            masterGain = 255;
            Array.Fill(surround, false);

            Array.Fill(previousEffects, (byte)0);
            Array.Fill(previousNote, 440);
            Array.Fill(previousVolume, 1);
            playbackStarted = false;

            Array.Fill(waveformTablePointer, 0);
            Array.Fill(vibratoWaveform, 0);
            Array.Fill(tremoloWaveform, 0);
            Array.Fill(panbrelloWaveform, 0);
            nonResetTickCounter = 0;
        }

        internal static void RunOneLine(int v, ushort pattern)
        {
            row = v;
            tickCounter = 0;
            PlayRows = 1;
            resetVariables();
            if (orders[order] != pattern)
            {
                if (orders.Contains(pattern))
                {
                    order = orders.IndexOf(pattern);
                }
                else
                {
                    Consolex.SetMessage("Pattern " + pattern + " is not in the ordering, switching to pattern " + orders[order]);
                    Consolex.patternTo(orders[order]);
                }
            }
        }
        internal static void ExecuteRow()
        {
            if (PlayRows > 0 | PlayRows == -1)
            {
                playbackStarted = true;
                bool jumped = false;
                bool looped = false;
                int extension = 0;
                for (int channelIndex = 0; channelIndex < 8; channelIndex++)
                {
                    Channel channel = song.patterns[orders[order] - 1].channels[channelIndex];
                    Note note = channel.notes[row];

                    if (note.effect.type == effectParameter.Type.S && ((note.effect.value & 0xF0) == 0x40) && (tickCounter == (note.effect.value & 0xF))  //If the effect is S4x and the delay is reached
                        || ((rowLoop == 0) //If the effect is SEx and the delay has just started
                        && (tickCounter == 0))) //Or it's just the start of a row
                    {
                        if (note.note != Note.N.EMPTY)
                        {
                            if (note.note == Note.N.END)
                            {
                                AudioSubsystem.Stop(channelIndex);
                            }
                            else
                            {

                                if (note.effect.type != effectParameter.Type.G)
                                {
                                    chanPitch[channelIndex] = pitches[note.internal_note];
                                }
                                AudioSubsystem.SetInstrument(channelIndex, note.instrument);
                                AudioSubsystem.Start(channelIndex);
                            }
                        }
                        if (note.volume.type != volumeParameter.Type.N)
                        {
                            if (note.volume.type == volumeParameter.Type.V)
                            {
                                chanPreGain[channelIndex] = note.volume.value;
                            } // else, whelp
                        }
                    }
                    //handle effects
                    handleEffects(ref jumped, ref looped, ref extension, channelIndex, channel, note, note.effect);
                    AudioSubsystem.SetPitch(channelIndex, chanPitch[channelIndex]);
                    AudioSubsystem.SetPan(channelIndex, (float)chanPan[channelIndex] % 255.0f);
                    AudioSubsystem.SetPreGain(channelIndex, Math.Max(Math.Min(chanPreGain[channelIndex] / 255.0, 1.0), 0.0));
                    AudioSubsystem.SetPostGain(channelIndex, chanPostGain[channelIndex], chanPostGain[channelIndex] * (surround[channelIndex] ? -1 : 1));
                }
                if (masterGain > 255)
                {
                    masterGain = 255;
                }
                else if (masterGain < 0) masterGain = 0;
                AudioSubsystem.SetMasterGain(masterGain / 255.0);
                if (tickCounter == Speed+extension)
                {
                    tickCounter = 0;
                }
                else
                if (++tickCounter == Speed+extension)
                {
                    if (PlayRows > 0)
                    {
                        PlayRows--;
                    }
                    if (PlayRows != 0 && rowLoop == 0)
                    {
                        row++;
                        Consolex.rowTo(row);
                    }
                }
                if (row == 32)
                {
                    row = 0;
                    order++;
                    if (order >= orders.Count())
                    {
                        order = 0;
                    }
                    Consolex.rowTo(0);
                    Consolex.patternTo(orders[order]);
                }
            }
            else
            {
                playbackStarted = false;
            }
        }

        private static void handleEffects(ref bool jumped, ref bool looped, ref int extension, int channelIndex, Channel channel, Note note, effectParameter effect)
        {
            int value;
            switch (effect.type)
            {
                case effectParameter.Type.A:
                    Speed = effect.value;
                    break;
                case effectParameter.Type.B:
                    if (!jumped)
                    {
                        row = 0;
                        Consolex.rowTo(0);
                        jumped = true;
                    }
                    value = effect.value;
                    if (row != 31)
                    {
                        if (channel.notes[row + 1].effect.type == effectParameter.Type.O)
                        {
                            value &= channel.notes[row + 1].effect.value << 8;
                        }
                    }
                    if (value < orders.Count())
                    {
                        order = value;
                        Consolex.patternTo(orders[order]);
                    }
                    else
                    {
                        Consolex.SetMessage(effectBad(row, channelIndex) + "Order does not exist");
                    }
                    break;
                case effectParameter.Type.C:
                    if (!jumped)
                    {
                        order++;
                        if (order >= orders.Count())
                        {
                            order = 0;
                        }
                        Consolex.patternTo(orders[order]);
                        jumped = true;
                    }
                    if (effect.value < 32)
                    {
                        row = effect.value;
                        Consolex.rowTo(row);
                    }
                    else
                    {
                        Consolex.SetMessage(effectBad(row, channelIndex) + " Cxx Row is too high, must be less than 32");
                    }
                    break;
                case effectParameter.Type.D:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    affectVolume(ref chanPreGain[channelIndex], value, channelIndex);
                    break;
                case effectParameter.Type.E:
                case effectParameter.Type.F:
                case effectParameter.Type.G:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    if ((note.note != Note.N.EMPTY) && (note.note != Note.N.END))
                    {
                        previousNote[channelIndex] = pitches[note.internal_note];
                    }
                    double difference;
                    bool tick;
                    if ((value & 0xF0) == 0xF0)
                    {
                        difference = ((value & 0xF) / 50.0);
                        tick = false;
                    }
                    else
                    if ((value & 0xF0) == 0xE0)
                    {
                        difference = ((value & 0xF) / 400.0);
                        tick = false;
                    }
                    else
                    {
                        difference = (value / 600.0);
                        tick = true;
                    }
                    if (tick ? tickCounter != 0 : tickCounter == 0)
                    {
                        switch (effect.type)
                        {
                            case effectParameter.Type.E:
                                chanPitch[channelIndex] *= 1 - difference;
                                break;
                            case effectParameter.Type.F:
                                chanPitch[channelIndex] *= 1 + difference;
                                break;
                            case effectParameter.Type.G:
                                if (chanPitch[channelIndex] > previousNote[channelIndex])
                                {
                                    //Console.Error.Write((chanPitch[channelIndex] *= 1 - difference));
                                    if ((chanPitch[channelIndex] *= 1 - difference) < previousNote[channelIndex])
                                        chanPitch[channelIndex] = previousNote[channelIndex];
                                }
                                else
                                {
                                    //Console.Error.Write((chanPitch[channelIndex] *= 1 + difference));
                                    if ((chanPitch[channelIndex] *= 1 + difference) > previousNote[channelIndex])
                                        chanPitch[channelIndex] = previousNote[channelIndex];
                                }
                                break;
                        }
                    }
                    break;
                case effectParameter.Type.H:
                case effectParameter.Type.U:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    waveformTablePointer[channelIndex] += (value & 0xf0) >> 2;
                    waveformTablePointer[channelIndex] %= 256;
                    chanPitch[channelIndex] *= 1 + (waveformTables[vibratoWaveform[channelIndex], waveformTablePointer[channelIndex]] * (value & 0xF) / 
                        (effect.type == effectParameter.Type.H ? 8.0 : 32.0));
                    break;
                case effectParameter.Type.R:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    waveformTablePointer[channelIndex] += (value & 0xf0) >> 2;
                    waveformTablePointer[channelIndex] %= 256;
                    chanPreGain[channelIndex] += (waveformTables[tremoloWaveform[channelIndex], waveformTablePointer[channelIndex]] * (value & 0xF) * 16.0);
                    break;
                case effectParameter.Type.Y:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    waveformTablePointer[channelIndex] += (value & 0xf0) >> 2;
                    waveformTablePointer[channelIndex] %= 256;
                    chanPan[channelIndex] += (waveformTables[panbrelloWaveform[channelIndex], waveformTablePointer[channelIndex]] * (value & 0xF) * 8.0);
                    break;
                case effectParameter.Type.I:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    if (tickCounter == 0 && note.volume.type == volumeParameter.Type.V)
                    {
                        previousVolume[channelIndex] = chanPreGain[channelIndex];
                    }
                    int upper = (value & 0xf0) >> 4;
                    chanPreGain[channelIndex] = nonResetTickCounter++ % (upper + (value & 0xf)) < upper - 1 ? previousVolume[channelIndex] : 0; //!< Out of all of the cycle of the tremor we could be currently on, if it's in the first half have it on otherwise kill the volume.
                    break;
                case effectParameter.Type.J:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    if ((note.note != Note.N.EMPTY) && (note.note != Note.N.END))
                    {
                        previousNote[channelIndex] = note.internal_note;
                    }
                    else if ((int)previousNote[channelIndex] >= pitches.Length)
                    {
                        previousNote[channelIndex] = pitches.Length / 2;
                    }
                    switch (tickCounter % 3)
                    {
                        case 0:
                            chanPitch[channelIndex] = pitches[(int)previousNote[channelIndex]];
                            break;
                        case 1:
                            chanPitch[channelIndex] = pitches[(int)previousNote[channelIndex] + ((value & 0xf0) >> 4)];
                            break;
                        case 2:
                            chanPitch[channelIndex] = pitches[(int)previousNote[channelIndex] + (value & 0xf) + ((value & 0xf0) >> 4)];
                            break;
                    }
                    break;
                case effectParameter.Type.K:
                    handleEffects(ref jumped, ref looped, ref extension, channelIndex, channel, note, new effectParameter
                    {
                        type = effectParameter.Type.D,
                        value = effect.value
                    });
                    handleEffects(ref jumped, ref looped, ref extension, channelIndex, channel, note, new effectParameter
                    {
                        type = effectParameter.Type.H,
                        value = 0
                    });
                    break;
                case effectParameter.Type.L:
                    handleEffects(ref jumped, ref looped, ref extension, channelIndex, channel, note, new effectParameter
                    {
                        type = effectParameter.Type.D,
                        value = effect.value
                    });
                    handleEffects(ref jumped, ref looped, ref extension, channelIndex, channel, note, new effectParameter
                    {
                        type = effectParameter.Type.G,
                        value = 0
                    });
                    break;
                case effectParameter.Type.M:
                    chanPostGain[channelIndex] = effect.value;
                    break;
                case effectParameter.Type.N:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    affectVolume(ref chanPostGain[channelIndex], value, channelIndex);
                    break;
                case effectParameter.Type.P:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    affectVolume(ref chanPan[channelIndex], value, channelIndex);
                    break;
                case effectParameter.Type.Q:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    if (tickCounter % (value & 0xf) == 0)
                    {
                        AudioSubsystem.Start(channelIndex);
                        switch ((value & 0xf0) >> 4)
                        {
                            case 1:  chanPreGain[channelIndex] -= 1; break;
                            case 2:  chanPreGain[channelIndex] -= 2; break;
                            case 3:  chanPreGain[channelIndex] -= 4; break;
                            case 4:  chanPreGain[channelIndex] -= 8; break;
                            case 5:  chanPreGain[channelIndex] -= 16; break;
                            case 6:  chanPreGain[channelIndex] /= 1.5; break;
                            case 7:  chanPreGain[channelIndex] /= 2; break;
                            case 9:  chanPreGain[channelIndex] += 1; break;
                            case 10: chanPreGain[channelIndex] += 2; break;
                            case 11: chanPreGain[channelIndex] += 4; break;
                            case 12: chanPreGain[channelIndex] += 8; break;
                            case 13: chanPreGain[channelIndex] += 16; break;
                            case 14: chanPreGain[channelIndex] *= 1.5; break;
                            case 15: chanPreGain[channelIndex] *= 2; break;
                        }
                    }
                    break;
                case effectParameter.Type.T:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    if (value < 0x10)
                    {
                        Tempo -= (byte)(value & 0xf);
                    }
                    else if (value < 0x20)
                    {
                        Tempo -= (byte)(value & 0xf);
                    } else
                    {
                        Tempo = (byte)value;
                    }
                    break;
                case effectParameter.Type.V:
                    masterGain = effect.value;
                    break;
                case effectParameter.Type.W:
                    value = findPrevious(effect, channel, channelIndex, row);
                    if (value == -1) break;
                    affectVolume(ref masterGain, value, channelIndex);
                    break;
                case effectParameter.Type.X:
                    chanPan[channelIndex] = effect.value;
                    break;
                default:
                    Consolex.SetMessage(effectBad(row, channelIndex) + "Unknown effect");
                    break;
                case effectParameter.Type.S:
                    switch ((effect.value & 0xf0) >> 4)
                    {
                        case 0:
                            Consolex.SetMessage(effectBad(row, channelIndex) + "S0x does nothing");
                            break;
                        case 1:
                            Consolex.SetMessage(effectBad(row, channelIndex) + "S1x is not supported");
                            break;
                        case 2:
                            Consolex.SetMessage(effectBad(row, channelIndex) + "This is not an Amiga");
                            break;
                        case 3:
                            vibratoWaveform[channelIndex] = (effect.value & 0xf);
                            break;
                        case 4:
                            tremoloWaveform[channelIndex] = (effect.value & 0xf);
                            break;
                        case 5:
                            panbrelloWaveform[channelIndex] = (effect.value & 0xf);
                            break;
                        case 6:
                            extension += (effect.value & 0xf);
                            break;
                        case 7:
                            Consolex.SetMessage(effectBad(row, channelIndex) + "New Note Actions are not supported.");
                            break;
                        case 8:
                            chanPan[channelIndex] = (effect.value & 0xf) << 4;
                            break;
                        case 9:
                            switch (effect.value & 0xf)
                            {
                                case 0:
                                    surround[channelIndex] = false;
                                    break;
                                case 1:
                                    surround[channelIndex] = true;
                                    break;
                                case 2:
                                    AudioSubsystem.SetFM(channelIndex, false);
                                    break;
                                case 3:
                                    AudioSubsystem.SetFM(channelIndex, true);
                                    break;
                                default:
                                    Consolex.SetMessage(effectBad(row, channelIndex) + "Sound control does not exist.");
                                    break;
                            }
                            break;
                        case 10:
                            Consolex.SetMessage(effectBad(row, channelIndex) + "This is not a sampler");
                            break;
                        case 11:
                            if ((effect.value & 0xf) == 0)
                            {
                                loopPoint = row;
                            }
                            else
                            {
                                if (patternLoop == (effect.value & 0xf))
                                {
                                    patternLoop = 0;
                                }
                                else
                                {
                                    patternLoop++;
                                    row = loopPoint;
                                    Consolex.rowTo(row);
                                }
                            }
                            break;
                        case 12:
                            if (tickCounter == (effect.value & 0xf))
                                AudioSubsystem.Stop(channelIndex);
                            break;
                        case 14:
                            if (looped) break;
                            if (tickCounter > 0) break;
                            if (rowLoop == 0)
                            {
                                rowLoop = effect.value & 0xf;
                            }
                            else
                            {
                                rowLoop -= 1;
                            }
                            break;
                    }
                    break;
            }
        }

        private static void affectVolume(ref double variable, int value, int channelIndex)
        {
            if (value == 0xff)
            {
                Consolex.SetMessage(effectBad(row, channelIndex) + "Parameter cannot be FF");
            }
            else
            if ((value & 0xF0) == 0)
            {
                if (tickCounter != 0)
                {
                    variable -= (byte)(value & 0xF);
                }
            }
            else
            if ((value & 0x0F) == 0)
            {
                if (tickCounter != 0)
                {
                    variable += (byte)((value & 0xF0) >> 4);
                }
            }
            else
            if ((value & 0xF0) == 0xF0)
            {
                if (tickCounter == 0)
                {
                    variable -= (byte)(value & 0xF);
                }
            }
            else
            if ((value & 0x0F) == 0xF)
            {
                if (tickCounter == 0)
                {
                    variable += (byte)((value & 0xF0) >> 4);
                }
            }
        }

        private static int findPrevious(effectParameter effect, Channel channel, int channelIndex, int row)
        {
            int value;
            if (effect.value != 0)
            {
                previousEffects[(int)effect.type] = effect.value;
            }
            else if (previousEffects[(int)effect.type] == 0)
            {
                int i;
                for (i = row; i >= 0; i--)
                {
                    if (channel.notes[i].effect.type == effect.type &&
                    channel.notes[i].effect.value != 0) break;
                }
                if (!(i < 0))
                {
                    previousEffects[(int)effect.type] = channel.notes[i].effect.value;
                }
                else
                {
                    Consolex.SetMessage(effectBad(row, channelIndex) + "No previous value to recall. Use something other than 00");
                    return -1;
                }
            }

            value = previousEffects[(int)effect.type];
            return value;
        }

        private static string effectBad(int row, int channelIndex)
        {
            return String.Format("Effect at row {0} channel {1} has invalid parameter: ", row, channelIndex);
        }

        internal static void RemovePattern(int v)
        {
            song.patterns.RemoveAt(v - 1);
            orders = orders.Select(x => (x > v) ? (ushort)(x - 1) : x).ToList();
        }

        internal static void LoadFile(Stream stream)
        {
            try
            {
                song.loadfromfile(new BinaryReader(stream));
                row = 0;
                order = 0;

            }
            catch (IOException)
            {
                Consolex.SetMessage("IOException: File is not valid");
            }
        }
        public static void SaveFile(Stream stream)
        {
            try
            {
                song.saveToFile(new BinaryWriter(stream));
            }
            catch (IOException)
            {
                Consolex.SetMessage("IOException: File is not valid");
            }
        }

        //! Data structure for all things related to the song itself
        /*! Handles things like loading and saving to disk, storage
         * of data in memory, and playback
         */
        private class Song
        {
            public List<Pattern> patterns; //!< The unordered list of patterns that are used in the project.
            public string songname; //!< The 30 character name of the song.
            public string authorname; //!< The 30 character name of the musician.
            public List<ushort> orders; //!< The ordered list of which pattern happens when. Patterns may appear more than once, allowing repetition.
            public Version createdVersion; //!< The version of Basic Tracker that the loaded song was created in.
            public Version compatibleVersion; //!< The lowest version of Basic Tracker this song will run in. Usually the same as the created version.
            public byte globalVol; //!< The global volume, from 00 to 7f.
            public byte speed; //!< The speed of the song. It's how many ticks are run for each row of the song. Larger values increase accuracy in a way but slow the song down.
            public byte tempo; //!< The tempo of the song in BPM. The row speed is therefore tempo*4

            //! Empty contructor
            /*! Creates a new song that is empty. This is always called at program boot.
             */
            public Song()
            {
                patterns = new List<Pattern> { new Pattern() };
                songname = "";
                authorname = "";
                orders = new List<ushort> { 1 };
                createdVersion = typeof(Program).Assembly.GetName().Version;
                compatibleVersion = createdVersion;
                globalVol = 0x7F;
                speed = 0x08;
                tempo = 0x40;
            }

            //! Loads a song from a BSCM file
            /*! Takes a reader and sets itself to the state of the song stored in the file.
             * The file does not contain anything which is not settable by the user, like the global
             * volume. This can be set using a volume command in the pattern.
             * 
             * @param[in] br A binary stream which is the file to read from.
             */
            public void loadfromfile(BinaryReader br)
            {
                if (!br.ReadChars(4).SequenceEqual(G.signature))
                {
                    throw new IOException();
                }
                songname = new string(br.ReadChars(30));
                authorname = new string(br.ReadChars(30));
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
                br.ReadChars(6); //reserved bytes
                globalVol = 0;
                speed = br.ReadByte();
                tempo = br.ReadByte();
                for (int i = 0; i < orders.Capacity; i++)
                {
                    orders.Add(br.ReadUInt16());
                }
                for (int i = 0; i < patternPtr.Length; i++)
                {
                    patternPtr[i] = br.ReadUInt32();
                }
                foreach (uint i in patternPtr)
                {
                    br.BaseStream.Seek(i, SeekOrigin.Begin);
                    ushort patlen = br.ReadUInt16();
                    ushort rowlen = br.ReadUInt16();
                    patterns.Add(decodePattern(patlen, rowlen, br.ReadBytes(patlen)));
                }
            }

            internal void saveToFile(BinaryWriter bw)
            {
                bw.Write(G.signature);
                bw.Write(songname.PadRight(30, '\0').ToCharArray());
                bw.Write(authorname.PadRight(30, '\0').ToCharArray());
                bw.Write((ushort)orders.Count());
                bw.Write((ushort)patterns.Count());
                uint[] patternPtr = new uint[patterns.Count()];
                bw.Write((ushort)(((createdVersion.Major & 0xF) << 12) | (createdVersion.Minor & 0xFF)));
                bw.Write((ushort)(((compatibleVersion.Major & 0xF) << 12) | (compatibleVersion.Minor & 0xFF)));
                bw.Write(new char[] { '\0', '\0', '\0', '\0', '\0', '\0', });
                bw.Write(Speed);
                bw.Write(Tempo);
                foreach (ushort order in orders)
                {
                    bw.Write(order);
                }
                int startOfThePointers = (int)bw.BaseStream.Position;
                foreach (uint i in patternPtr)
                {
                    bw.Write((uint)0);
                }
                for (int i = 0; i < song.patterns.Count(); i++)
                {
                    patternPtr[i] = (uint)bw.BaseStream.Position;
                    byte[] data = encodePattern(song.patterns[i]);
                    bw.Write((ushort)data.Length); //patlen
                    bw.Write((ushort)32); // rowlen
                    bw.Write(data);
                }
                bw.Flush();
                bw.Seek(startOfThePointers, SeekOrigin.Begin);
                foreach (uint i in patternPtr)
                {
                    bw.Write(i);
                }
            }

            //! Loads a pattern
            /*! Loads a pattern from the file slice.
             * 
             * @param patlen The length of the pattern in bytes
             * @param rowlen The number of rows
             * 
             * @return The decoded pattern
             */
            private Pattern decodePattern(ushort patlen, ushort rowlen, byte[] data)
            {
                Pattern pattern = new Pattern();
                int[] prevMaskVars = new int[8];
                byte[] prevNote = new byte[8] { 255, 255, 255, 255, 255, 255, 255, 255 };
                byte[] prevInst = new byte[8];
                volumeParameter[] prevVolume = new volumeParameter[8];
                Array.Fill(prevVolume, new volumeParameter() { type = volumeParameter.Type.N, value = 255 });
                effectParameter[] prevEffect = new effectParameter[8];
                Array.Fill(prevEffect, new effectParameter() { type = effectParameter.Type.NONE, value = 0 });
                using (Stream st = new MemoryStream(data))
                {
                    for (int row = 0; row < rowlen; row++)
                    {
                        while (true)
                        {
                            Note note = new Note();
                            int channelVar = st.ReadByte();
                            if (channelVar == 0) break;
                            int channel = (channelVar - 1) & 7;
                            int maskVar;
                            if ((channelVar & 128) == 0)
                            {
                                maskVar = st.ReadByte();
                                prevMaskVars[channel] = maskVar;
                            }
                            else
                            {
                                maskVar = prevMaskVars[channel];
                            }
                            if ((maskVar & 16) != 0)
                            {
                                note.internal_note = prevNote[channel];
                            }
                            else
                            if ((maskVar & 1) != 0)
                            {
                                byte tempnote = (byte)st.ReadByte();
                                note.internal_note = tempnote;
                                if (tempnote == (byte)Note.N.END) note.note = Note.N.END;
                                if (tempnote == (byte)Note.N.EMPTY) note.note = Note.N.EMPTY;
                                prevNote[channel] = tempnote;
                            }
                            if ((maskVar & 32) != 0)
                            {
                                note.instrument = prevInst[channel];
                            }
                            else
                            if ((maskVar & 2) != 0)
                            {
                                note.instrument = (byte)st.ReadByte();
                                prevInst[channel] = note.instrument;
                            }
                            if ((maskVar & 64) != 0)
                            {
                                note.volume = prevVolume[channel];
                            }
                            else
                            if ((maskVar & 4) != 0)
                            {
                                note.volume = note.decodeVolume((byte)st.ReadByte());
                                prevVolume[channel] = note.volume;
                            }
                            if ((maskVar & 128) != 0)
                            {
                                note.effect = prevEffect[channel];
                            }
                            else
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

                            pattern.channels[channel].notes[row] = note;
                        }
                    }
                }
                return pattern;
            }
            private byte[] encodePattern(Pattern pattern)
            {
                //throw new NotImplementedException();
                int[] prevMaskVars = new int[8];
                byte[] prevNote = new byte[8] { 255, 255, 255, 255, 255, 255, 255, 255 };
                byte[] prevInst = new byte[8];
                volumeParameter[] prevVolume = new volumeParameter[8];
                Array.Fill(prevVolume, new volumeParameter() { type = volumeParameter.Type.N, value = 255 });
                effectParameter[] prevEffect = new effectParameter[8];
                Array.Fill(prevEffect, new effectParameter() { type = effectParameter.Type.NONE, value = 0 });
                byte[] data = new byte[65535];
                int finalPosition;
                using (Stream st = new MemoryStream(data))
                {
                    for (int row = 0; row < 32; row++)
                    {
                        for (int channel = 0; channel < pattern.channels.Length; channel++)
                        {
                            Note note = pattern.channels[channel].notes[row];
                            if (note.note == Note.N.EMPTY && note.volume.type == volumeParameter.Type.N && note.effect.type == effectParameter.Type.NONE) continue;

                            int maskVar = 0;
                            if (note.internal_note == prevNote[channel])
                            {
                                maskVar |= 16;
                            }
                            else
                            {
                                prevNote[channel] = note.internal_note;
                                maskVar |= 1;
                            }
                            if (note.instrument == prevInst[channel])
                            {
                                maskVar |= 32;
                            }
                            else
                            {
                                prevInst[channel] = note.instrument;
                                maskVar |= 2;
                            }
                            if (note.volume.Equals(prevVolume[channel]))
                            {
                                maskVar |= 64;
                            }
                            else
                            {
                                prevVolume[channel] = note.volume;
                                maskVar |= 4;
                            }
                            if (note.effect.Equals(prevEffect[channel]))
                            {
                                maskVar |= 128;
                            }
                            else
                            {
                                prevEffect[channel] = note.effect;
                                maskVar |= 8;
                            }
                            /*if ((maskVar & 1) != 0)
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
                            }*/
                            int channelVar = channel + 1;
                            if (maskVar == prevMaskVars[channel])
                            {
                                channelVar |= 128;
                                st.WriteByte((byte)channelVar);
                            }
                            else
                            {
                                st.WriteByte((byte)channelVar);
                                st.WriteByte((byte)maskVar);
                            }
                            if ((maskVar & 1) != 0)
                            {
                                st.WriteByte(note.internal_note);
                            }
                            if ((maskVar & 2) != 0)
                            {
                                st.WriteByte(note.instrument);
                            }
                            if ((maskVar & 4) != 0)
                            {
                                st.WriteByte(note.volume.value);
                            }
                            if ((maskVar & 8) != 0)
                            {
                                st.WriteByte((byte)note.effect.type);
                                st.WriteByte(note.effect.value);
                            }
                        }
                        st.WriteByte(0); // End of row
                    }
                    finalPosition = (int)st.Position;
                }
                return data.Take(finalPosition).ToArray();
            }
        }
        //! Performs a deep copy on a pattern, and then places the copy at the end of the pattern list.
        /* We perform a deep copy using Newtonsoft's Json library - this way the entire thing is turned into a string and back.
         * It's the quickest way to copy something, I kid you not. https://stackoverflow.com/questions/78536/deep-cloning-objects
         * This copies the properties in the order in the actual source file. Due to this, the internal_note is defined before the
         * note and octave, so that the writing of internal note doesn't incorrectly write to note and octave.
         * IMPORTANT NOTE for when this inevitably comes up later, this only copies public things.
         * It can't even see the private properties, so of course it can't copy them. */
        internal static int CopyPattern(int currentPattern)
        {
            song.patterns.Add(
                JsonConvert.DeserializeObject<Pattern>(
                JsonConvert.SerializeObject(
                    song.patterns[currentPattern - 1])));
            return song.patterns.Count();
        }

        public static void CopyChannel(int currentPattern, int channel)
        {
            clipboardChannel = song.patterns[currentPattern - 1].channels[channel];
        }
        internal static void PasteChannel(int currentPattern, int channel)
        {
            song.patterns[currentPattern - 1].channels[channel] = (
                JsonConvert.DeserializeObject<Channel>(
                JsonConvert.SerializeObject(
                    clipboardChannel)));
        }
        public static void RotateChannel(int currentPattern, int channel)
        {
            LinkedList<Note> q = new LinkedList<Note>(song.patterns[currentPattern - 1].channels[channel].notes);
            q.AddFirst(q.Last());
            q.RemoveLast();
            song.patterns[currentPattern - 1].channels[channel].notes = q.ToArray();
        }

        internal static void AskForRow(int currentPattern)
        {
            if (currentPattern == orders[order])
            {
                Consolex.rowTo(row);
            }
            else
            {
                Consolex.rowTo(-1);
            }
        }

        //! A single pattern
        /*! Stores only channel data really
         */
        class Pattern
        {
            public Channel[] channels = new Channel[G.channels];
            //! inits channels
            public Pattern()
            {
                for (int i = 0; i < G.channels; i++)
                {
                    channels[i] = new Channel();
                }
            }
        }
        //! Contains notes
        class Channel
        {
            public Note[] notes = new Note[G.depth];
            //! inits notes
            public Channel()
            {
                for (int i = 0; i < G.depth; i++)
                {
                    notes[i] = new Note();
                }
            }
        }
        //! A single note.
        /*! The grid is made of these. Stores the value,
         * instrument, volume, effect and effect parameter.
         */
        class Note
        {
            //! The note value
            public enum N
            {
                C_, //!< C
                Db, //!< D flat
                D_, //!< D
                Eb, //!< E flat
                E_, //!< E
                F_, //!< F
                Gb, //!< G flat
                G_, //!< G
                Ab, //!< A flat
                A_, //!< A
                Bb, //!< B flat
                B_, //!< B
                END = 254,  //!< end note, stop channel (===)
                EMPTY = 255 //!< undefined, yet to be input (-_-)
            }
            //! Set the internal note to the note and octave for later retrieval.
            public byte internal_note
            {
                set
                {
                    if (value == (byte)N.END)
                    {
                        note = N.END;
                    }
                    else if (value == (byte)N.EMPTY)
                    {
                        note = N.EMPTY;
                    }
                    else
                    {
                        note = (N)(value % 12);
                        octave = value / 12;
                    }
                    _internal_note = value;
                }
                get { return _internal_note; }
            }
            private byte _internal_note;
            public N note { get; set; }
            public int octave { get; set; }
            public byte instrument; //!< The instrument
            public volumeParameter volume;
            public effectParameter effect;
            public Note()
            {
                octave = 4;
                note = N.EMPTY;
                instrument = 0;
                volume = new volumeParameter() { type = volumeParameter.Type.N, value = 255 };
                effect = new effectParameter() { type = effectParameter.Type.NONE, value = 0 };
            }
            //! part of the file loading. Decodes what effect is in the volume column because of course.
            /*! Removed most of this code to simplify the tracker. It's a shame but I simply
             * can't implement all of this in the time allotted.
             */
            public volumeParameter decodeVolume(byte vol)
            {
                volumeParameter param = new volumeParameter();
                /*if (vol <= 64)
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
                }*/
                param.value = vol;
                param.type = volumeParameter.Type.V;
                return param;
            }
        }


        //! a data type to hold the Volume parameter
        /* holds the value and type - but type is always "V" so that's nice
         */
        struct volumeParameter
        {
            public enum Type
            {
                N, //!< No effect
                A, //!< Fine Volume Slide Up
                B, //!< Fine Volume Slide Down
                C, //!< Volume Slide Up 
                D, //!< Volume Slide Down
                E, //!< Portamento Down
                F, //!< Portamento Up
                G, //!< Tone Portamento
                H, //!< Vibrato Depth
                P, //!< Set Panning
                V, //!< Set Volume
            }
            public Type type;
            public byte value;
        }
        //! a data type to hold the Effect parameter
        /* holds the value and type.
         */
        struct effectParameter
        {
            public enum Type
            {
                NONE = 0, //!< No effect, ---
                A, //!< Set Speed 
                B, //!< Position Jump
                C, //!< Pattern Break
                D, //!< Pre Volume Slide
                E, //!< Portamento Down
                F, //!< Portamento Up
                G, //!< Tone Portamento
                H, //!< Vibrato
                I, //!< Tremor
                J, //!< Arpeggio
                K, //!< Pre Volume Slide + Vibrato 
                L, //!< Pre Volume Slide + Tone Portamento
                M, //!< Set Post Volume 
                N, //!< Post Volume Slide
                O, //!< Parameter Extension
                P, //!< Panning Slide
                Q, //!< Retrigger
                R, //!< Tremolo 
                S, //!< Special
                T, //!< Set Tempo 
                U, //!< Fine Vibrato 
                V, //!< Set Global Volume 
                W, //!< Global Volume Slide 
                X, //!< Set Panning 
                Y, //!< Panbrello
                Z, //!< Filter coefficients
            }
            public Type type;
            public byte value;
        }
    }
}
