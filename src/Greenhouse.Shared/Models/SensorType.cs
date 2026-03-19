namespace Greenhouse.Shared.Models;

/// <summary>
/// Enumeración de tipos de sensores disponibles en el invernadero
/// </summary>
public enum SensorType
{
    /// <summary>
    /// Sensor de temperatura (celsius)
    /// </summary>
    Temperature,
    
    /// <summary>
    /// Sensor de humedad relativa (porcentaje)
    /// </summary>
    Humidity,
    
    /// <summary>
    /// Sensor de dióxido de carbono (ppm)
    /// </summary>
    CO2,
    
    /// <summary>
    /// Sensor de intensidad lumínica (porcentaje)
    /// </summary>
    Light
}
