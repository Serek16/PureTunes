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
    private const double GapBetweenAds = 1;

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
        if (!matchedTracks.Any())
        {
            throw new ArgumentException("List of matched tracks is empty.");
        }

        // Sort matches by the order in which they occur.
        matchedTracks = matchedTracks.OrderBy(x => x.QueryMatchStartsAt).ToList();

        var waveformRegionModels = new List<WaveformRegionModel>();
        foreach (var resultEntry in matchedTracks)
        {
            // Add that yellow filler region if needed, when a gap between ads is small enough.
            if (waveformRegionModels.Any() &&
                resultEntry.QueryMatchStartsAt - waveformRegionModels.Last().End <= GapBetweenAds)
            {
                waveformRegionModels.Add(new WaveformRegionModel(
                    waveformRegionModels.Last().End,
                    resultEntry.Coverage.QueryMatchStartsAt - TimeOffsetFront,
                    "_filler"
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
        foreach (var region in waveformRegionList)
        {
            if (_intervalsToCut.Any()
                && region.Start <= _intervalsToCut.Last().CommercialEnd
                && region.End > _intervalsToCut.Last().CommercialEnd)
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
            End = queryLength
        });

        // In case when the first region starts at 0 or the last one ends at the end of the query, the code simply
        // doesn't work (probably other cases too).
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

    private async Task CutAudioFiles(ConvertModel item, string outputFileName, string outputDirPath,
        IMediaInfo mediaInfo)
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