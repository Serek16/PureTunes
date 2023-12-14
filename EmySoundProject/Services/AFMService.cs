using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2019.Presentation;
using Microsoft.Extensions.Logging;
using NAudio.Wave;
using Radzen;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.Query;

namespace EmySoundProject.Services;

// Audio Fingerprinting and Matching Service
public class AFMService
{
    private readonly IFingerprintStorage _fingerprintStorage;
    private readonly IAudioService _audioService;
    private readonly ILogger<AFMService> _logger;
    private readonly NotificationService _notificationService;

    public AFMService(
        IFingerprintStorage fingerprintStorage,
        IAudioService audioService,
        NotificationService notificationService,
        ILogger<AFMService> logger)
    {
        _fingerprintStorage = fingerprintStorage;
        _audioService = audioService;
        _logger = logger;
        _notificationService = notificationService;
    }

    // Function fingerprints tracks and add them to the Emy database for further examination.
    public async Task<IEnumerable<TrackInfo>> AddDataset(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new ArgumentException($"The directory path is incorrect. Couldn't find {directoryPath}");
        }

        var addedTrackList = new List<TrackInfo>();

        // Iterate through all files.
        foreach (var filePath in Directory.GetFiles(directoryPath))
        {
            var trackInfo = await FingerprintAndAddFile(filePath);
            if (trackInfo != null) {
                addedTrackList.Add(trackInfo);
            }
        }

        return addedTrackList;
    }

    public async Task<TrackInfo> FingerprintAndAddFile(string filePath)
    {
        // Check if the existing file has .wav extension and if it is at least 2 seconds long.
        if (!Path.GetExtension(filePath).Equals(".wav"))
        {
            _logger.LogWarning($"{filePath} will not be included because it isn't a WAV file");
            return null;
        }

        if (GetWavFileDuration(filePath) < 2)
        {
            _logger.LogWarning($"{filePath} will not be included because it is too short (less than 2 seconds)");
            return null;
        }

        try
        {
            var trackInfo = new TrackInfo(filePath, Path.GetFileNameWithoutExtension(filePath), string.Empty);
            var hashes = await FingerprintCommandBuilder
                .Instance
                .BuildFingerprintCommand()
                .From(filePath)
                .UsingServices(_audioService)
                .Hash();

            // Add file to the EmySound database.
            _fingerprintStorage.AddTrack(trackInfo, hashes);
            _logger.LogInformation($"Added \"{trackInfo.Title}\" the EmySound database.");
            _notificationService.Notify(new NotificationMessage
            {
                Duration = 1000, Severity = NotificationSeverity.Success,
                Summary = $"Wstawiono plik {filePath} z {hashes.Count} odciskami."
            });
            return trackInfo;
        }
        catch (Exception e)
        {
            _logger.LogError($"Couldn't add track to EmySound database. Error info:\n{e.Message}");
            _notificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Error, Summary = e.Message });
            return null;
        }
    }

    public async Task<bool> IsFilePathCorrect(string filePath)
    {
        if (File.Exists(filePath))
        {
            if (Path.GetExtension(filePath) == ".wav")
            {
                _logger.LogInformation($"File \"{filePath}\" is correct.");
                _notificationService.Notify(new NotificationMessage
                    { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Plik poprawny!" });

                return true;
            }

            _logger.LogError($"File \"{filePath}\" doesn't have correct format The file needs to be in WAV format.");
            _notificationService.Notify(new NotificationMessage
            {
                Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Niepoprawne rozszerzenie pliku!"
            });
        }
        else
        {
            _logger.LogError($"File \"{filePath}\" doesn't exist");
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
            .UsingServices(_fingerprintStorage.GetModel(), _audioService)
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

        _logger.LogInformation($"Found {matches.Count} matches.");

        return matches;
    }

    public async Task<IEnumerable<TrackInfo>> ReadAllLoadedTracks()
    {
        return _fingerprintStorage.GetAllTracks();
    }

    // Function returns file length.
    private static double GetWavFileDuration(string fileName)
    {
        using var wf = new WaveFileReader(fileName);
        return wf.TotalTime.TotalSeconds;
    }
}