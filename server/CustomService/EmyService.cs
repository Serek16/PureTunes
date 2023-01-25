﻿using NAudio.Wave;
using Radzen;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SoundFingerprinting.DAO.Data;

namespace EmyProject.CustomService;

public class EmyService
{
    private readonly ILogger<EmyService> _logger;
    private readonly NotificationService _notificationService;

    private readonly EmyModelService _modelService;
    private readonly IAudioService _audioService;

    public EmyService(NotificationService notificationService, ILogger<EmyService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;

        _modelService = EmyModelService.NewInstance("localhost", 3399);
        _audioService = new SoundFingerprintingAudioService();
        
        EmptyEmySoundDatabase();
    }

    // Function returns file length.
    private double GetWavFileDuration(string fileName)
    {
        using var wf = new WaveFileReader(fileName);
        return wf.TotalTime.TotalSeconds;
    }

    // Function fingerprints tracks and them to the Emy database for further examination.
    public async Task AddDataset(string path)
    {
        if (!Directory.Exists(path))
        {
            throw new ArgumentException($"The directory path is incorrect. Couldn't find {path}");
        }
        
        EmptyEmySoundDatabase();

        // Iterate through all files.
        foreach (string file in Directory.GetFiles(path))
        {
            // Check if the existing file has .wav extension and if it is at least 2 seconds long.
            if (Path.GetExtension(file) == ".wav")
            {
                if (GetWavFileDuration(file) >= 2)
                {
                    try
                    {
                        var trackInfo = new TrackInfo(file, Path.GetFileNameWithoutExtension(file), string.Empty);
                        var hashes = await FingerprintCommandBuilder
                            .Instance
                            .BuildFingerprintCommand()
                            .From(file)
                            .UsingServices(_audioService)
                            .Hash();

                        // Add file to the EmySound database.
                        _modelService.Insert(trackInfo, hashes);
                        _logger.LogInformation("Added \"{Track}\" the EmySound database.", trackInfo.Title);
                        _notificationService.Notify(new NotificationMessage
                        {
                            Duration = 1000, Severity = NotificationSeverity.Success,
                            Summary = $"Wstawiono plik {file} z {hashes.Count} odciskami."
                        });
                    }
                    catch (Exception e)
                    {
                        _logger.LogError("Couldn't add track to EmySound database. Error info:\n{ErrorMessage}",
                            e.Message);
                        _notificationService.Notify(new NotificationMessage
                            { Duration = 1000, Severity = NotificationSeverity.Error, Summary = e.Message });
                    }
                }
            }
        }
    }

    private void EmptyEmySoundDatabase()
    {
        // Delete all existing datasets from EmySound database.
        foreach (var item in _modelService.ReadAllTracks())
        {
            _modelService.DeleteTrack(item.Id);
        }
    }
    
    public async Task<bool> IsFilePathCorrect(string file)
    {
        if (File.Exists(file))
        {
            if (Path.GetExtension(file) == ".wav")
            {
                _logger.LogInformation("File \"{FileName}\" is correct.", file);
                _notificationService.Notify(new NotificationMessage
                    { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Plik poprawny!" });

                return true;
            }

            _logger.LogError("File \"{FileName}\" doesn't have correct format The file needs to be in WAV format.",
                file);
            _notificationService.Notify(new NotificationMessage
            {
                Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Niepoprawne rozszerzenie pliku!"
            });
        }
        else
        {
            _logger.LogError("File \"{FileName}\" doesn't exist", file);
            _notificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
        }

        return false;
    }

    // function searches for matching patterns in the file examined.
    public async Task<List<ResultEntry>> FindMatches(string file, double confidence)
    {
        _logger.LogInformation("Finding matching fingerprints in the query.");
        var result = await QueryCommandBuilder.Instance
            .BuildQueryCommand()
            .From(file)
            .UsingServices(_modelService, _audioService)
            .Query();

        List<ResultEntry> matches = new List<ResultEntry>();

        if (result.ContainsMatches)
        {
            foreach (var (entry, _) in result.ResultEntries)
            {
                // The condition for checking the minimum % of result.
                if (entry?.Confidence > confidence)
                {
                    matches.Add(entry);
                }
            }
        }

        _logger.LogInformation("Found {MatchesCount} matches.", matches.Count);

        return matches;
    }

    public async Task<IEnumerable<TrackData>> ReadAllLoadedTracks()
    {
        return _modelService.ReadAllTracks();
    }
}