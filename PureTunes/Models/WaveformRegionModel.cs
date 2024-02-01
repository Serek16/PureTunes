namespace PureTunes.Models;

public class WaveformRegionModel
{
    public double Start { get; }

    public double End { get; }

    public string RegionName { get; }

    public WaveformRegionModel(double start, double end, string regionName)
    {
        Start = start;
        End = end;
        RegionName = regionName;
    }
}