using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PureTunes.Services;
using Radzen.Blazor;
using SoundFingerprinting.Data;
namespace PureTunes.Pages;

public partial class TrackListPageComponent : ComponentBase
{
    [Inject]
    protected IFingerprintStorage FingerprintStorage { get; set; }

    [Inject]
    protected AFMService AFMService { get; set; }

    protected RadzenDataGrid<TrackInfo> DataGrid { get; set; }

    protected List<TrackInfo> TrackList;

    protected int Index;

    protected ElementReference FileInput;
    protected string SelectedFilePath;

    protected override async Task OnInitializedAsync()
    {
        TrackList = FingerprintStorage.GetAllTracks().ToList();
    }

    protected async Task RemoveAll_onClick(MouseEventArgs args)
    {
        RemoveAllTracks();
    }

    protected async Task RemoveSpecificTrack_onClick(MouseEventArgs arg, TrackInfo trackInfo)
    {
        RemoveSpecificTrack(trackInfo);
    }
}