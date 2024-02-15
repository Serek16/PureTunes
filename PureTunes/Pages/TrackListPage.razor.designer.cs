using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using PureTunes.Exceptions;
using PureTunes.Services;
using Radzen;
using Radzen.Blazor;
using SoundFingerprinting.Data;
namespace PureTunes.Pages;

public partial class TrackListPageComponent : ComponentBase
{
    [Inject] protected IFingerprintStorage FingerprintStorage { get; set; }

    [Inject] protected AFMService AFMService { get; set; }

    [Inject] protected NotificationService NotificationService { get; set; }

    [Inject] protected ILogger<MainPageComponent> Logger { get; set; }

    protected RadzenDataGrid<TrackInfo> TrackInfoDataGrid { get; set; }

    protected List<TrackInfo> TrackList;

    protected int Index = 0;

    protected ElementReference FileInput;
    protected string SelectedFilePath;

    protected bool RemoveAll_isBusy;

    protected bool RemoveAllFromPage_isBusy;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            TrackList = FingerprintStorage.GetAllTracks().ToList();
        }
        catch (DockerConnectionException e)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Duration = 10000, Severity = NotificationSeverity.Error, Summary = "Nie można połączyć się Dockerem!"
            });
            Logger.LogError("Couldn't establish connection with Emy Storage. Make sure that Emy Docker container is running");
        }
    }

    protected async Task RemoveAll_onClick(MouseEventArgs args)
    {
        RemoveAll_isBusy = true;
        RemoveAllTracks();
        RemoveAll_isBusy = false;
    }

    protected async Task RemoveAllFromPage_onClick(MouseEventArgs args)
    {
        RemoveAllFromPage_isBusy = true;
        RemoveTracksFromSinglePage();
        RemoveAllFromPage_isBusy = false;
    }

    protected async Task RemoveSpecificTrack_onClick(MouseEventArgs arg, TrackInfo trackInfo)
    {
        RemoveSpecificTrack(trackInfo);
    }
}