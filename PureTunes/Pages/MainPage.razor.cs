using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PureTunes.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PureTunes.Exceptions;
using PureTunes.Models;
using PureTunes.Services;
using Radzen;
using SoundFingerprinting.Query;

namespace PureTunes.Pages;

public partial class MainPageComponent
{
    protected RenderFragment WaveformDisplayRender;

    private WaveformDisplay _waveformDisplay;

    protected List<ResultEntry> ResultList = new();

    private async Task FindMatches()
    {
        IsFingerprintingAndMatching = true;
        try
        {
            if (!(await AfmService.ReadAllLoadedTracks()).Any())
            {
                Logger.LogError(
                    "Emy Sound database is empty. Please provide at least one track.");
                NotificationService.Notify(new NotificationMessage
                    { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Baza Emy Sound jest pusta!" });

                return;
            }
        }
        catch (DockerConnectionException e)
        {
            Logger.LogError($"Can't start the task. {e.Message}");
            NotificationService.Notify(new NotificationMessage
            {
                Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Nie można połączyć się Dockerem!"
            });

            return;
        }

        if (!await IsFilePathCorrect(FilePath))
        {
            return;
        }

        Logger.LogInformation("Started processing files.");
        NotificationService.Notify(new NotificationMessage
            { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Rozpoczęto procesowanie plików!" });


        var time0 = DateTime.Now;
        ResultList = await AfmService.FindMatches(FilePath, Confidence / 100d);
        await RenderResultWaveform(FilePath);
        var timeSpan = DateTime.Now.Subtract(time0);

        Logger.LogInformation("The matching process has been successfully completed in {TimeSpan} seconds.",
            timeSpan.ToString("ss'.'ff"));
        NotificationService.Notify(new NotificationMessage
            { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Wygenerowano rezultat!" });

        IsFingerprintingAndMatching = false;
    }

    private async Task ExtractAudioClips(List<ResultEntry> matches)
    {
        NotificationService.Notify(new NotificationMessage
        {
            Duration = 1000, Severity = NotificationSeverity.Success,
            Summary = "Rozpoczęto generowanie plików!"
        });

        if (!matches.Any())
        {
            Logger.LogInformation("There was no matching tracks. The process stops.");
            NotificationService.Notify(new NotificationMessage
            {
                Duration = 1000, Severity = NotificationSeverity.Success,
                Summary = "Nie znaleziono żadnych pasujących reklam w audycji!"
            });
            return;
        }

        IsExctracting = true;
        var waveformRegionList = await _waveformDisplay.GetWaveformRegionList();
        await AudioExtractionService.AssignRegionsToCut(waveformRegionList, ResultList.First().QueryLength);
        await AudioExtractionService.FileGenerate(FilePath);
        IsExctracting = false;

        NotificationService.Notify(new NotificationMessage
        {
            Duration = 1000, Severity = NotificationSeverity.Success,
            Summary = "Pomyślnie wygenerowano plik!"
        });
    }

    private async Task RenderResultWaveform(string fileToCutPath)
    {
        var waveformRegionModels = await AudioExtractionService.AssignRegions(ResultList);
        var peaks = AudioPeaksReader.ReadAudioPeaks(fileToCutPath, 0.01);
        WaveformDisplayRender = builder =>
        {
            builder.OpenComponent<WaveformDisplay>(0);
            builder.AddAttribute(0, "AudioFilePath", fileToCutPath);
            builder.AddAttribute(0, "WaveformRegionModels", waveformRegionModels);
            builder.AddAttribute(0, "AudioPeaks", peaks);
            builder.AddComponentReferenceCapture(1, inst => _waveformDisplay = (WaveformDisplay)inst);
            builder.CloseComponent();
        };
        StateHasChanged(); // Request UI to be re-rendered. @waveformDisplay is now visible
    }

    private List<PathModel> GetExaminedFiles()
    {
        if (!Directory.Exists(_examinedFileDirectoryPath))
        {
            Logger.LogError($"Directory \"{_examinedFileDirectoryPath}\" doesn't exist.");
            NotificationService.Notify(new NotificationMessage
                { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Katalog nie istnieje!" });

            return new List<PathModel>();
        }

        var examinedFileList = new List<PathModel>();

        foreach (var file in Directory.GetFiles(_examinedFileDirectoryPath).ToList())
        {
            if (Path.GetExtension(file) == ".wav")
            {
                examinedFileList.Add(new PathModel(file));
            }
        }

        return examinedFileList;
    }

    private async Task<bool> IsFilePathCorrect(string filePath)
    {
        if (File.Exists(filePath))
        {
            if (Path.GetExtension(filePath) == ".wav")
            {
                Logger.LogInformation($"File \"{filePath}\" is correct.");
                NotificationService.Notify(new NotificationMessage
                    { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Plik poprawny!" });

                return true;
            }

            Logger.LogError($"File \"{filePath}\" doesn't have correct format The file needs to be in WAV format.");
            NotificationService.Notify(new NotificationMessage
            {
                Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Niepoprawne rozszerzenie pliku!"
            });
        }
        else
        {
            Logger.LogError($"File \"{filePath}\" doesn't exist");
            NotificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
        }

        return false;
    }
}