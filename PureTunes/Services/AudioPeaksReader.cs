namespace PureTunes.Services;

using NAudio.Wave;
using System;
using System.Collections.Generic;

public static class AudioPeaksReader
{
    public static double[] GetAudioPeaks(string filePath, double peaksPerSecond)
    {
        using var audioFileReader = new AudioFileReader(filePath);

        var sampleRate = audioFileReader.WaveFormat.SampleRate;
        var channelCount = audioFileReader.WaveFormat.Channels;
        // Calculate the sample window size based on the number of seconds per peak
        var sampleWindow = (int)(sampleRate * 1 / peaksPerSecond);

        var peaks = new List<double>();
        var buffer = new float[sampleRate * channelCount];

        int samplesRead;
        while ((samplesRead = audioFileReader.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < samplesRead; i += sampleWindow * channelCount)
            {
                double maxSample = 0;
                for (int j = i; j < i + sampleWindow * channelCount && j < samplesRead; j += channelCount)
                {
                    double sampleValue = 0;
                    for (int channel = 0; channel < channelCount; channel++)
                    {
                        sampleValue += buffer[j + channel];
                    }
                    sampleValue /= channelCount;
                    maxSample = Math.Max(maxSample, Math.Abs(sampleValue));
                }
                // Add the max sample (peak) for this window to the list
                peaks.Add(maxSample);
            }
        }

        return peaks.ToArray();
    }
}
