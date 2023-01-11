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

    private List<PathModel> GetCatalog()
    {
        return EmyService.DatasetDirList;
    }

    private async Task UpdateEmy()
    {
        await EmyService.AddDataset(Path);
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
                "Invalid file path. Please provide a valid file path - {FilePath}. File needs to have .wav format",
                FilePath);
            NotificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
        }

        Logger.LogInformation("Started processing files.");
        NotificationService.Notify(new NotificationMessage
            { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Rozpoczęto procsowanie plików!" });
        
        var result = await EmyService.FindMatches(FilePath, Confidence / 100d);
        AudioService.FileGenerate(result, FilePath);

        Logger.LogInformation("The process has been successfully completed.");
        NotificationService.Notify(new NotificationMessage
            { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Wygenerowano rezultat!" });
        return JsonConvert.SerializeObject(result, Formatting.Indented);
    }
}