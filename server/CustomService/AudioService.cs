using EmyProject.CustomService.Model;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace EmyProject.CustomService;

public class AudioService
{
    private readonly string _outPath;

    private readonly ILogger<AudioService> _logger;

    public AudioService(IConfiguration configuration, ILogger<AudioService> logger)
    {
        _outPath = configuration["outPath"];
        _logger = logger;
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
            var startTime = TimeSpan.FromSeconds(item.Start);
            var endTime = TimeSpan.FromSeconds(item.End);
            
            _logger.LogInformation("Exporting audio from {Start} to {End} to file {FileName}.",
                startTime.ToString("hh\\:mm\\:ss\\.ffff"), 
                endTime.ToString("hh\\:mm\\:ss\\.ffff"),
                list.IndexOf(item) + ".wav");
            
            await Xabe.FFmpeg.FFmpeg.Conversions.New()
                .AddStream(mediaInfo.Streams)
                .AddParameter(
                    $"-ss {startTime} -t {endTime.Subtract(startTime)}")
                .SetOutput(directory + "\\" + list.IndexOf(item) + ".wav")
                .Start();
        }

        if (list.Count > 0)
        {
            _logger.LogInformation("Creating the resulting file of the operation.");
            List<AudioFileReader> audio = new List<AudioFileReader>();
            foreach (var item in list)
            {
                audio.Add(new AudioFileReader(directory + "\\" + list.IndexOf(item) + ".wav"));
            }

            var playlist = new ConcatenatingSampleProvider(audio);
            WaveFileWriter.CreateWaveFile16(directory + "\\result.wav", playlist);
        }
        
        _logger.LogInformation("The result has been generated.");
    }
}