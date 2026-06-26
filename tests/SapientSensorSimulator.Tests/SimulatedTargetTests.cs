using SapientSensorSimulator;
using Xunit;

namespace SapientSensorSimulator.Tests;

public class SimulatedTargetTests
{
    [Fact]
    public void GetState_AtPhaseZero_IsDueEastOfOriginAtRadius()
    {
        var target = new SimulatedTarget
        {
            ObjectId = "t1",
            RadiusMetres = 1000,
            AngularSpeedRadPerSec = 0.1,
            AltitudeMetres = 50,
            PhaseOffsetRad = 0
        };

        var (lat, lon, alt, eastRate, northRate) = target.GetState(originLatDeg: 60.0, originLonDeg: 25.0, elapsedSeconds: 0);

        // angle = 0 => east = radius, north = 0 => longitude increases, latitude unchanged.
        Assert.True(lon > 25.0);
        Assert.Equal(60.0, lat, precision: 6);
        Assert.Equal(50, alt, precision: 6);

        // Tangential velocity at angle 0 points north (east_rate ~ 0, north_rate > 0).
        Assert.Equal(0, eastRate, precision: 6);
        Assert.True(northRate > 0);
    }

    [Fact]
    public void GetState_DifferentTargets_ProduceDistinctPositions()
    {
        var a = new SimulatedTarget { ObjectId = "a", RadiusMetres = 100, PhaseOffsetRad = 0 };
        var b = new SimulatedTarget { ObjectId = "b", RadiusMetres = 200, PhaseOffsetRad = 1 };

        var stateA = a.GetState(60.0, 25.0, 10);
        var stateB = b.GetState(60.0, 25.0, 10);

        Assert.NotEqual(stateA, stateB);
    }
}
