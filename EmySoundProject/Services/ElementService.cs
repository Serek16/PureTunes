using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SoundFingerprinting.Data;

namespace EmySoundProject.Services;

public class ElementService
{
    [Inject]
    protected AFMService AFMService { get; set; }

    private List<TrackInfo> _elements = new();

    public List<TrackInfo> Elements => _elements;

    public void AddElementList(IEnumerable<TrackInfo> trackInfoList)
    {
        foreach (var t in trackInfoList)
        {
            if (!_elements.Contains(t))
            {
                _elements.Add(t);
            }
        }
    }

    public void AddElement(TrackInfo trackInfo)
    {
        _elements.Add(trackInfo);
    }

    public void RemoveAll()
    {
        _elements.Clear();
    }

    public void Remove(TrackInfo trackInfo)
    {
        _elements.Remove(trackInfo);
    }
}