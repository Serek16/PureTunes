using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SoundFingerprinting;
using SoundFingerprinting.Data;
using SoundFingerprinting.Emy;

namespace PureTunes.Services;

public class EmyDockerModelServiceAdapter : IFingerprintStorage
{
    private readonly EmyModelService _modelService;
    private readonly ILogger<EmyDockerModelServiceAdapter> _logger;

    public EmyDockerModelServiceAdapter(ILogger<EmyDockerModelServiceAdapter> logger, string host = "localhost", int port = 3399)
    {
        _modelService = EmyModelService.NewInstance(host, port);
        _logger = logger;
        if (!IsDockerConnected())
        {
            _logger.LogError("Couldn't establish connection. Make sure that Emy Docker container is running");
        }
    }

    public void AddTrack(TrackInfo track, AVHashes hashes)
    {
        _modelService.Insert(track, hashes);
    }

    public IEnumerable<TrackInfo> GetAllTracks()
    {
        return _modelService.ReadAllTracks().Select(track =>
            new TrackInfo(
                track.Id,
                track.Title,
                track.Artist,
                track.MetaFields,
                track.MediaType
            ));
    }

    public void DeleteAllTracks()
    {
        var readAllTracks = _modelService.ReadAllTracks();
        foreach (var track in readAllTracks)
        {
            _modelService.DeleteTrack(track.Id);
        }
    }

    public IModelService GetModel()
    {
        return _modelService;
    }

    public void DeleteTrackById(string trackId)
    {
        _modelService.DeleteTrack(trackId);
    }

    private bool IsDockerConnected()
    {
        try
        {
            // If the function throws an exception it means service can't connect to the EmySound docker.
            _modelService.ReadAllTracks();
            return true;
        }
        catch (System.Net.Sockets.SocketException e)
        {
            return false;
        }
    }
}