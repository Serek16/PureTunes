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

namespace EmyProject.CustomService
{
    public class EmyService
    {
        private readonly IConfiguration configuration;
        private readonly NotificationService notificationService;
        private string datasetPath;

        private readonly EmyModelService ModelService;
        private readonly IAudioService AudioService;

        public List<PathModel> list = new List<PathModel>();
        public EmyService(IConfiguration _configuration, NotificationService _notificationService)
        {
            configuration = _configuration;
            notificationService = _notificationService;
            datasetPath = configuration["Dataset"];

            ModelService = EmyModelService.NewInstance("localhost", 3399);
            AudioService = new SoundFingerprintingAudioService();

            CheckDataset();
        }

        private void CheckDataset()
        {
            //Sprawdzanie czy istnieje katalog.
            if (!Directory.Exists(datasetPath))
            {
                notificationService.Notify(new NotificationMessage { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Katalog nie istnieje!" });
            }
            else
            {
                //Sprawdzanie czy katalog zawiera podkatalogi z utworami.
                if (Directory.GetDirectories(datasetPath).Length == 0)
                {
                    notificationService.Notify(new NotificationMessage { Duration = 2000, Severity = NotificationSeverity.Error, Summary = "Brak katalogów dataset!" });
                }
                else
                {
                    foreach (var catalog in Directory.GetDirectories(datasetPath).ToList())
                    {
                        foreach (var file in Directory.GetFiles(catalog).ToList())
                        {
                            if (Path.GetExtension(file) == ".wav")
                            {
                                //Przypisanie listy katalogów do listy "list" po znalezieniu przynajmniej 1 pliku wav.
                                list.Add(new PathModel {  Path = catalog });
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        //Funkcja zwracająca długośc pliku.
        public double GetWavFileDuration(string fileName)
        {
            using (var wf = new WaveFileReader(fileName))
            {
                return wf.TotalTime.TotalSeconds;
            }
        }

        public async Task AddDataset(string path)
        {
            //Usunięcie wszystkich istniejacych dataset z bazy EmySound.
            foreach (var item in ModelService.ReadAllTracks())
            {
                ModelService.DeleteTrack(item.Id);
            }

            //Iteracja po wszystkich plikach.
            foreach (string file in Directory.GetFiles(path))
            {
                //Sprawdzanie czy plik jest w formacie "wav" oraz czy jego długośc przekracza 2 sekundy.
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
                                .UsingServices(AudioService)
                                .Hash();

                            //Wstawienie pliku do bazy EmySound.
                            ModelService.Insert(track, hashes);
                            notificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Success, Summary = $"Wstawiono plik {file} z {hashes.Count} odciskami." });
                        }
                        catch (Exception e)
                        {
                            notificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Error, Summary = e.Message });
                        }
                    }
                }
            }
        }

        //Sprawdzanie czy plik istnieje.
        public async Task FileExists(string file)
        {
            if (File.Exists(file))
            {
                //Sprawdzanie czy ma odpowiedni format.
                if (Path.GetExtension(file) == ".wav")
                {
                    notificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Success, Summary = "Plik poprawny!" });
                }
                else
                {
                    notificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Niepoprawne rozszerzenie pliku!" });
                }
            }
            else
            {
                notificationService.Notify(new NotificationMessage { Duration = 1000, Severity = NotificationSeverity.Error, Summary = "Plik nie istnieje!" });
            }
        }

        //Funkcja do sprawdzania czy utwór zwiera pasujące wzorce.
        public async Task<List<ResultEntry>> FindMatches(string file, double confidence)
        {
            var result = await QueryCommandBuilder.Instance
            .BuildQueryCommand()
            .From(file)
            .UsingServices(ModelService, AudioService)
            .Query();

            List<ResultEntry> matches = new List<ResultEntry>();

            if (result.ContainsMatches)
            {
                foreach (var (entry, _) in result.ResultEntries)
                {
                    //Warunek sprawdzania minimalnego % rezulatutu.
                    if (entry?.Confidence > confidence)
                    {
                        matches.Add(entry);
                    }
                }
            }
            return matches.OrderBy(x => x.Confidence).ToList();
        }
    }
}
