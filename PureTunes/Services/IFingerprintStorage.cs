﻿using System.Collections.Generic;
using SoundFingerprinting;
using SoundFingerprinting.Data;

namespace PureTunes.Services;

public interface IFingerprintStorage
{
    void AddTrack(TrackInfo track, AVHashes hashes);

    IEnumerable<TrackInfo> GetAllTracks();

    void DeleteAllTracks();

    IModelService GetModel();

    void DeleteTrackById(string trackId);

    int Count();
}