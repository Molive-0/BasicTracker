using System;
using System.IO;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BasicTracker
{
    static public class AudioSubsystem
    {
        static WaveOut waveout;
        static TrackerGenerator t;
        static MixingSampleProvider mixer;
        static public void init()
        {
            waveout = new WaveOut();
            t = new TrackerGenerator();
            mixer = new MixingSampleProvider(t.WaveFormat);
            mixer.AddMixerInput(t);
            waveout.Init(mixer);
            waveout.Play();
        }

        public class TrackerGenerator : ISampleProvider
        {
            // Wave format
            private readonly WaveFormat waveFormat;

            // Generator variable
            private long nSample;

            private ChannelGenerator[] channels;
            private double[] panning;
            private bool[] echoEnable;
            private double[] FIR;
            private double masterVol;
            private double echoVol;
            private double echoFeedback;
            private int echoLength; //!< 256 + echolenght*64 bytes of buffer

            private float[] echo;
            private MixingSampleProvider masterMix;
            private MixingSampleProvider echoMix;
            private BufferedWaveProvider[] channelBuffers;
            private PanningSampleProvider[] panners;

            /// <summary>
            /// Initializes a new instance for the Generator (Default :: 32Khz, 2 channels)
            /// </summary>
            public TrackerGenerator()
                : this(32000, 2)
            {

            }

            /// <summary>
            /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
            /// </summary>
            /// <param name="sampleRate">Desired sample rate</param>
            /// <param name="channel">Number of channels</param>
            public TrackerGenerator(int sampleRate, int channel)
            {
                channels = new ChannelGenerator[8];
                channelBuffers = new BufferedWaveProvider[] 
                {
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                    new BufferedWaveProvider(waveFormat),
                };
                panners = new PanningSampleProvider[8];
                for (int i = 0; i < 8; i++)
                {
                    panners[i] = new PanningSampleProvider(channelBuffers[i].ToSampleProvider());
                }
                masterMix = new MixingSampleProvider(panners);
                echoMix = new MixingSampleProvider(panners);
                nSample = 0;
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
            }

            /// <summary>
            /// The waveformat of this WaveProvider (same as the source)
            /// </summary>
            public WaveFormat WaveFormat => waveFormat;

            /// <summary>
            /// Reads from this provider.
            /// </summary>
            public int Read(float[] buffer, int offset, int count)
            {
                int outIndex = offset;

                foreach (var c in channelBuffers)
                {
                    c.ClearBuffer();
                }

                // Through a great mess of technicality we have to run the channels themselves in step with
                // each other, which is complicated and time consuming. We quickly get the data into buffers.
                float[][] longerbuffer = new float[8][];
                for (int i = 0; i < 8; i++)
                {
                    longerbuffer[i] = new float[count];
                }
                for (int sampleCount = 0; sampleCount < count / waveFormat.Channels; sampleCount++)
                {
                    float prev = 1.0f;
                    for (int i = 0; i < 8; i++)
                    {
                        float[] buf = new float[waveFormat.Channels];
                        channels[i].Read(buf, 0, 1, prev);
                        prev = buf[0] + 1.0f;
                        longerbuffer[i][sampleCount * 2] = buf[0];
                        longerbuffer[i][sampleCount * 2 + 1] = buf[1];
                    }
                }
                // Here's something interesting, in order to move the data about here wew perform a block copy.
                // However, I'm pretty sure AddSamples also performs a block copy. So that's two block copies per channel :)
                for (int i = 0; i < 8; i++)
                {
                    byte[] bytesbuffer = new byte[count*4];
                    Buffer.BlockCopy(longerbuffer[i], 0, bytesbuffer, 0, bytesbuffer.Length);
                    channelBuffers[i].AddSamples(bytesbuffer, 0, count);
                }
                for (int i = 0; i < 8; i++)
                {
                    panners[i].Pan = (float)panning[i];
                }
                return masterMix.Read(buffer, offset, count);
            }
        }

        /// <summary>
        /// Signal Generator type
        /// </summary>
        public enum SignalGeneratorType
        {
            /// <summary>
            /// White noise
            /// </summary>
            White,
            /// <summary>
            /// Sine wave
            /// </summary>
            Sin,
            /// <summary>
            /// Square wave
            /// </summary>
            Square,
            /// <summary>
            /// Triangle Wave
            /// </summary>
            Triangle,
            /// <summary>
            /// Sawtooth wave
            /// </summary>
            SawTooth,
            /// <summary>
            /// No sound.
            /// </summary>
            None,
        }

        public class ChannelGenerator : ISampleProvider
        {
            // Wave format
            private readonly WaveFormat waveFormat;

            // Random Number for the White Noise
            private readonly Random random = new Random();

            // Const Math
            private const double TwoPi = 2 * Math.PI;

            // Generator variable
            private long nSample;

            // State machine code
            private SignalGeneratorType Type;
            private double Pitch;
            private double Gain;
            private bool running;
            private bool modulation;

            /// <summary>
            /// Initializes a new instance for the Generator (Default :: 32Khz, 2 channels)
            /// </summary>
            public ChannelGenerator()
                : this(32000, 2)
            {

            }

            /// <summary>
            /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
            /// </summary>
            /// <param name="sampleRate">Desired sample rate</param>
            /// <param name="channel">Number of channels</param>
            public ChannelGenerator(int sampleRate, int channel)
            {
                nSample = 0;
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
            }

            /// <summary>
            /// The waveformat of this WaveProvider (same as the source)
            /// </summary>
            public WaveFormat WaveFormat => waveFormat;

            public void Stop()
            {
                Type = SignalGeneratorType.None;
                running = false;
            }
            public void Start()
            {
                nSample = 0;
                running = true;
            }
            public void SetInstrument(int inst)
            {
                Type = (SignalGeneratorType)inst;
            }
            public void SetGain(double gain) => Gain = gain;
            public void SetPitch(double pitch) => Pitch = pitch;
            public void SetModulation(bool b) => modulation = b;

            /// <summary>
            /// Reads from this provider.
            /// </summary>
            public int Read(float[] buffer, int offset, int count)
            {
                return Read(buffer, offset, count, 1.0);
            }

            /// <summary>
            /// Reads from this provider, with FM stuff.
            /// </summary>
            public int Read(float[] buffer, int offset, int count, double modulation)
            {
                int outIndex = offset;

                // Generator current value
                double multiple;
                double sampleValue;
                double sampleSaw;
                double Frequency = this.modulation?Pitch * modulation:Pitch;

                // Complete Buffer
                for (int sampleCount = 0; sampleCount < count / waveFormat.Channels; sampleCount++)
                {
                    if (running)
                    {
                        switch (Type)
                        {
                            case SignalGeneratorType.Sin:

                                // Sinus Generator

                                multiple = TwoPi * Frequency / waveFormat.SampleRate;
                                sampleValue = Gain * Math.Sin(nSample * multiple);

                                nSample++;

                                break;


                            case SignalGeneratorType.Square:

                                // Square Generator

                                multiple = 2 * Frequency / waveFormat.SampleRate;
                                sampleSaw = ((nSample * multiple) % 2) - 1;
                                sampleValue = sampleSaw > 0 ? Gain : -Gain;

                                nSample++;
                                break;

                            case SignalGeneratorType.Triangle:

                                // Triangle Generator

                                multiple = 2 * Frequency / waveFormat.SampleRate;
                                sampleSaw = ((nSample * multiple) % 2);
                                sampleValue = 2 * sampleSaw;
                                if (sampleValue > 1)
                                    sampleValue = 2 - sampleValue;
                                if (sampleValue < -1)
                                    sampleValue = -2 - sampleValue;

                                sampleValue *= Gain;

                                nSample++;
                                break;

                            case SignalGeneratorType.SawTooth:

                                // SawTooth Generator

                                multiple = 2 * Frequency / waveFormat.SampleRate;
                                sampleSaw = ((nSample * multiple) % 2) - 1;
                                sampleValue = Gain * sampleSaw;

                                nSample++;
                                break;

                            case SignalGeneratorType.White:

                                // White Noise Generator
                                sampleValue = (Gain * NextRandomTwo());
                                break;

                            default:
                                sampleValue = 0.0;
                                break;
                        }

                        for (int i = 0; i < waveFormat.Channels; i++)
                        {
                            buffer[outIndex++] = (float)sampleValue;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < waveFormat.Channels; i++)
                        {
                            buffer[outIndex++] = 0.0f;
                        }
                    }
                }
                return count;
            }

            /// <summary>
            /// Private :: Random for WhiteNoise &amp; Pink Noise (Value form -1 to 1)
            /// </summary>
            /// <returns>Random value from -1 to +1</returns>
            private double NextRandomTwo()
            {
                return 2 * random.NextDouble() - 1;
            }
        }
    }
}
