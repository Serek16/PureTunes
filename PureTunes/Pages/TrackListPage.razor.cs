using System;
using System.Globalization;
using System.Linq;
using PureTunes.Exceptions;
using SoundFingerprinting.Data;

namespace PureTunes.Pages;

public partial class TrackListPageComponent
{
    protected static string FormatDate(string insertDate)
    {
        if (DateTime.TryParseExact(insertDate, "yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out DateTime dateTime))
        {
            return dateTime.ToString("dd.MM.yyyy HH:mm:ss");
        }

        return string.Empty; // Return an empty string if parsing fails
    }

    private void RemoveAllTracks()
    {
        TrackList.Clear();
        FingerprintStorage.DeleteAllTracks();
        TrackInfoDataGrid.Reload();
    }

    private void RemoveTracksFromSinglePage()
    {
        foreach (var trackInfo in TrackInfoDataGrid.View)
        {
            FingerprintStorage.DeleteTrackById(trackInfo.Id);
        }
        TrackInfoDataGrid.Reload();
    }

    private void RemoveSpecificTrack(TrackInfo trackInfo)
    {
        if (!TrackList.Contains(trackInfo))
        {
            return;
        }

        TrackList.Remove(trackInfo);
        FingerprintStorage.DeleteTrackById(trackInfo.Id);
        TrackInfoDataGrid.Reload();
    }

    protected void HandleTracksAdded()
    {
        TrackList = FingerprintStorage.GetAllTracks().ToList();
        TrackInfoDataGrid.Data = TrackList;
        TrackInfoDataGrid.Reload();
    }

    protected void ResetIndex(bool shouldReset)
    {
        if (shouldReset)
        {
            Index = 0;
        }
    }
}