namespace SapientSensorSimulator;

public enum NoiseKind
{
    /// <summary>No noise — the field is sent exactly as the trajectory model computes it.</summary>
    None,

    /// <summary>Normally-distributed random error. Magnitude is the standard deviation.</summary>
    Gaussian,

    /// <summary>Uniformly-distributed random error in [-Magnitude, +Magnitude].</summary>
    Uniform,

    /// <summary>"Säännönmukaista": a deterministic, repeating bias (a slow sine wave driven by
    /// wall-clock time) rather than randomness — e.g. a sensor with a consistent calibration
    /// drift instead of pure noise. Magnitude is the bias amplitude.</summary>
    Systematic
}
