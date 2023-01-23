using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Radzen;
using EmyProject.CustomService;
using Microsoft.AspNetCore.Components;
using EmyProject.CustomService.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EmyProject.Pages;

public partial class MainPageComponent
{
    [Inject] protected EmyService EmyService { get; set; }

    [Inject] protected AudioService AudioService { get; set; }

    [Inject] protected ILogger<MainPageComponent> Logger { get; set; }


    private async Task UpdateEmy()
    {
        await EmyService.AddDataset(DirectoryPath);
    }

    private async Task CheckFile()
    {
        await EmyService.IsFilePathCorrect(FilePath);
    }

    private async Task<string> GetResult()
    {
        if (!await EmyService.IsFilePathCorrect(FilePath))
        {
            Logger.LogError(
                "Invalid file path. Please provide a valid file path - {FilePath}. File needs to have WAV format",
                FilePath);
            NotificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
            
            return string.Empty;
        }
        
        Logger.LogInformation("Started processing files.");
        NotificationService.Notify(new NotificationMessage
            { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Rozpoczęto procesowanie plików!" });


        var time0 = DateTime.Now;

        var result = await EmyService.FindMatches(FilePath, Confidence / 100d);
        await AudioService.FileGenerate(result, FilePath);

        var timeSpan = DateTime.Now.Subtract(time0);

        Logger.LogInformation("The process has been successfully completed in {TimeSpan} seconds.",
            timeSpan.ToString("ss'.'ff"));
        NotificationService.Notify(new NotificationMessage
            { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Wygenerowano rezultat!" });
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }

    private List<PathModel> GetDirectories()
    {
        if (!Directory.Exists(_datasetPath))
        {
            Logger.LogError("Directory \"{DatasetPath}\" doesn't exist.", _datasetPath);
            NotificationService.Notify(new NotificationMessage
                { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Katalog nie istnieje!" });

            return new List<PathModel>();
        }

        // Check if the directory contains subdirectories with audio files.
        if (Directory.GetDirectories(_datasetPath).Length == 0)
        {
            Logger.LogError("Directory \"{DatasetPath}\" doesn't contain any subdirectories.", _datasetPath);
            NotificationService.Notify(new NotificationMessage
                { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Brak katalogów dataset!" });

            return new List<PathModel>();
        }

        var datasetDirList = new List<PathModel>();

        foreach (var catalog in Directory.GetDirectories(_datasetPath).ToList())
        {
            foreach (var file in Directory.GetFiles(catalog).ToList())
            {
                if (Path.GetExtension(file) == ".wav")
                {
                    // Add subdirectory to DatasetDirList if a subdirectory contains at least 1 .wav file.
                    datasetDirList.Add(new PathModel(catalog));
                    break;
                }
            }
        }

        return datasetDirList;
    }

    private List<PathModel> GetExaminedFiles()
    {
        if (!Directory.Exists(_examinedFileDirectoryPath))
        {
            Logger.LogError("Directory \"{ExaminedFilePathDataset}\" doesn't exist.", _examinedFileDirectoryPath);
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
}