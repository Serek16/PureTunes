using EmyProject.CustomService.Model;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Radzen;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Builder;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;
using SoundFingerprinting.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EmyProject.CustomService;

public class EmyService
{
    private readonly string _datasetPath;

    private readonly NotificationService _notificationService;

    private readonly EmyModelService _modelService;
    private readonly IAudioService _audioService;

    public List<PathModel> DatasetDirList { get; } = new List<PathModel>();

    public EmyService(IConfiguration configuration, NotificationService notificationService)
    {
        _datasetPath = configuration["Dataset"];

        _notificationService = notificationService;

        _modelService = EmyModelService.NewInstance("localhost", 3399);
        _audioService = new SoundFingerprintingAudioService();

        CheckDataset();
    }

    private void CheckDataset()
    {
        if (!Directory.Exists(_datasetPath))
        {
            _notificationService.Notify(new NotificationMessage
                { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Katalog nie istnieje!" });
        }
        else
        {
            // Check if the directory contains subdirectories with audio files.
            if (Directory.GetDirectories(_datasetPath).Length == 0)
            {
                _notificationService.Notify(new NotificationMessage
                    { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Brak katalogów dataset!" });
            }
            else
            {
                foreach (var catalog in Directory.GetDirectories(_datasetPath).ToList())
                {
                    foreach (var file in Directory.GetFiles(catalog).ToList())
                    {
                        if (Path.GetExtension(file) == ".wav")
                        {
                            // Add subdirectory to DatasetDirList if a subdirectory contains at least 1 .wav file. 
                            DatasetDirList.Add(new PathModel { Path = catalog });
                            break;
                        }
                    }
                }
            }
        }
    }
    
    // Function returns file length.
    private double GetWavFileDuration(string fileName)
    {
        using var wf = new WaveFileReader(fileName);
        return wf.TotalTime.TotalSeconds;
    }

    public async Task AddDataset(string path)
    {
        // Delete all existing datasets from EmySound database.
        foreach (var item in _modelService.ReadAllTracks())
        {
            _modelService.DeleteTrack(item.Id);
        }
        
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
                        var track = new TrackInfo(file, Path.GetFileNameWithoutExtension(file), string.Empty);
                        var hashes = await FingerprintCommandBuilder
                            .Instance
                            .BuildFingerprintCommand()
                            .From(file)
                            .UsingServices(_audioService)
                            .Hash();
                        
                        // Add file to the EmySound database.
                        _modelService.Insert(track, hashes);
                        _notificationService.Notify(new NotificationMessage
                        {
                            Duration = 1000, Severity = NotificationSeverity.Success,
                            Summary = $"Wstawiono plik {file} z {hashes.Count} odciskami."
                        });
                    }
                    catch (Exception e)
                    {
                        _notificationService.Notify(new NotificationMessage
                            { Duration = 1000, Severity = NotificationSeverity.Error, Summary = e.Message });
                    }
                }
            }
        }
    }
    
    public async Task FileExists(string file)
    {
        if (File.Exists(file))
        {
            if (Path.GetExtension(file) == ".wav")
            {
                _notificationService.Notify(new NotificationMessage
                    { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Plik poprawny!" });
            }
            else
            {
                _notificationService.Notify(new NotificationMessage
                {
                    Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Niepoprawne rozszerzenie pliku!"
                });
            }
        }
        else
        {
            _notificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
        }
    }
    
    // Function checks whether audio contains matching patterns.
    public async Task<List<ResultEntry>> FindMatches(string file, double confidence)
    {
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

        return matches.OrderBy(x => x.QueryMatchStartsAt).ToList();
    }
}