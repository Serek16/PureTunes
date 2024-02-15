using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using PureTunes.Exceptions;
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
            _logger.LogError("Couldn't establish connection with Emy storage. Make sure that Emy Docker container is running");
        }
    }

    public void AddTrack(TrackInfo track, AVHashes hashes)
    {
        try
        {
            _modelService.Insert(track, hashes);
        }
        catch (SocketException e)
        {
            throw new DockerConnectionException();
        }
    }

    public IEnumerable<TrackInfo> GetAllTracks()
    {
        try
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
        catch (SocketException e)
        {
            throw new DockerConnectionException();
        }
    }

    public void DeleteAllTracks()
    {
        try
        {
            var readAllTracks = _modelService.ReadAllTracks();
            foreach (var track in readAllTracks)
            {
                _modelService.DeleteTrack(track.Id);
                _logger.LogInformation("deleted " + track.Id);
            }
        }
        catch (SocketException e)
        {
            throw new DockerConnectionException();
        }
    }

    public IModelService GetModel()
    {
        return _modelService;
    }

    public void DeleteTrackById(string trackId)
    {
        try
        {
            _modelService.DeleteTrack(trackId);
        }
        catch (SocketException e)
        {
            throw new DockerConnectionException();
        }
    }

    public int Count()
    {
        return _modelService.GetTrackIds().Count();
    }

    private bool IsDockerConnected()
    {
        try
        {
            // If the function throws an exception it means service can't connect to the EmySound docker.
            _modelService.ReadAllTracks();
            return true;
        }
        catch (SocketException e)
        {
            return false;
        }
    }
}