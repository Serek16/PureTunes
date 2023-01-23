using EmyProject.CustomService.Model;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

    public async Task FileGenerate(List<ResultEntry> result, string file)
    {
        var directory = _outPath + "\\" + DateTime.Now.ToString("MMdd.hhmmss");
        Directory.CreateDirectory(directory);

        List<ConvertModel> list = new List<ConvertModel>();

        const double timeOffset = 0;

        foreach (var resultEntry in result)
        {
            list.Add(new ConvertModel
            {
                Start = list.Any() ? list.Last().CommercialEnd : 0,
                End = resultEntry.QueryMatchStartsAt - timeOffset,
                CommercialEnd = resultEntry.QueryMatchStartsAt + resultEntry.DiscreteTrackCoverageLength + timeOffset
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

        _logger.LogInformation("The result has been generated and saved to file result.wav.");
    }
}