﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NAudio.Wave;

namespace KeyAsio.Net.Audio
{
    public class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        public static string CachePath = "_temp.wav";
        public CachedSound(string audioFileName)
        {
            var newFileName = CachePath;

            try
            {
                int outRate = 44100;
                int channels = 2;
                var outFormat = new WaveFormat(outRate, channels);
                using (var audioFileReader = new AudioFileReader(audioFileName))
                using (var resampler = new MediaFoundationResampler(audioFileReader, outFormat))
                using (var stream = new FileStream(newFileName, FileMode.Create))
                {
                    resampler.ResamplerQuality = 60;
                    WaveFileWriter.WriteWavFileToStream(stream, resampler);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(audioFileName);
                throw;
            }


            using (var audioFileReader = new AudioFileReader(newFileName))
            {
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }

                AudioData = wholeFile.ToArray();
            }
        }
    }
}