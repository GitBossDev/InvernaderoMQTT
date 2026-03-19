using System.Text.Json;
using Greenhouse.Shared.Models;

namespace Greenhouse.Sensors.Simulators;

/// <summary>
/// Simulador de sensor de humedad (modelo moderno)
/// Este sensor representa un dispositivo moderno que incluye procesamiento local
/// y envía datos en formato JSON estructurado
/// 
/// Características:
/// - Rango: 30-90%
/// - Rango ideal: 60-80%
/// - Variación: Solo disminuye en 2% por lectura (evaporación)
/// - Intervalo de publicación: 60 segundos
/// - Formato: JSON estructurado (sensor moderno con procesamiento)
/// </summary>
public class HumiditySensor : BaseSensor
{
    // Constantes de configuración
    private const double HUMIDITY_MIN = 30.0;
    private const double HUMIDITY_MAX = 90.0;
    private const double HUMIDITY_IDEAL_MIN = 60.0;
    private const double HUMIDITY_IDEAL_MAX = 80.0;
    private const double HUMIDITY_DECREASE = 2.0;  // La humedad solo disminuye (evaporación)

    /// <summary>
    /// Intervalo de publicación en milisegundos (60 segundos)
    /// </summary>
    public int PublishIntervalMs => 60000;

    /// <summary>
    /// Constructor del sensor de humedad
    /// </summary>
    /// <param name="sensorId">Identificador único del sensor (ej: "humidity-01")</param>
    public HumiditySensor(string sensorId)
        : base(sensorId,
               initialValue: 70.0,  // Valor inicial en rango ideal
               minValue: HUMIDITY_MIN,
               maxValue: HUMIDITY_MAX)
    {
        Console.WriteLine($"[{SensorId}] Sensor de humedad inicializado");
        Console.WriteLine($"[{SensorId}] Rango: {HUMIDITY_MIN}% - {HUMIDITY_MAX}% (Ideal: {HUMIDITY_IDEAL_MIN}% - {HUMIDITY_IDEAL_MAX}%)");
        Console.WriteLine($"[{SensorId}] Variación: -{HUMIDITY_DECREASE}% por lectura (solo disminuye)");
        Console.WriteLine($"[{SensorId}] Intervalo: {PublishIntervalMs / 1000} segundos");
        Console.WriteLine($"[{SensorId}] Formato: JSON (sensor moderno con procesamiento)");
    }

    /// <summary>
    /// Actualiza la lectura del sensor simulando evaporación natural
    /// La humedad SOLO DISMINUYE en 2% por cada lectura (evaporación natural del invernadero)
    /// Cuando alcanza el mínimo, se reinicia al máximo (simula riego/humidificación externa)
    /// </summary>
    public override void UpdateReading()
    {
        // La humedad disminuye constantemente por evaporación
        double newValue = CurrentValue - HUMIDITY_DECREASE;

        // Si llega al mínimo, simular que se activó el sistema de riego (reinicia al máximo)
        if (newValue < MinValue)
        {
            CurrentValue = MaxValue;
            Console.WriteLine($"[{SensorId}] Humedad crítica alcanzada. Sistema de riego activado. Humedad: {CurrentValue:F1}%");
        }
        else
        {
            CurrentValue = newValue;
            string status = GetHumidityStatus();
            Console.WriteLine($"[{SensorId}] Humedad actualizada: {CurrentValue:F1}% (Cambio: -{HUMIDITY_DECREASE}%) [{status}]");
        }
    }

    /// <summary>
    /// Genera el payload en formato JSON estructurado
    /// Este sensor moderno tiene capacidad de procesamiento local
    /// y envía datos en el formato estándar SensorReading
    /// </summary>
    /// <returns>String con JSON serializado</returns>
    public override string GeneratePayload()
    {
        // Sensor moderno: envía JSON estructurado directamente
        var reading = new SensorReading
        {
            SensorId = SensorId,
            SensorType = "humidity",
            Value = Math.Round(CurrentValue, 1),  // Un decimal de precisión
            Unit = "percent",
            Timestamp = DateTime.UtcNow
        };

        // Serializar a JSON con formato legible
        var options = new JsonSerializerOptions
        {
            WriteIndented = false  // JSON compacto para MQTT (menos bytes)
        };

        return JsonSerializer.Serialize(reading, options);
    }

    /// <summary>
    /// Obtiene el topic MQTT para publicar lecturas de humedad
    /// Sigue la estructura: greenhouse/sensors/humidity/{sensorId}
    /// </summary>
    /// <returns>Topic MQTT completo</returns>
    public override string GetTopic()
    {
        return $"greenhouse/sensors/humidity/{SensorId}";
    }

    /// <summary>
    /// Determina el estado de la humedad respecto al rango ideal
    /// </summary>
    /// <returns>String indicando el estado: OK, BAJA, o ALTA</returns>
    private string GetHumidityStatus()
    {
        if (CurrentValue < HUMIDITY_IDEAL_MIN)
            return "BAJA";
        else if (CurrentValue > HUMIDITY_IDEAL_MAX)
            return "ALTA";
        else
            return "OK";
    }

    /// <summary>
    /// Verifica si la humedad está en el rango ideal
    /// </summary>
    /// <returns>True si está en rango ideal, false en caso contrario</returns>
    public bool IsInIdealRange()
    {
        return CurrentValue >= HUMIDITY_IDEAL_MIN && CurrentValue <= HUMIDITY_IDEAL_MAX;
    }

    /// <summary>
    /// Verifica si la humedad está en nivel crítico (requiere riego inmediato)
    /// </summary>
    /// <returns>True si la humedad es menor a 40%, false en caso contrario</returns>
    public bool IsCriticalLow()
    {
        return CurrentValue < 40.0;
    }
}
