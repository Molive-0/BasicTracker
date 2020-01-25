using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using System;
using System.Windows.Forms;

namespace BasicTracker
{
    static public class AudioSubsystem
    {
        static AsioOut asio;
        static WasapiOut wasapi;
        static TrackerGenerator t;

        static public void init()
        {
            t = new TrackerGenerator();
            string[] drivers = AsioOut.GetDriverNames();

            bool worked = true;
            int i;
            for (i = 0; i < drivers.Length; i++)
            {
                Console.WriteLine(i.ToString() + ": " + drivers[i]);
            }
            Console.WriteLine(i.ToString() + ": WasApi");
            bool inputValid = false;
            int input;
            do
            {
                Console.Write("Pick an audio backend: ");
                string inputString = Console.ReadLine();
                inputValid = Int32.TryParse(inputString, out input);
                inputValid &= (input >= 0 && input <= i);
            } while (!inputValid);
            if (input != i)
            {
                try
                {
                    worked = false;
                    if (!AsioDriver.GetAsioDriverByName(drivers[input]).CanSampleRate(32000))
                        t = new TrackerGenerator(48000, 2);
                    asio = new AsioOut(input);
                    asio.Init(t);
                    asio.Play();
                    worked = true;
                }
                catch (Exception) { }
            }
            if (!worked)
            {
                MessageBox.Show("We couldn't load an ASIO driver for this system, so we're falling back on a wasapi implementation. " +
                    "If you want an ASIO driver, ASIO4ALL is free and tends to work great.", "ASIO error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                input = i;
            }
            if (input == i)
            {
                wasapi = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 5);
                t = new TrackerGenerator();
                wasapi.Init(t);
                wasapi.Play();
            }
        }
        static public void Shutdown()
        {
            if (asio != null)
            {
                asio.Stop();
                asio.Dispose();
            }
            if (wasapi != null)
            {
                wasapi.Stop();
                wasapi.Dispose();
            }
        }

        public static void SetPostGain(int channel, double left, double right) { t.SetPostGain(channel, left, right); }
        public static void SetPreGain(int channel, double value) { t.SetPreGain(channel, value); }
        public static void SetFM(int channel, bool value) { t.SetFM(channel, value); }
        public static void SetInstrument(int channel, int value) { t.SetInstrument(channel, value); }
        public static void SetPitch(int channel, double value) { t.SetPitch(channel, value); }
        public static void SetPan(int channel, double value) { t.SetPan(channel, value); }
        public static void SetMasterGain(double value) { t.SetMasterGain(value); }
        public static void Start(int channel) { t.Start(channel); }
        public static void Stop(int channel) { t.Stop(channel); }

        private class TrackerGenerator : ISampleProvider
        {
            // Wave format
            private readonly WaveFormat waveFormat;

            // Generator variable
            private long nSample;

            private ChannelGenerator[] channels;
            private bool[] echoEnable;
            private bool[] surround;
            private double[] FIR;
            private double echoVol;
            private double echoFeedback;
            private int echoLength; //!< 256 + echolength*64 bytes of buffer

            private float[] echo;
            private MixingSampleProvider masterMix;
            private VolumeSampleProvider masterGain;
            private MixingSampleProvider echoMix;
            private BufferedSampleProvider[] channelBuffers;
            private PanningSampleProvider[] panners;
            private SurroundSampleProvider[] gainers;

            public void SetPostGain(int channel, double valueLeft, double valueRight) { gainers[channel].VolumeLeft = (float)valueLeft; gainers[channel].VolumeRight = (float)valueRight; }
            public void SetPreGain(int channel, double value) => channels[channel].SetGain(value);
            public void SetFM(int channel, bool value) => channels[channel].SetModulation(value);
            public void SetInstrument(int channel, int value) => channels[channel].SetInstrument(value);
            public void SetPitch(int channel, double value) => channels[channel].SetPitch(value);
            public void SetPan(int channel, double value) => panners[channel].Pan = (float)((value/128.0)-1.0);
            public void SetMasterGain(double value) => masterGain.Volume = (float)(value / 8.0);
            public void Start(int channel) => channels[channel].Start();
            public void Stop(int channel) => channels[channel].Stop();

            /// <summary>
            /// Initializes a new instance for the Generator (Default :: 32Khz, 2 channels)
            /// </summary>
            public TrackerGenerator()
                : this(32000, 2)
            {

            }
            public TrackerGenerator(WaveFormat f)
                : this(f.SampleRate, f.Channels)
            {

            }

            /// <summary>
            /// Initializes a new instance for the Generator (UserDef SampleRate &amp; Channels)
            /// </summary>
            /// <param name="sampleRate">Desired sample rate</param>
            /// <param name="channel">Number of channels</param>
            public TrackerGenerator(int sampleRate, int channel)
            {
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
                channels = new ChannelGenerator[8];
                for (int i = 0; i < 8; i++)
                {
                    channels[i] = new ChannelGenerator(sampleRate, 1);
                }
                channelBuffers = new BufferedSampleProvider[8];
                for (int i = 0; i < 8; i++)
                {
                    channelBuffers[i] = new BufferedSampleProvider(new WaveFormat(sampleRate, 32, 1));
                }
                panners = new PanningSampleProvider[8];
                for (int i = 0; i < 8; i++)
                {
                    panners[i] = new PanningSampleProvider(channelBuffers[i]);
                    panners[i].Pan = 0.0f;
                }
                gainers = new SurroundSampleProvider[8];
                for (int i = 0; i < 8; i++)
                {
                    gainers[i] = new SurroundSampleProvider(panners[i]);
                    gainers[i].VolumeLeft = 1.0f;
                    gainers[i].VolumeRight = 1.0f;
                }
                masterMix = new MixingSampleProvider(gainers);
                masterGain = new VolumeSampleProvider(masterMix);
                masterGain.Volume = 0.125f;
                //echoMix = new MixingSampleProvider(gainers);
                nSample = 0;
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

                //Console.WriteLine(count);

                //foreach (var c in channelBuffers)
                //{
                //   c.ClearBuffer();
                //}

                // Through a great mess of technicality we have to run the channels themselves in step with
                // each other, which is complicated and time consuming. We quickly get the data into buffers.
                float[][] longerbuffer = new float[8][];
                for (int i = 0; i < 8; i++)
                {
                    longerbuffer[i] = new float[count / 2];
                }
                float[] buf = new float[1];
                float prev;
                for (int sampleCount = 0; sampleCount < count / 2; sampleCount++)
                {
                    prev = 1.0f;
                    for (int i = 0; i < 8; i++)
                    {
                        channels[i].Read(buf, 0, 1, prev);
                        prev = buf[0] + 1.0f;
                        longerbuffer[i][sampleCount] = buf[0];
                    }
                }
                // Here's something interesting, in order to move the data about here wew perform a block copy.
                // However, I'm pretty sure AddSamples also performs a block copy. So that's two block copies per channel :)
                for (int i = 0; i < 8; i++)
                {
                    channelBuffers[i].AddSamples(longerbuffer[i], 0, count / 2);
                }
                //return masterMix.Read(buffer, offset, count);
                return masterGain.Read(buffer, offset, count);
            }
        }

        /// <summary>
        /// Signal Generator type
        /// </summary>
        public enum SignalGeneratorType
        {
            /// <summary>
            /// No sound.
            /// </summary>
            None = 0,
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
            /// White noise
            /// </summary>
            White,
        }

        private class ChannelGenerator : ISampleProvider
        {
            // Wave format
            private readonly WaveFormat waveFormat;

            // Random Number for the White Noise
            private readonly Random random = new Random();
            private int prevNSample;
            private double prevSampleValue;

            // Const Math
            private const double TwoPi = 2 * Math.PI;

            // Generator variable
            private double nSample;

            // State machine code
            private SignalGeneratorType Type;
            private double Pitch;
            private double Gain;
            private double attackRamp;
            private bool modulation;
            private double attack;
            private bool LFO;


            /// <summary>
            /// Initializes a new instance for the Generator (Default :: 32Khz, mono)
            /// </summary>
            public ChannelGenerator()
                : this(32000, 1)
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
                Type = SignalGeneratorType.None;
                LFO = false;
                Pitch = 1000.0;
                Gain = 1.0;
                attackRamp = 0;
                attack = 0;
                modulation = false;
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
            }

            /// <summary>
            /// The waveformat of this WaveProvider (same as the source)
            /// </summary>
            public WaveFormat WaveFormat => waveFormat;

            public void Stop()
            {
                attackRamp = -0.01;
            }
            public void Start()
            {
                nSample = 0;
                attackRamp = 0.01;
                attack = 0;
                prevNSample = 0;
            }
            public void SetInstrument(int inst)
            {
                if (inst == 0) Type = SignalGeneratorType.None;
                Type = (SignalGeneratorType)((inst - 1) % 5 + 1);
                LFO = inst > 5;
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
                double Frequency = this.modulation ? Pitch * modulation : Pitch;
                if (LFO) Frequency /= 100;

                // Complete Buffer
                for (int sampleCount = 0; sampleCount < count / waveFormat.Channels; sampleCount++)
                {
                    if (attack > 0)
                    {
                        switch (Type)
                        {
                            case SignalGeneratorType.Sin:

                                // Sinus Generator

                                multiple = TwoPi * Frequency / waveFormat.SampleRate;
                                sampleValue = Gain * Math.Sin(nSample);

                                nSample += multiple;

                                break;


                            case SignalGeneratorType.Square:

                                // Square Generator

                                multiple = 2 * Frequency / waveFormat.SampleRate;
                                sampleSaw = ((nSample) % 2) - 1;
                                sampleValue = sampleSaw > 0 ? Gain : -Gain;

                                nSample += multiple;
                                break;

                            case SignalGeneratorType.Triangle:

                                // Triangle Generator

                                sampleSaw = ((nSample) % 2);
                                sampleValue = 2 * sampleSaw;
                                if (sampleValue > 1)
                                    sampleValue = 2 - sampleValue;
                                if (sampleValue < -1)
                                    sampleValue = -2 - sampleValue;

                                sampleValue *= Gain;

                                nSample += 2 * Frequency / waveFormat.SampleRate;
                                break;

                            case SignalGeneratorType.SawTooth:

                                // SawTooth Generator

                                multiple = 2 * Frequency / waveFormat.SampleRate;
                                sampleSaw = ((nSample) % 2) - 1;
                                sampleValue = Gain * sampleSaw;

                                nSample += multiple;
                                break;

                            case SignalGeneratorType.White:

                                // White Noise Generator
                                if (prevNSample < (int)nSample)
                                {
                                    sampleValue = (Gain * NextRandomTwo());
                                    prevSampleValue = sampleValue;
                                    prevNSample = (int)nSample;
                                }
                                else
                                {
                                    sampleValue = prevSampleValue;
                                }
                                nSample += 2 * Frequency / waveFormat.SampleRate;
                                break;

                            default:
                                sampleValue = 0.0;
                                break;
                        }

                        for (int i = 0; i < waveFormat.Channels; i++)
                        {
                            buffer[outIndex++] = (float)(sampleValue * attack);
                        }


                    }
                    else
                    {
                        for (int i = 0; i < waveFormat.Channels; i++)
                        {
                            buffer[outIndex++] = 0.0f;
                        }
                    }
                    attack += attackRamp;
                    if (attack >= 1 || attack <= 0)
                    {
                        attack = (int)attack;
                        attackRamp = 0;
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

    /// <summary>
    /// Provides a buffered store of samples
    /// Read method will return queued samples or fill buffer with zeroes
    /// Now backed by a circular buffer
    /// </summary>
    public class BufferedSampleProvider : ISampleProvider
    {
        private float[] buffer;
        private readonly WaveFormat waveFormat;

        /// <summary>
        /// Creates a new buffered WaveProvider
        /// </summary>
        /// <param name="waveFormat">WaveFormat</param>
        public BufferedSampleProvider(WaveFormat waveFormat)
        {
            this.waveFormat = waveFormat;
            BufferLength = waveFormat.AverageBytesPerSecond * 5;
        }

        /// <summary>
        /// Buffer length in bytes
        /// </summary>
        public int BufferLength { get; set; }

        /// <summary>
        /// Gets the WaveFormat
        /// </summary>
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        /// <summary>
        /// Adds samples. Takes a copy of buffer, so that buffer can be reused if necessary
        /// </summary>
        public void AddSamples(float[] buf, int offset, int count)
        {
            // create buffer here to allow user to customise buffer length
            if (buffer == null)
            {
                buffer = new float[BufferLength];
            }

            if (count > BufferLength)
            {
                throw new InvalidOperationException("Buffer full");
            }

            Buffer.BlockCopy(buf, offset, buffer, 0, count * 4);
        }

        /// <summary>
        /// Reads from this WaveProvider
        /// Will always return count bytes, since we will zero-fill the buffer if not enough available
        /// </summary>
        public int Read(float[] buf, int offset, int count)
        {
            if (buffer != null) // not yet created
            {
                Buffer.BlockCopy(buffer, offset, buf, 0, count * 4);
            }
            return count;
        }

        /// <summary>
        /// Discards all audio from the buffer
        /// </summary>
        public void ClearBuffer()
        {
            buffer = new float[BufferLength];
        }
    }
    public class SurroundSampleProvider : ISampleProvider //Based on code for the VolumeSampleProvider
    {
        private readonly ISampleProvider source;

        /// <summary>
        /// Initializes a new instance of VolumeSampleProvider
        /// </summary>
        /// <param name="source">Source Sample Provider</param>
        public SurroundSampleProvider(ISampleProvider source)
        {
            this.source = source;
            VolumeLeft = 1.0f;
            VolumeRight = 1.0f;
        }

        /// <summary>
        /// WaveFormat
        /// </summary>
        public WaveFormat WaveFormat => source.WaveFormat;

        /// <summary>
        /// Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="sampleCount">Number of samples desired</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int sampleCount)
        {
            int samplesRead = source.Read(buffer, offset, sampleCount);
            if ((VolumeLeft != 1f) && (VolumeRight != 1f))
            {
                for (int n = 0; n < sampleCount; n++)
                {
                    buffer[offset + n] *= (n % 2 == 0) ? VolumeLeft : VolumeRight;
                }
            }
            return samplesRead;
        }

        /// <summary>
        /// Allows adjusting the volume, 1.0f = full volume
        /// </summary>
        public float VolumeLeft { get; set; }
        public float VolumeRight { get; set; }
    }
}
