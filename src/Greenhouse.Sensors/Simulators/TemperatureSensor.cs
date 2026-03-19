namespace Greenhouse.Sensors.Simulators;

/// <summary>
/// Simulador de sensor de temperatura (modelo antiguo)
/// Este sensor representa un dispositivo legacy que solo envía valores numéricos en texto plano
/// El procesamiento y formateo a JSON debe hacerse en el controlador
/// 
/// Características:
/// - Rango: 15-35°C
/// - Rango ideal: 20-25°C
/// - Variación: ±1°C por lectura
/// - Intervalo de publicación: 30 segundos
/// - Formato: Texto plano (solo el número)
/// </summary>
public class TemperatureSensor : BaseSensor
{
    // Constantes de configuración
    private const double TEMP_MIN = 15.0;
    private const double TEMP_MAX = 35.0;
    private const double TEMP_IDEAL_MIN = 20.0;
    private const double TEMP_IDEAL_MAX = 25.0;
    private const double TEMP_VARIATION = 1.0;  // Variación de ±1 grado por actualización

    /// <summary>
    /// Intervalo de publicación en milisegundos (30 segundos)
    /// </summary>
    public int PublishIntervalMs => 30000;

    /// <summary>
    /// Constructor del sensor de temperatura
    /// </summary>
    /// <param name="sensorId">Identificador único del sensor (ej: "temp-01")</param>
    public TemperatureSensor(string sensorId) 
        : base(sensorId, 
               initialValue: 22.0,  // Valor inicial en rango ideal
               minValue: TEMP_MIN, 
               maxValue: TEMP_MAX)
    {
        Console.WriteLine($"[{SensorId}] Sensor de temperatura inicializado");
        Console.WriteLine($"[{SensorId}] Rango: {TEMP_MIN}°C - {TEMP_MAX}°C (Ideal: {TEMP_IDEAL_MIN}°C - {TEMP_IDEAL_MAX}°C)");
        Console.WriteLine($"[{SensorId}] Variación: ±{TEMP_VARIATION}°C por lectura");
        Console.WriteLine($"[{SensorId}] Intervalo: {PublishIntervalMs / 1000} segundos");
        Console.WriteLine($"[{SensorId}] Formato: Texto plano (sensor antiguo)");
    }

    /// <summary>
    /// Actualiza la lectura del sensor simulando variaciones naturales de temperatura
    /// La temperatura puede subir o bajar 1 grado en cada actualización
    /// </summary>
    public override void UpdateReading()
    {
        // Generar variación aleatoria: -1, 0, o +1 grado
        // 0: -1°C, 1: sin cambio, 2: +1°C
        int variation = Random.Next(0, 3) - 1;  // Resultado: -1, 0, o 1
        double change = variation * TEMP_VARIATION;

        double newValue = CurrentValue + change;

        // Asegurar que el valor esté dentro del rango permitido
        CurrentValue = ClampValue(newValue);

        // Información de debug para entender el comportamiento
        string status = GetTemperatureStatus();
        Console.WriteLine($"[{SensorId}] Temperatura actualizada: {CurrentValue:F1}°C (Cambio: {change:+0.0;-0.0;0}°C) [{status}]");
    }

    /// <summary>
    /// Genera el payload en formato texto plano (solo el número)
    /// Este es un sensor antiguo que no soporta JSON
    /// El controller deberá formatear este valor al estándar JSON
    /// </summary>
    /// <returns>String con el valor numérico en texto plano</returns>
    public override string GeneratePayload()
    {
        // Sensor antiguo: solo envía el valor como texto
        // Formato: "22.5" (un decimal de precisión)
        return CurrentValue.ToString("F1");
    }

    /// <summary>
    /// Obtiene el topic MQTT para publicar lecturas de temperatura
    /// Sigue la estructura: greenhouse/sensors/temperature/{sensorId}
    /// </summary>
    /// <returns>Topic MQTT completo</returns>
    public override string GetTopic()
    {
        return $"greenhouse/sensors/temperature/{SensorId}";
    }

    /// <summary>
    /// Determina el estado de la temperatura respecto al rango ideal
    /// </summary>
    /// <returns>String indicando el estado: OK, BAJA, o ALTA</returns>
    private string GetTemperatureStatus()
    {
        if (CurrentValue < TEMP_IDEAL_MIN)
            return "BAJA";
        else if (CurrentValue > TEMP_IDEAL_MAX)
            return "ALTA";
        else
            return "OK";
    }

    /// <summary>
    /// Verifica si la temperatura está en el rango ideal
    /// </summary>
    /// <returns>True si está en rango ideal, false en caso contrario</returns>
    public bool IsInIdealRange()
    {
        return CurrentValue >= TEMP_IDEAL_MIN && CurrentValue <= TEMP_IDEAL_MAX;
    }
}
