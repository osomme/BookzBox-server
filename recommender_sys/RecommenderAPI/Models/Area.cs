/// <summary>
/// Represents a square area starting at a set location.
/// </summary>
public struct Area
{
    public readonly double lat;
    public readonly double lng;
    /// <summary>
    /// The distance in each direction from the mid point.
    /// </summary>
    public readonly double d;

    public Area(double lat, double lng, double d)
    {
        this.lat = lat;
        this.lng = lng;
        this.d = d;
    }
}