﻿using System.Collections.Generic;
using System.Linq;
using SoundFingerprinting;
using SoundFingerprinting.Data;
using SoundFingerprinting.InMemory;

namespace PureTunes.Services;

public class InMemoryModelServiceAdapter : IFingerprintStorage
{
    private readonly InMemoryModelService _modelService;

    public InMemoryModelServiceAdapter()
    {
        _modelService = new InMemoryModelService();
    }

    public void AddTrack(TrackInfo track, AVHashes hashes)
    {
        _modelService.Insert(track, hashes);
    }

    public IEnumerable<TrackInfo> GetAllTracks()
    {
        var trackIds = _modelService.GetTrackIds();
        var trackInfoList = new List<TrackInfo>();
        foreach (var trackId in trackIds)
        {
            trackInfoList.Add(_modelService.ReadTrackById(trackId));
        }

        return trackInfoList;
    }

    public void DeleteAllTracks()
    {
        var trackIds = _modelService.GetTrackIds();
        foreach (var trackId in trackIds)
        {
            _modelService.DeleteTrack(trackId);
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

    public int Count()
    {
        return _modelService.GetTrackIds().Count();
    }
}