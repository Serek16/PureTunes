using System;
using System.Collections.Generic;
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

    private List<PathModel> GetDirectories()
    {
        return EmyService.DatasetDirList;
    }

    private List<PathModel> GetExaminedFilePathList()
    {
        return EmyService.ExaminedFileList;
    }

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
}