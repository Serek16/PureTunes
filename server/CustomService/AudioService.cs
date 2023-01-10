using EmyProject.CustomService.Model;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xabe.FFmpeg;

namespace EmyProject.CustomService
{
    public class AudioService
    {
        private readonly IConfiguration configuration;
        private string outPath;

        public AudioService(IConfiguration _configuration)
        {
            configuration = _configuration;
            outPath = configuration["outPath"];
        }

        public async void FileGenerate(List<ResultEntry> result, string file)
        {
            var directory = outPath + "\\" + DateTime.Now.ToString("MMdd.hhmmss");
            //Tworzenie katalogu
            Directory.CreateDirectory(directory);

            double Lenght = result.First().QueryLength;

            List<ConvertModel> list = new List<ConvertModel>();

            list.Add(new ConvertModel
            {
                Start = 0,
                End = result[0].QueryMatchStartsAt - 0.15,
                CommercialEnd = result[0].QueryMatchStartsAt + result[0].DiscreteTrackCoverageLength + 0.15
            });

            for (int i = 1; i < result.Count; i++)
            {
                list.Add(new ConvertModel
                {
                    Start = list.Last().CommercialEnd,
                    End = result[i].QueryMatchStartsAt - 0.15,
                    CommercialEnd = result[i].QueryMatchStartsAt + result[i].DiscreteTrackCoverageLength + 0.15
                });
            }

            list.Add(new ConvertModel
            {
                Start = list.Last().CommercialEnd,
                End = Lenght
            });

            var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(file);
            foreach (var item in list)
            {
                // TrimWavFile(file, directory + "\\" + list.IndexOf(item) + ".wav", TimeSpan.FromSeconds(item.Start), TimeSpan.FromSeconds(Lenght - item.End));
                await Xabe.FFmpeg.FFmpeg.Conversions.New()
                    .AddStream(mediaInfo.Streams)
                    .AddParameter(
                        $"-ss {TimeSpan.FromSeconds(item.Start)} -t {TimeSpan.FromSeconds(item.End - item.Start)}")
                    .SetOutput(directory + "\\" + list.IndexOf(item) + ".wav")
                    .Start();
            }

            if (list.Count > 0)
            {
                List<AudioFileReader> audio = new List<AudioFileReader>();
                foreach (var item in list)
                {
                    audio.Add(new AudioFileReader(directory + "\\" + list.IndexOf(item) + ".wav"));
                }

                var playlist = new ConcatenatingSampleProvider(audio);
                WaveFileWriter.CreateWaveFile16(directory + "\\result.wav", playlist);
            }
        }


        //https://markheath.net/post/trimming-wav-file-using-naudio
        private void TrimWavFile(string inPath, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd)
        {
            using (WaveFileReader reader = new WaveFileReader(inPath))
            {
                using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                    int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                    startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

                    int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                    endBytes = endBytes - endBytes % reader.WaveFormat.BlockAlign;
                    int endPos = (int)reader.Length - endBytes;

                    TrimWavFile(reader, writer, startPos, endPos);
                }
            }
        }

        private void TrimWavFile(WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos)
        {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}