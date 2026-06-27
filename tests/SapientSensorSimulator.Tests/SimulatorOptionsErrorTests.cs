using SapientSensorSimulator;
using Xunit;

namespace SapientSensorSimulator.Tests;

public class SimulatorOptionsErrorTests
{
    [Fact]
    public void ResolveReportedErrorMetres_NoNoiseNoOverride_ReturnsNull()
    {
        var options = new SimulatorOptions { Host = "h", Port = 1 };

        Assert.Null(options.ResolveReportedErrorMetres("East"));
    }

    [Fact]
    public void ResolveReportedErrorMetres_AutoMode_UsesNoiseMagnitude()
    {
        var options = Parse("--port", "1", "--noise", "East:Gaussian:3.5");

        Assert.Equal(3.5, options!.ResolveReportedErrorMetres("East"));
    }

    [Fact]
    public void ResolveReportedErrorMetres_FixedOverride_TakesPrecedenceOverNoise()
    {
        var options = Parse("--port", "1", "--noise", "East:Gaussian:3.5", "--error", "East:10");

        Assert.Equal(10, options!.ResolveReportedErrorMetres("East"));
    }

    [Fact]
    public void ResolveReportedErrorMetres_FixedOverrideWithoutNoise_StillReportsError()
    {
        // "Fixed" mode shouldn't require --noise to also be set — a sensor can have a known,
        // declared error characteristic without the simulator actually perturbing the data.
        var options = Parse("--port", "1", "--error", "Altitude:2.0");

        Assert.Equal(2.0, options!.ResolveReportedErrorMetres("Altitude"));
    }

    [Fact]
    public void ResolveReportedErrorMetres_NoneKindNoise_ReturnsNull()
    {
        var options = Parse("--port", "1", "--noise", "East:None:5.0");

        Assert.Null(options!.ResolveReportedErrorMetres("East"));
    }

    private static SimulatorOptions? Parse(params string[] args) => SimulatorOptions.Parse(args);
}
