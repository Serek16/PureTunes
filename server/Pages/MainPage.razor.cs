using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Radzen;
using Radzen.Blazor;
using EmyProject.CustomService;
using Microsoft.AspNetCore.Components;
using EmyProject.CustomService.Model;
using Newtonsoft.Json;
using SoundFingerprinting.Query;

namespace EmyProject.Pages
{
    public partial class MainPageComponent
    {
        [Inject]
        protected EmyService emyService { get; set; }
        [Inject]
        protected AudioService audioService { get; set; }

        public List<PathModel> GetCatalog()
        {
            return emyService.list;
        }

        public async Task UpdateEmy()
        {
            await emyService.AddDataset(Path);
        }

        public async Task CheckFile()
        {
            await emyService.FileExists(FilePath);
        }

        public async Task<string> GetResult()
        {
            if (FilePath.Length == 0)
            {
                NotificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
            }
            var result = await emyService.FindMatches(FilePath, Confidence);
            audioService.FileGenerate(result, FilePath);
            NotificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Wygenerowano rezultat!" });
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
    }
}
