using NAudio.Wave;
using NAudio.Wave.Asio;
using NAudio.Wave.SampleProviders;
using System;
using System.Windows.Forms;

namespace BasicTracker
{
    //! Contains a software mockup of the sound hardware of an old computer.
    static public class AudioSubsystem
    {
        static AsioOut asio; //!< The ASIO out device
        static WasapiOut wasapi; //!< The WasAPI out device
        static TrackerGenerator t; //!< The actual synth.

        //! Init the Audio subsystem
        /*! This init handles the very first section of the program, where it asks for a driver to use.
         * It allows you to pick because before I had it choose itself and nothing worked. The first
         * screen doesn't really fit with the rest of the project but oh well.
         */
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
        //! This is the only place in the project with a destructor. It handles closing the audio drivers
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
        //! Alias for the TrackerGenerator
        /*! 
         * @param channel Channel to affect
         * @param left Volume of left channel between 0 and 1
         * @param right Volume of right channel between 0 and 1
         */
        public static void SetPostGain(int channel, double left, double right) { t.SetPostGain(channel, left, right); }
        //! Alias for the TrackerGenerator
        /*!
         * @param channel Channel to affect
         * @param value Volume of the signal between 0 and 1
         */
        public static void SetPreGain(int channel, double value) { t.SetPreGain(channel, value); }
        //! Alias for the TrackerGenerator
        /*!
         * @param channel Channel to affect
         * @param value Bool, true to enable FM
         */
        public static void SetFM(int channel, bool value) { t.SetFM(channel, value); }
        //! Alias for the TrackerGenerator
        /*! 
         * @param channel Channel to affect
         * @param value Instrument to swap to
         */
        public static void SetInstrument(int channel, int value) { t.SetInstrument(channel, value); }
        //! Alias for the TrackerGenerator
        /*! 
         * @param channel Channel to affect
         * @param value Pitch in hertz
         */
        public static void SetPitch(int channel, double value) { t.SetPitch(channel, value); }
        //! Alias for the TrackerGenerator
        /*! 
         * @param channel Channel to affect
         * @param value Panning, between 0 and 255
         */
        public static void SetPan(int channel, double value) { t.SetPan(channel, value); }
        //! Alias for the TrackerGenerator
        /*! 
         * @param value volume of master, between 0 and 1
         */
        public static void SetMasterGain(double value) { t.SetMasterGain(value); }
        //! Alias for the TrackerGenerator
        /*! 
         * @param channel Channel to affect
         */
        public static void Start(int channel) { t.Start(channel); }
        //! Alias for the TrackerGenerator
        /*! 
         * @param channel Channel to affect
         */
        public static void Stop(int channel) { t.Stop(channel); }

        //! An Naudio sample provider that runs the audio backend.
        private class TrackerGenerator : ISampleProvider
        {
            //! Wave format
            private readonly WaveFormat waveFormat;

            private ChannelGenerator[] channels; //!< each of the channel audio

            private MixingSampleProvider masterMix; //!< Mix the channels down
            private VolumeSampleProvider masterGain; //!< Final volume control
            private BufferedSampleProvider[] channelBuffers; //!< Allows the code to put the channels into something more useful
            private PanningSampleProvider[] panners; //!< panning stage
            private SurroundSampleProvider[] gainers; //!< PreGain + surround
            //! Sets the gain past the FM
            /*! 
             * @param channel Channel to affect
             * @param valueLeft Volume of left channel between 0 and 1
             * @param valueRight Volume of right channel between 0 and 1
             */
            public void SetPostGain(int channel, double valueLeft, double valueRight) { gainers[channel].VolumeLeft = (float)valueLeft; gainers[channel].VolumeRight = (float)valueRight; }
            //! Sets the gain before the FM
            /*! 
             * @param channel Channel to affect
             * @param value Volume of the signal between 0 and 1
             */
            public void SetPreGain(int channel, double value) => channels[channel].SetGain(value);
            //! Sets the FM status
            /*! 
             * @param channel Channel to affect
             * @param value Bool, true to enable FM
             */
            public void SetFM(int channel, bool value) => channels[channel].SetModulation(value);
            //! Sets the instrument
            /*! 
             * @param channel Channel to affect
             * @param value Instrument to swap to
             */
            public void SetInstrument(int channel, int value) => channels[channel].SetInstrument(value);
            //! Sets the pitch
            /*! 
             * @param channel Channel to affect
             * @param value Pitch in hertz
             */
            public void SetPitch(int channel, double value) => channels[channel].SetPitch(value);
            //! Sets the pan
            /*! 
             * @param channel Channel to affect
             * @param value Panning, between 0 and 255
             */
            public void SetPan(int channel, double value) => panners[channel].Pan = (float)((value/128.0)-1.0);
            //! Sets the master volume
            /*! 
             * @param value volume of master, between 0 and 1
             */
            public void SetMasterGain(double value) => masterGain.Volume = (float)(value / 8.0);
            //! Starts the channel playing
            /*! 
             * @param channel Channel to affect
             */
            public void Start(int channel) => channels[channel].Start();
            //! Stops the channel playing
            /*! 
             * @param channel Channel to affect
             */
            public void Stop(int channel) => channels[channel].Stop();

            //! Initializes a new instance for the Generator (Default :: 32Khz, 2 channels)
            public TrackerGenerator()
                : this(32000, 2)
            {

            }

            //! Initializes a new instance for the Generator using a WaveFormat
            /*!
             * @param f Waveformat to take info from
             */
            public TrackerGenerator(WaveFormat f)
                : this(f.SampleRate, f.Channels)
            {

            }

            //! Initializes a new instance for the Generator 
            /*!
             @param sampleRate sample rate
             @param channel of channels
             */
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
            }

            //! The waveformat of this WaveProvider (same as the source)
            public WaveFormat WaveFormat => waveFormat;

            //! Reads from this provider.
            /*!
             * @param buffer Sound buffer to output the sound into
             * @param offset Where in the buffer the sound goes
             * @param count How many samples to make * channels
             * 
             * @return Samples read
             */
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
                // Here's something interesting, in order to move the data about here we perform a block copy.
                // However, I'm pretty sure AddSamples also performs a block copy. So that's two block copies per channel :)
                for (int i = 0; i < 8; i++)
                {
                    channelBuffers[i].AddSamples(longerbuffer[i], 0, count / 2);
                }
                //return masterMix.Read(buffer, offset, count);
                return masterGain.Read(buffer, offset, count);
            }
        }

        //! Signal Generator enum for which wave is playing
        public enum SignalGeneratorType
        {

            None = 0,   //!< No sound.
            Sin,        //!< Sine wave
            Square,     //!< Square wave
            Triangle,   //!< Triangle wave
            SawTooth,   //!< Sawtooth wave
            White,      //!< White noise
        }

        //! Audio synth for a single channel
        private class ChannelGenerator : ISampleProvider
        {
            //! Wave format
            private readonly WaveFormat waveFormat;

            //! Random Number for the White Noise
            private readonly Random random = new Random();
            private int prevNSample; //!< How long to stretch the white noise for
            private double prevSampleValue; //!< previous sample for the white noise stretching

            //! Const Math
            private const double TwoPi = 2 * Math.PI;

            //! How far through the sample we are
            private double nSample;


            private SignalGeneratorType Type;   //!< Wave type
            private double Pitch;               //!< Pitch of audio
            private double Gain;                //!< Volume of sound
            private double attackRamp;          //!< Used for making notes come in softer
            private bool modulation;            //!< Is FM on?
            private double attack;              //!< Used for making notes come in softer
            private bool LFO;                   //!< Is this an LFO channel?


            //! Initializes a new instance for the Generator (Default :: 32Khz, mono)
            public ChannelGenerator()
                : this(32000, 1)
            {

            }

            //! Initializes a new instance for the Generator (UserDef SampleRate & Channels)
            /*!
             * @param sampleRate Desired sample rate
             * @param channel Number of channels
             */
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

            //! The waveformat of this WaveProvider (same as the source)
            public WaveFormat WaveFormat => waveFormat;

            //! Lowers the volume of the sample to stop it
            public void Stop()
            {
                attackRamp = -0.01;
            }
            //! Raises the volume over time to start it
            public void Start()
            {
                nSample = 0;
                attackRamp = 0.01;
                attack = 0;
                prevNSample = 0;
            }
            //! Sets the instrument to the parameter
            /*!
             * @param inst The instrument to switch to
             */
            public void SetInstrument(int inst)
            {
                if (inst == 0) Type = SignalGeneratorType.None;
                Type = (SignalGeneratorType)((inst - 1) % 5 + 1);
                LFO = inst > 5;
            }
            //! Sets the gain to the parameter
            /*!
             * @param gain Gain value between 0 and 1
             */
            public void SetGain(double gain) => Gain = gain;
            //! Sets the pitch to the parameter
            /*! 
             * @param pitch The pitch in hertz
             */
            public void SetPitch(double pitch) => Pitch = pitch;
            //! Sets the modulation to the parameter
            /*!
             * @param b If the modulation is enabled or not
             */
            public void SetModulation(bool b) => modulation = b;

            //! Reads from this provider.
            /*!
             * @param buffer Sound buffer to output the sound into
             * @param offset Where in the buffer the sound goes
             * @param count How many samples to make * channels
             * 
             * @return Samples read
             */
            public int Read(float[] buffer, int offset, int count)
            {
                return Read(buffer, offset, count, 1.0);
            }

            //! Reads from this provider, with FM stuff.
            /*!
             * @param buffer Sound buffer to output the sound into
             * @param offset Where in the buffer the sound goes
             * @param count How many samples to make * channels
             * @param modulation The modulation input from the channel to the left
             * 
             * @return Samples read
             */
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

                                // Sine Generator

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

            //! Random for WhiteNoise (Value form -1 to 1)
            /*! 
             * @return Ranndom value from -1 to +1
             */
            private double NextRandomTwo()
            {
                return 2 * random.NextDouble() - 1;
            }
        }
    }

    //! Provides a buffered store of samples
    /*!
     * Used to produce a sample provider version
     * Read method will return queued samples or fill buffer with zeroes
     * Now backed by a circular buffer
     */
    public class BufferedSampleProvider : ISampleProvider
    {
        //! The internal sound buffer
        private float[] buffer;
        private readonly WaveFormat waveFormat; //!< format for the... wave

        //! Creates a new buffered WaveProvider
        /*! 
         * @param waveFormat WaveFormat
         */
        public BufferedSampleProvider(WaveFormat waveFormat)
        {
            this.waveFormat = waveFormat;
            BufferLength = waveFormat.AverageBytesPerSecond * 5;
        }

        //! Buffer length in bytes
        public int BufferLength { get; set; }

        //! Gets the WaveFormat
        public WaveFormat WaveFormat
        {
            get { return waveFormat; }
        }

        //! Adds samples. Takes a copy of buffer, so that buffer can be reused if necessary
        /*!
         * @param buf Sound buffer take the sound from
         * @param offset Where in the buffer the sound goes
         * @param count How many samples to make * channels
         */
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

        //! Reads from this WaveProvider
        /*! Will always return count bytes, since we will zero-fill the buffer if not enough available
         */
        /*!
         * @param buf Sound buffer to output the sound into
         * @param offset Where in the buffer the sound goes
         * @param count How many samples to make * channels
         * 
         * @return Samples read
         */
        public int Read(float[] buf, int offset, int count)
        {
            if (buffer != null) // not yet created
            {
                Buffer.BlockCopy(buffer, offset, buf, 0, count * 4);
            }
            return count;
        }

        //! Discards all audio from the buffer
        public void ClearBuffer()
        {
            if (buffer == null)
            {
                buffer = new float[BufferLength];
            }
            else
            {
                Array.Clear(buffer,0,buffer.Length);
            }
        }
    }
    public class SurroundSampleProvider : ISampleProvider //!< Based on code for the VolumeSampleProvider
    {
        //! Where the sound is coming from
        private readonly ISampleProvider source;

        //! Initializes a new instance of SurroundSampleProvider
        /*!
         * @param source Source Sample Provider
         */
        public SurroundSampleProvider(ISampleProvider source)
        {
            this.source = source;
            VolumeLeft = 1.0f;
            VolumeRight = 1.0f;
        }

        //! WaveFormat
        public WaveFormat WaveFormat => source.WaveFormat;

        //! Reads samples from this sample provider
        /*!
         * @param buffer Sample buffer
         * @param offset Offset into sample buffer
         * @param sampleCount Number of samples desired
         * @returns Number of samples read
         */
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

        //! Allows adjusting the volume, 1.0f = full volume in left channel
        public float VolumeLeft { get; set; }
        //! Allows adjusting the volume, 1.0f = full volume in right channel
        public float VolumeRight { get; set; }
    }
}
