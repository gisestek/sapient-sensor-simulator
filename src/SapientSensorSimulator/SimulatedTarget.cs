namespace SapientSensorSimulator;

/// <summary>
/// A target that moves in a circle of <see cref="RadiusMetres"/> around the sensor's origin
/// at constant angular speed — enough to exercise a Fusion Node's position + velocity handling
/// without needing a real trajectory model.
/// </summary>
public sealed class SimulatedTarget
{
    private const double EarthRadiusMetres = 6_371_000;

    public required string ObjectId { get; init; }
    public double RadiusMetres { get; init; } = 100;
    public double AngularSpeedRadPerSec { get; init; } = 0.1;
    public double AltitudeMetres { get; init; } = 50;
    public double PhaseOffsetRad { get; init; }

    public (double Lat, double Lon, double Alt, double EastRate, double NorthRate) GetState(
        double originLatDeg, double originLonDeg, double elapsedSeconds)
    {
        var angle = AngularSpeedRadPerSec * elapsedSeconds + PhaseOffsetRad;
        var east = RadiusMetres * Math.Cos(angle);
        var north = RadiusMetres * Math.Sin(angle);
        var eastRate = -RadiusMetres * AngularSpeedRadPerSec * Math.Sin(angle);
        var northRate = RadiusMetres * AngularSpeedRadPerSec * Math.Cos(angle);

        var lat = originLatDeg + north / EarthRadiusMetres * 180.0 / Math.PI;
        var lon = originLonDeg + east / (EarthRadiusMetres * Math.Cos(originLatDeg * Math.PI / 180.0)) * 180.0 / Math.PI;

        return (lat, lon, AltitudeMetres, eastRate, northRate);
    }
}
