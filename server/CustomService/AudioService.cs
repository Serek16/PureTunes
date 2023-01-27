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
using Microsoft.VisualBasic.CompilerServices;
using Xabe.FFmpeg;

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

    public async Task FileGenerate(List<ResultEntry> matchedTracks, string file)
    {
        if (!matchedTracks.Any())
        {
            throw new ArgumentException("List with matched tracks is empty.");
        }

        // Sort matches by the order in which they occur.
        matchedTracks = matchedTracks.OrderBy(x => x.QueryMatchStartsAt).ToList();
        
        var directoryPath = _outPath + "\\" + DateTime.Now.ToString("MMdd.hhmmss");
        Directory.CreateDirectory(directoryPath);

        var intervalsToCut = new List<ConvertModel>();

        const double timeOffset = 0;

        foreach (var resultEntry in matchedTracks)
        {
            intervalsToCut.Add(new ConvertModel
            {
                Start = intervalsToCut.Any() ? intervalsToCut.Last().CommercialEnd : 0,
                End = resultEntry.QueryMatchStartsAt - timeOffset,
                CommercialEnd = resultEntry.QueryMatchStartsAt + resultEntry.DiscreteTrackCoverageLength + timeOffset
            });
        }

        intervalsToCut.Add(new ConvertModel
        {
            Start = intervalsToCut.Last().CommercialEnd,
            End = matchedTracks.First().QueryLength // End of the main audio file.
        });

        var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(file);
        var i = 0;
        foreach (var item in intervalsToCut)
        {
            await CutAudioFiles(item, StringType.FromInteger(i), directoryPath, mediaInfo);
            i++;
        }

        _logger.LogInformation("Creating the resulting file of the operation.");
        var finalAudioParts = new List<AudioFileReader>();
        foreach (var item in intervalsToCut)
        {
            finalAudioParts.Add(new AudioFileReader(directoryPath + "\\" + intervalsToCut.IndexOf(item) + ".wav"));
        }

        var playlist = new ConcatenatingSampleProvider(finalAudioParts);
        WaveFileWriter.CreateWaveFile16(directoryPath + "\\result.wav", playlist);

        _logger.LogInformation("The result has been generated and saved to file result.wav.");
    }

    private async Task CutAudioFiles(ConvertModel item, string fileName, string directoryPath, IMediaInfo mediaInfo)
    {
        var startTime = TimeSpan.FromSeconds(item.Start);
        var endTime = TimeSpan.FromSeconds(item.End);

        _logger.LogInformation("Exporting audio from {Start} to {End} to file {FileName}.",
            startTime.ToString("hh\\:mm\\:ss\\.ffff"),
            endTime.ToString("hh\\:mm\\:ss\\.ffff"),
            fileName + ".wav");

        await Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream(mediaInfo.Streams)
            .AddParameter(
                $"-ss {startTime} -t {endTime.Subtract(startTime)}")
            .SetOutput(directoryPath + "\\" + fileName + ".wav")
            .Start();
    }
}