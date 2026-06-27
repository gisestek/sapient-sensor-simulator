using Google.Protobuf.WellKnownTypes;
using SapientMsg.BsiFlex335V20;
using RegistrationTypes = SapientMsg.BsiFlex335V20.Registration.Types;

namespace SapientSensorSimulator;

/// <summary>
/// Builds the two SAPIENT (BSI Flex 335 v2.0) messages this simulator sends: a Registration
/// (once, on connect) and DetectionReports (one per simulated target, per tick).
/// </summary>
public static class MessageFactory
{
    public static SapientMessage BuildDetectionReport(
        string nodeId, string objectId, double lat, double lon, double alt, double eastRate, double northRate, double upRate = 0)
    {
        return new SapientMessage
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            NodeId = nodeId,
            DetectionReport = new DetectionReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ObjectId = objectId,
                DetectionConfidence = 1.0f,
                Location = new Location
                {
                    X = lon,
                    Y = lat,
                    Z = alt,
                    CoordinateSystem = LocationCoordinateSystem.LatLngDegM,
                    Datum = LocationDatum.Wgs84E
                },
                EnuVelocity = new ENUVelocity
                {
                    EastRate = eastRate,
                    NorthRate = northRate,
                    UpRate = upRate
                }
            }
        };
    }

    public static SapientMessage BuildRegistration(string nodeId, string nodeName)
    {
        // Minimal but schema-valid Registration: fills every field BSI Flex 335 v2.0 marks
        // is_mandatory in registration.proto, with simple placeholder values — there is no
        // real ASM behind this, so capability/mode details are illustrative only.
        var worldArea = new RegistrationTypes.LocationType
        {
            LocationUnits = LocationCoordinateSystem.LatLngDegM,
            LocationDatum = LocationDatum.Wgs84E
        };

        return new SapientMessage
        {
            Timestamp = Timestamp.FromDateTime(DateTime.UtcNow),
            NodeId = nodeId,
            Registration = new Registration
            {
                IcdVersion = "BSI Flex 335 v2.0",
                Name = nodeName,
                ShortName = "SIM",
                NodeDefinition =
                {
                    new RegistrationTypes.NodeDefinition { NodeType = RegistrationTypes.NodeType.Other }
                },
                Capabilities =
                {
                    new RegistrationTypes.Capability { Category = "Simulation", Type = "SimulatedTarget" }
                },
                StatusDefinition = new RegistrationTypes.StatusDefinition
                {
                    StatusInterval = new RegistrationTypes.Duration
                    {
                        Units = RegistrationTypes.TimeUnits.Seconds,
                        Value = 5
                    }
                },
                ModeDefinition =
                {
                    new RegistrationTypes.ModeDefinition
                    {
                        ModeName = "Default",
                        ModeType = RegistrationTypes.ModeType.Default,
                        SettleTime = new RegistrationTypes.Duration { Units = RegistrationTypes.TimeUnits.Seconds, Value = 0 },
                        Task = new RegistrationTypes.TaskDefinition
                        {
                            RegionDefinition = new RegistrationTypes.RegionDefinition
                            {
                                RegionType = { RegistrationTypes.RegionType.AreaOfInterest },
                                RegionArea = { worldArea }
                            }
                        }
                    }
                },
                ConfigData =
                {
                    new RegistrationTypes.ConfigurationData
                    {
                        Manufacturer = "SapientSensorSimulator",
                        Model = "v1"
                    }
                }
            }
        };
    }
}
