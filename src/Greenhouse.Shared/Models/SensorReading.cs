using System.Text.Json.Serialization;

namespace Greenhouse.Shared.Models;

/// <summary>
/// Modelo estándar para lecturas de sensores
/// Representa el formato JSON estructurado para envío de datos de sensores
/// </summary>
public class SensorReading
{
    /// <summary>
    /// Identificador único del sensor físico
    /// Ejemplo: "temp-01", "humidity-01"
    /// </summary>
    [JsonPropertyName("sensorId")]
    public string SensorId { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de sensor (temperature, humidity, co2, light)
    /// </summary>
    [JsonPropertyName("sensorType")]
    public string SensorType { get; set; } = string.Empty;

    /// <summary>
    /// Valor de la lectura del sensor
    /// Puede ser temperatura en celsius, humedad en porcentaje, etc.
    /// </summary>
    [JsonPropertyName("value")]
    public double Value { get; set; }

    /// <summary>
    /// Unidad de medida del valor
    /// Ejemplos: "celsius", "percent", "ppm", "lux"
    /// </summary>
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp de cuando se tomó la lectura
    /// Formato ISO 8601: "2026-03-19T10:30:00Z"
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Constructor por defecto
    /// </summary>
    public SensorReading()
    {
    }

    /// <summary>
    /// Constructor con parámetros para facilitar creación
    /// </summary>
    public SensorReading(string sensorId, string sensorType, double value, string unit)
    {
        SensorId = sensorId;
        SensorType = sensorType;
        Value = value;
        Unit = unit;
        Timestamp = DateTime.UtcNow;
    }
}
