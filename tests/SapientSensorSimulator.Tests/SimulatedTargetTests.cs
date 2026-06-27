using SapientSensorSimulator;
using Xunit;

namespace SapientSensorSimulator.Tests;

public class SimulatedTargetTests
{
    [Fact]
    public void GetLocalState_AtPhaseZero_IsDueEastOfOriginAtRadius()
    {
        var target = new SimulatedTarget
        {
            ObjectId = "t1",
            RadiusMetres = 1000,
            AngularSpeedRadPerSec = 0.1,
            AltitudeMetres = 50,
            PhaseOffsetRad = 0
        };

        var (east, north, up, eastRate, northRate, upRate) = target.GetLocalState(absoluteSeconds: 0);

        // angle = 0 => east = radius, north = 0.
        Assert.Equal(1000, east, precision: 6);
        Assert.Equal(0, north, precision: 6);
        Assert.Equal(50, up, precision: 6);
        Assert.Equal(0, upRate, precision: 6);

        // Tangential velocity at angle 0 points north (east_rate ~ 0, north_rate > 0).
        Assert.Equal(0, eastRate, precision: 6);
        Assert.True(northRate > 0);
    }

    [Fact]
    public void GetLocalState_DifferentTargets_ProduceDistinctPositions()
    {
        var a = new SimulatedTarget { ObjectId = "a", RadiusMetres = 100, PhaseOffsetRad = 0 };
        var b = new SimulatedTarget { ObjectId = "b", RadiusMetres = 200, PhaseOffsetRad = 1 };

        var stateA = a.GetLocalState(10);
        var stateB = b.GetLocalState(10);

        Assert.NotEqual(stateA, stateB);
    }

    [Fact]
    public void GetLocalState_SameParametersAndTime_ProducesIdenticalPosition()
    {
        // The whole point: two independent SimulatedTarget instances (e.g. on different
        // machines) with the same parameters and the same wall-clock time must compute the
        // exact same trajectory point, so a Fusion Node can associate them as one target.
        var a = new SimulatedTarget { ObjectId = "a", RadiusMetres = 250, AngularSpeedRadPerSec = 0.2, PhaseOffsetRad = 0.5 };
        var b = new SimulatedTarget { ObjectId = "b-different-process", RadiusMetres = 250, AngularSpeedRadPerSec = 0.2, PhaseOffsetRad = 0.5 };

        var stateA = a.GetLocalState(12345.678);
        var stateB = b.GetLocalState(12345.678);

        Assert.Equal(stateA, stateB);
    }

    [Fact]
    public void ToLatLon_EastIncreasesLongitude_NorthIncreasesLatitude()
    {
        var (lat, lon) = SimulatedTarget.ToLatLon(originLatDeg: 60.0, originLonDeg: 25.0, east: 1000, north: 0);

        Assert.True(lon > 25.0);
        Assert.Equal(60.0, lat, precision: 6);
    }
}
