using System.Collections.Generic;
using System.Threading.Tasks;
using Radzen;
using EmyProject.CustomService;
using Microsoft.AspNetCore.Components;
using EmyProject.CustomService.Model;
using Newtonsoft.Json;

namespace EmyProject.Pages
{
    public partial class MainPageComponent
    {
        [Inject] protected EmyService EmyService { get; set; }
        [Inject] protected AudioService AudioService { get; set; }

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
            await EmyService.FileExists(FilePath);
        }

        private async Task<string> GetResult()
        {
            if (FilePath.Length == 0)
            {
                NotificationService.Notify(new NotificationMessage
                    { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
            }

            var result = await EmyService.FindMatches(FilePath, Confidence / 100d);
            AudioService.FileGenerate(result, FilePath);
            NotificationService.Notify(new NotificationMessage
                { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Wygenerowano rezultat!" });
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
    }
}