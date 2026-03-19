namespace Greenhouse.Sensors.Simulators;

/// <summary>
/// Clase base abstracta para todos los simuladores de sensores
/// Define la estructura común que todos los sensores deben implementar
/// </summary>
public abstract class BaseSensor
{
    /// <summary>
    /// Identificador único del sensor
    /// </summary>
    public string SensorId { get; protected set; }

    /// <summary>
    /// Valor actual del sensor
    /// </summary>
    protected double CurrentValue { get; set; }

    /// <summary>
    /// Valor mínimo permitido para este sensor
    /// </summary>
    protected double MinValue { get; set; }

    /// <summary>
    /// Valor máximo permitido para este sensor
    /// </summary>
    protected double MaxValue { get; set; }

    /// <summary>
    /// Generador de números aleatorios para simular variaciones
    /// </summary>
    protected Random Random { get; }

    /// <summary>
    /// Constructor base para inicializar propiedades comunes
    /// </summary>
    /// <param name="sensorId">ID único del sensor</param>
    /// <param name="initialValue">Valor inicial de la lectura</param>
    /// <param name="minValue">Valor mínimo del rango</param>
    /// <param name="maxValue">Valor máximo del rango</param>
    protected BaseSensor(string sensorId, double initialValue, double minValue, double maxValue)
    {
        SensorId = sensorId;
        CurrentValue = initialValue;
        MinValue = minValue;
        MaxValue = maxValue;
        Random = new Random();
    }

    /// <summary>
    /// Actualiza la lectura del sensor con un nuevo valor simulado
    /// Cada sensor implementa su propia lógica de variación
    /// </summary>
    public abstract void UpdateReading();

    /// <summary>
    /// Obtiene el valor actual del sensor
    /// </summary>
    /// <returns>Valor actual de la lectura</returns>
    public virtual double GetCurrentValue()
    {
        return CurrentValue;
    }

    /// <summary>
    /// Genera el payload (contenido del mensaje) para publicar en MQTT
    /// Cada sensor puede tener su propio formato (texto plano, JSON, etc.)
    /// </summary>
    /// <returns>String con el payload a publicar</returns>
    public abstract string GeneratePayload();

    /// <summary>
    /// Obtiene el topic MQTT específico para este sensor
    /// Sigue el formato: greenhouse/sensors/{tipo}/{id}
    /// </summary>
    /// <returns>Topic MQTT completo</returns>
    public abstract string GetTopic();

    /// <summary>
    /// Restringe un valor al rango permitido [MinValue, MaxValue]
    /// </summary>
    /// <param name="value">Valor a restringir</param>
    /// <returns>Valor dentro del rango permitido</returns>
    protected double ClampValue(double value)
    {
        if (value < MinValue) return MinValue;
        if (value > MaxValue) return MaxValue;
        return value;
    }
}
