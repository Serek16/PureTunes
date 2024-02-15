using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using PureTunes.Models;
using SoundFingerprinting.Query;
using Xabe.FFmpeg;

namespace PureTunes.Services;

public class AudioExtractionService
{
    private const double GapSize = 1;

    private const double TimeOffsetFront = 0;

    private const double TimeOffsetEnd = 0.4;

    private readonly string _outPath;

    private readonly ILogger<AudioExtractionService> _logger;

    private List<ConvertModel> _intervalsToCut;

    public AudioExtractionService(IConfiguration configuration, ILogger<AudioExtractionService> logger)
    {
        _outPath = configuration["OutPath"];
        _logger = logger;
    }

    public async Task<List<WaveformRegionModel>> AssignRegions(List<ResultEntry> matchedTracks)
    {
        // Sort matches by the order in which they occur.
        matchedTracks = matchedTracks.OrderBy(x => x.QueryMatchStartsAt).ToList();

        var waveformRegionModels = new List<WaveformRegionModel>();
        foreach (var resultEntry in matchedTracks)
        {
            if (waveformRegionModels.Any() &&
                resultEntry.QueryMatchStartsAt - waveformRegionModels.Last().End <= GapSize)
            {
                waveformRegionModels.Add(new WaveformRegionModel(
                    waveformRegionModels.Last().End,
                    resultEntry.Coverage.QueryMatchStartsAt - TimeOffsetFront,
                    "_gap"
                ));
            }

            waveformRegionModels.Add(new WaveformRegionModel(
                resultEntry.Coverage.QueryMatchStartsAt - TimeOffsetFront,
                resultEntry.Coverage.QueryMatchEndsAt + TimeOffsetEnd,
                resultEntry.Track.Title
            ));
        }

        return waveformRegionModels;
    }

    public async Task AssignRegionsToCut(List<WaveformRegionModel> waveformRegionList, double queryLength)
    {
        waveformRegionList = waveformRegionList.OrderBy(r => r.Start).ToList();
        _intervalsToCut = new List<ConvertModel>();

        double lastEnd = 0; // Track the end of the last interval to know where the next one should start

        foreach (var region in waveformRegionList)
        {
            // Check if the current region overlaps with the last interval
            if (_intervalsToCut.Any() && region.Start <= _intervalsToCut.Last().CommercialEnd)
            {
                // Extend the last interval if the current region ends after the last interval
                if (region.End > _intervalsToCut.Last().CommercialEnd)
                {
                    _intervalsToCut.Last().CommercialEnd = region.End;
                }
                // If the current region is completely inside the last interval, no further action is needed
            }
            else
            {
                // Add a new interval that starts from the end of the last interval (or 0 if it's the first one)
                // and ends at the start of the current region. This covers the gap before the current region.
                // Then, use the end of the current region as the "CommercialEnd" for cutting purposes.
                var newInterval = new ConvertModel
                {
                    Start = lastEnd,
                    End = region.Start,
                    CommercialEnd = region.End
                };

                _intervalsToCut.Add(newInterval);
            }

            // Update lastEnd to ensure it's always at the end of the last processed region
            // This helps in creating the next interval correctly without overlaps or gaps
            lastEnd = Math.Max(lastEnd, region.End);
        }

        // Optionally, you might want to add a final interval to cover any remaining content after the last region
        if (lastEnd < queryLength)
        {
            _intervalsToCut.Add(new ConvertModel
            {
                Start = lastEnd,
                End = queryLength,
                CommercialEnd = queryLength // Assuming you want to cut until the very end of the content
            });
        }
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

        foreach (var item in _intervalsToCut)
        {
            File.Delete(directoryPath + "\\" + _intervalsToCut.IndexOf(item) + ".wav");
        }

        _logger.LogInformation("The result has been generated and saved to file result.wav.");
    }

    private async Task CutAudioFiles(ConvertModel item, string outputFileName, string outputDirPath,
        IMediaInfo inputFileMediaInfo)
    {
        var startTime = TimeSpan.FromSeconds(item.Start);
        var endTime = TimeSpan.FromSeconds(item.End);

        _logger.LogInformation("Exporting audio from {Start} to {End} to file {FileName}.",
            startTime.ToString("hh\\:mm\\:ss\\.ffff"),
            endTime.ToString("hh\\:mm\\:ss\\.ffff"),
            outputFileName + ".wav");

        await Xabe.FFmpeg.FFmpeg.Conversions.New()
            .AddStream(inputFileMediaInfo.Streams)
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