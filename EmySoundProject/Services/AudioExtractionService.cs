using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmySoundProject.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SoundFingerprinting.Query;
using Xabe.FFmpeg;

namespace EmySoundProject.Services;

public class AudioExtractionService
{
    private readonly string _outPath;

    private readonly ILogger<AudioExtractionService> _logger;

    private List<ConvertModel> _intervalsToCut;

    public List<WaveformRegionModel> WaveformRegionModels;

    public AudioExtractionService(IConfiguration configuration, ILogger<AudioExtractionService> logger)
    {
        _outPath = configuration["OutPath"];
        _logger = logger;
    }

    public async Task AssignRegionsToCut(List<ResultEntry> matchedTracks)
    {
        if (!matchedTracks.Any())
        {
            throw new ArgumentException("List of matched tracks is empty.");
        }

        // Sort matches by the order in which they occur.
        matchedTracks = matchedTracks.OrderBy(x => x.QueryMatchStartsAt).ToList();

        const double timeOffset = 0;

        WaveformRegionModels = new List<WaveformRegionModel>();
        _intervalsToCut = new List<ConvertModel>();
        foreach (var resultEntry in matchedTracks)
        {
            _intervalsToCut.Add(new ConvertModel
            {
                Start = _intervalsToCut.Any() ? _intervalsToCut.Last().CommercialEnd : 0,
                End = resultEntry.QueryMatchStartsAt - timeOffset,
                CommercialEnd = resultEntry.QueryMatchStartsAt + resultEntry.DiscreteTrackCoverageLength + timeOffset
            });
            WaveformRegionModels.Add(new WaveformRegionModel(
                resultEntry.Coverage.QueryMatchStartsAt,
                resultEntry.Coverage.QueryMatchEndsAt,
                resultEntry.Track.Title
            ));
        }

        _intervalsToCut.Add(new ConvertModel
        {
            Start = _intervalsToCut.Last().CommercialEnd,
            End = matchedTracks.First().QueryLength // End of the main audio file.
        });
    }

    public async Task AssignRegionsToCut(List<WaveformRegionModel> waveformRegionList)
    {
        waveformRegionList.Sort((reg1, reg2) => reg1.Start.CompareTo(reg2.Start));
        var endOfFile = _intervalsToCut.Last().End;
        _intervalsToCut = new List<ConvertModel>();
        foreach (var region in waveformRegionList)
        {
            if (_intervalsToCut.Any() && region.Start <= _intervalsToCut.Last().CommercialEnd)
            {
                _intervalsToCut.Last().CommercialEnd = region.End;
                continue;
            }
            _intervalsToCut.Add(new ConvertModel
            {
                Start = _intervalsToCut.Any() ? _intervalsToCut.Last().CommercialEnd : 0,
                End = region.Start,
                CommercialEnd = region.End
            });
        }
        _intervalsToCut.Add(new ConvertModel
        {
            Start = _intervalsToCut.Last().CommercialEnd,
            End = endOfFile
        });
    }

    public async Task FileGenerate(string fileToCutPath)
    {
        if (!_intervalsToCut.Any())
        {
            throw new ArgumentException("List of intervals to cut is empty.");
        }

        var directoryPath = _outPath + "\\" + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
        Directory.CreateDirectory(directoryPath);

        var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(fileToCutPath);
        var i = 0;
        foreach (var item in _intervalsToCut)
        {
            await CutAudioFiles(item, StringType.FromInteger(i), directoryPath, mediaInfo);
            i++;
        }

        _logger.LogInformation("Creating the resulting file of the operation.");
        var finalAudioParts = new List<AudioFileReader>();
        foreach (var item in _intervalsToCut)
        {
            finalAudioParts.Add(new AudioFileReader(directoryPath + "\\" + _intervalsToCut.IndexOf(item) + ".wav"));
        }

        var playlist = new ConcatenatingSampleProvider(finalAudioParts);
        WaveFileWriter.CreateWaveFile16(directoryPath + "\\result.wav", playlist);

        CloseAudioFiles(finalAudioParts);

        _logger.LogInformation("The result has been generated and saved to file result.wav.");
    }

    private async Task CutAudioFiles(ConvertModel item, string outputFileName, string outputDirPath, IMediaInfo mediaInfo)
    {
        var startTime = TimeSpan.FromSeconds(item.Start);
        var endTime = TimeSpan.FromSeconds(item.End);

        _logger.LogInformation("Exporting audio from {Start} to {End} to file {FileName}.",
            startTime.ToString("hh\\:mm\\:ss\\.ffff"),
            endTime.ToString("hh\\:mm\\:ss\\.ffff"),
            outputFileName + ".wav");

        await Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream(mediaInfo.Streams)
            .AddParameter(
                $"-ss {startTime} -t {endTime.Subtract(startTime)}")
            .SetOutput(outputDirPath + "\\" + outputFileName + ".wav")
            .Start();
    }

    private static void CloseAudioFiles(List<AudioFileReader> finalAudioParts)
    {
        foreach (var file in finalAudioParts)
        {
            file.Close();
        }
    }
}