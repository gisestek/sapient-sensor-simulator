namespace SapientSensorSimulator;

/// <summary>
/// A target that moves in a circle of <see cref="RadiusMetres"/> around the sensor's origin at
/// constant angular speed — enough to exercise a Fusion Node's position + velocity handling
/// without needing a real trajectory model.
///
/// Position is a pure function of wall-clock time (<paramref name="absoluteSeconds"/> in
/// <see cref="GetLocalState"/> — Unix time, not "seconds since this process started"). Two
/// simulator instances with the same target parameters and synchronised clocks therefore compute
/// the exact same trajectory at the exact same moment, even on different machines — which is the
/// point: a Fusion Node's TrackManager can then genuinely associate them as the same physical
/// target by position+time, the way independent real sensors looking at the same target would.
/// </summary>
public sealed class SimulatedTarget
{
    private const double EarthRadiusMetres = 6_371_000;

    public required string ObjectId { get; init; }
    public double RadiusMetres { get; init; } = 100;
    public double AngularSpeedRadPerSec { get; init; } = 0.1;
    public double AltitudeMetres { get; init; } = 50;
    public double PhaseOffsetRad { get; init; }

    /// <summary>Local east/north/up metres and ENU velocity, before any noise or lat/lon conversion.</summary>
    public (double East, double North, double Up, double EastRate, double NorthRate, double UpRate) GetLocalState(double absoluteSeconds)
    {
        var angle = AngularSpeedRadPerSec * absoluteSeconds + PhaseOffsetRad;
        var east = RadiusMetres * Math.Cos(angle);
        var north = RadiusMetres * Math.Sin(angle);
        var eastRate = -RadiusMetres * AngularSpeedRadPerSec * Math.Sin(angle);
        var northRate = RadiusMetres * AngularSpeedRadPerSec * Math.Cos(angle);

        return (east, north, AltitudeMetres, eastRate, northRate, 0);
    }

    public static (double Lat, double Lon) ToLatLon(double originLatDeg, double originLonDeg, double east, double north)
    {
        var lat = originLatDeg + north / EarthRadiusMetres * 180.0 / Math.PI;
        var lon = originLonDeg + east / (EarthRadiusMetres * Math.Cos(originLatDeg * Math.PI / 180.0)) * 180.0 / Math.PI;
        return (lat, lon);
    }

    /// <summary>Converts a north-south distance to the equivalent latitude span — for reporting
    /// a metres-based error as Location.y_error, which is in the same unit as Location.Y (degrees).</summary>
    public static double MetresToDegreesLat(double metres) => metres / EarthRadiusMetres * 180.0 / Math.PI;

    /// <summary>Converts an east-west distance to the equivalent longitude span at the given
    /// latitude — for reporting a metres-based error as Location.x_error.</summary>
    public static double MetresToDegreesLon(double metres, double atLatitudeDeg) =>
        metres / (EarthRadiusMetres * Math.Cos(atLatitudeDeg * Math.PI / 180.0)) * 180.0 / Math.PI;
}
