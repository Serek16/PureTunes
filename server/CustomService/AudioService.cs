using EmyProject.CustomService.Model;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EmyProject.CustomService;

public class AudioService
{
    private readonly string _outPath;

    public AudioService(IConfiguration configuration)
    {
        _outPath = configuration["outPath"];
    }

    public async void FileGenerate(List<ResultEntry> result, string file)
    {
        var directory = _outPath + "\\" + DateTime.Now.ToString("MMdd.hhmmss");
        Directory.CreateDirectory(directory);

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
            End = result.First().QueryLength // End of the main audio file.
        });

        var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(file);
        foreach (var item in list)
        {
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
}