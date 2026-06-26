using Google.Protobuf;
using SapientMsg.BsiFlex335V20;
using SapientSensorSimulator;
using Xunit;

namespace SapientSensorSimulator.Tests;

public class MessageFactoryTests
{
    [Fact]
    public void BuildRegistration_FillsAllMandatoryFields()
    {
        var nodeId = Guid.NewGuid().ToString();
        var message = MessageFactory.BuildRegistration(nodeId, "Test Sensor");

        var roundTripped = SapientMessage.Parser.ParseFrom(message.ToByteArray());

        Assert.Equal(SapientMessage.ContentOneofCase.Registration, roundTripped.ContentCase);
        var registration = roundTripped.Registration;

        Assert.Equal("BSI Flex 335 v2.0", registration.IcdVersion);
        Assert.NotEmpty(registration.NodeDefinition);
        Assert.NotEmpty(registration.Capabilities);
        Assert.NotNull(registration.StatusDefinition?.StatusInterval);
        Assert.NotEmpty(registration.ModeDefinition);
        Assert.NotNull(registration.ModeDefinition[0].SettleTime);
        Assert.NotNull(registration.ModeDefinition[0].Task);
        Assert.NotEmpty(registration.ModeDefinition[0].Task.RegionDefinition.RegionType);
        Assert.NotEmpty(registration.ModeDefinition[0].Task.RegionDefinition.RegionArea);
        Assert.NotEmpty(registration.ConfigData);
    }

    [Fact]
    public void BuildDetectionReport_RoundTripsLocationAndVelocity()
    {
        var nodeId = Guid.NewGuid().ToString();
        var message = MessageFactory.BuildDetectionReport(
            nodeId, objectId: "obj-42", lat: 60.5, lon: 25.1, alt: 42, eastRate: 3, northRate: -2);

        var roundTripped = SapientMessage.Parser.ParseFrom(message.ToByteArray());

        Assert.Equal(SapientMessage.ContentOneofCase.DetectionReport, roundTripped.ContentCase);
        var report = roundTripped.DetectionReport;

        Assert.Equal("obj-42", report.ObjectId);
        Assert.Equal(DetectionReport.LocationOneofOneofCase.Location, report.LocationOneofCase);
        Assert.Equal(25.1, report.Location.X, precision: 6);
        Assert.Equal(60.5, report.Location.Y, precision: 6);
        Assert.Equal(42, report.Location.Z, precision: 6);
        Assert.Equal(LocationCoordinateSystem.LatLngDegM, report.Location.CoordinateSystem);

        Assert.Equal(DetectionReport.VelocityOneofOneofCase.EnuVelocity, report.VelocityOneofCase);
        Assert.Equal(3, report.EnuVelocity.EastRate, precision: 6);
        Assert.Equal(-2, report.EnuVelocity.NorthRate, precision: 6);
    }
}
