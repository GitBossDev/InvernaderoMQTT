using Greenhouse.Shared.Configuration;
using Greenhouse.Shared.Helpers;
using Greenhouse.Shared.Models;
using MQTTnet.Protocol;
using System.Text;
using System.Text.Json;

/*
 * GREENHOUSE CONTROLLER - PROCESADOR DE SENSORES (FASE 2)
 * 
 * Este programa se suscribe a los topics de sensores y procesa los datos
 * 
 * CAPACIDADES:
 * - Recibe datos de sensor antiguo de temperatura (texto plano) y los formatea a JSON
 * - Recibe datos de sensor moderno de humedad (JSON) y los procesa directamente
 * - Muestra información estructurada de cada lectura
 * 
 * TOPICS SUSCRITOS:
 * - greenhouse/sensors/temperature/# (sensores antiguos, texto plano)
 * - greenhouse/sensors/humidity/# (sensores modernos, JSON)
 */

Console.WriteLine("==============================================");
Console.WriteLine("  GREENHOUSE MQTT - CONTROLLER (Fase 2)");
Console.WriteLine("==============================================\n");

// Configurar parámetros de conexión MQTT
var settings = new MqttSettings
{
    BrokerHost = "localhost",
    BrokerPort = 1883,
    ClientId = "greenhouse-controller"
};

Console.WriteLine($"Configuración:");
Console.WriteLine($"  Broker: {settings.BrokerHost}:{settings.BrokerPort}");
Console.WriteLine($"  Client ID: {settings.ClientId}\n");

// Crear helper MQTT
var mqttHelper = new MqttClientHelper(settings);

try
{
    Console.WriteLine("Conectando al broker MQTT...");
    var client = await mqttHelper.ConnectAsync();

    if (!mqttHelper.IsConnected)
    {
        Console.WriteLine("\n[ERROR] No se pudo conectar al broker.");
        Console.WriteLine("Asegúrate de que Docker Desktop está corriendo");
        Console.WriteLine("y ejecuta: docker-compose up -d");
        return;
    }

    // Configurar el manejador de mensajes
    mqttHelper.SetMessageHandler(async e =>
    {
        var topic = e.ApplicationMessage.Topic;
        var payloadBytes = e.ApplicationMessage.PayloadSegment;
        var payloadRaw = Encoding.UTF8.GetString(payloadBytes);
        var qos = e.ApplicationMessage.QualityOfServiceLevel;
        var timestamp = DateTime.Now;

        try
        {
            // Determinar el tipo de sensor por el topic
            if (topic.Contains("/temperature/"))
            {
                // SENSOR ANTIGUO: Texto plano, necesita formateo
                ProcessTemperatureSensor(topic, payloadRaw, qos, timestamp);
            }
            else if (topic.Contains("/humidity/"))
            {
                // SENSOR MODERNO: JSON, procesamiento directo
                ProcessHumiditySensor(topic, payloadRaw, qos, timestamp);
            }
            else
            {
                // Otro tipo de mensaje
                Console.WriteLine($"\n[MENSAJE NO PROCESADO] Topic: {topic} | Payload: {payloadRaw}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] Error al procesar mensaje de {topic}: {ex.Message}");
        }

        await Task.CompletedTask;
    });

    Console.WriteLine("\n==============================================");
    Console.WriteLine("  SUSCRIPCIONES ACTIVAS");
    Console.WriteLine("==============================================\n");

    // Suscribirse a todos los sensores de temperatura
    await mqttHelper.SubscribeAsync(
        topic: "greenhouse/sensors/temperature/#",
        qos: MqttQualityOfServiceLevel.AtLeastOnce
    );

    // Suscribirse a todos los sensores de humedad
    await mqttHelper.SubscribeAsync(
        topic: "greenhouse/sensors/humidity/#",
        qos: MqttQualityOfServiceLevel.AtLeastOnce
    );

    Console.WriteLine("\n==============================================");
    Console.WriteLine("  PROCESANDO LECTURAS DE SENSORES");
    Console.WriteLine("  Presiona Ctrl+C para detener");
    Console.WriteLine("==============================================\n");

    // Mantener la aplicación corriendo
    while (true)
    {
        await Task.Delay(1000);
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n[ERROR] {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"[ERROR INTERNO] {ex.InnerException.Message}");
    }
}
finally
{
    Console.WriteLine("\n\nDesconectando del broker...");
    await mqttHelper.DisconnectAsync();
    Console.WriteLine("Aplicación finalizada.");
}

/// <summary>
/// Procesa mensajes de sensores de temperatura (formato texto plano)
/// Convierte el valor texto a JSON estructurado según el estándar SensorReading
/// </summary>
static void ProcessTemperatureSensor(string topic, string payloadRaw, MqttQualityOfServiceLevel qos, DateTime timestamp)
{
    // Extraer el sensor ID del topic
    // Topic: greenhouse/sensors/temperature/temp-01
    var parts = topic.Split('/');
    var sensorId = parts.Length > 3 ? parts[3] : "unknown";

    // El payload es texto plano: "22.5"
    // Intentar parsear a double
    if (!double.TryParse(payloadRaw, out double temperatureValue))
    {
        Console.WriteLine($"[ERROR] No se pudo parsear temperatura: {payloadRaw}");
        return;
    }

    // Crear objeto JSON estructurado
    var reading = new SensorReading
    {
        SensorId = sensorId,
        SensorType = "temperature",
        Value = temperatureValue,
        Unit = "celsius",
        Timestamp = DateTime.UtcNow
    };

    // Serializar a JSON para logging/almacenamiento
    var jsonReading = JsonSerializer.Serialize(reading, new JsonSerializerOptions { WriteIndented = true });

    // Determinar estado de la temperatura
    string status;
    if (temperatureValue < 20.0)
        status = "BAJA ❄️";
    else if (temperatureValue > 25.0)
        status = "ALTA 🔥";
    else
        status = "OK ✓";

    // Mostrar información procesada
    Console.WriteLine($"\n┌── TEMPERATURA PROCESADA ──────────────────────");
    Console.WriteLine($"│ Timestamp:  {timestamp:HH:mm:ss}");
    Console.WriteLine($"│ Topic:      {topic}");
    Console.WriteLine($"│ QoS:        {qos}");
    Console.WriteLine($"│ Sensor ID:  {sensorId}");
    Console.WriteLine($"│ Raw Payload: {payloadRaw} (texto plano - sensor antiguo)");
    Console.WriteLine($"│ Temperatura: {temperatureValue:F1}°C [{status}]");
    Console.WriteLine($"│");
    Console.WriteLine($"│ JSON Formateado:");
    foreach (var line in jsonReading.Split('\n'))
    {
        Console.WriteLine($"│   {line}");
    }
    Console.WriteLine($"└───────────────────────────────────────────────\n");
}

/// <summary>
/// Procesa mensajes de sensores de humedad (formato JSON)
/// Deserializa y muestra la información estructurada
/// </summary>
static void ProcessHumiditySensor(string topic, string payloadRaw, MqttQualityOfServiceLevel qos, DateTime timestamp)
{
    try
    {
        // El payload ya viene en JSON, deserializar directamente
        var reading = JsonSerializer.Deserialize<SensorReading>(payloadRaw);

        if (reading == null)
        {
            Console.WriteLine($"[ERROR] No se pudo deserializar JSON de humedad: {payloadRaw}");
            return;
        }

        // Determinar estado de la humedad
        string status;
        if (reading.Value < 60.0)
            status = "BAJA 🌵";
        else if (reading.Value > 80.0)
            status = "ALTA 💧";
        else
            status = "OK ✓";

        // Mostrar información procesada
        Console.WriteLine($"\n┌── HUMEDAD PROCESADA ──────────────────────────");
        Console.WriteLine($"│ Timestamp:  {timestamp:HH:mm:ss}");
        Console.WriteLine($"│ Topic:      {topic}");
        Console.WriteLine($"│ QoS:        {qos}");
        Console.WriteLine($"│ Sensor ID:  {reading.SensorId}");
        Console.WriteLine($"│ Tipo:       {reading.SensorType}");
        Console.WriteLine($"│ Humedad:    {reading.Value:F1}% [{status}]");
        Console.WriteLine($"│ Unidad:     {reading.Unit}");
        Console.WriteLine($"│ Timestamp Sensor: {reading.Timestamp:HH:mm:ss}");
        Console.WriteLine($"│ Formato:    JSON (sensor moderno)");
        Console.WriteLine($"└───────────────────────────────────────────────\n");
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"[ERROR] Error al parsear JSON de humedad: {ex.Message}");
        Console.WriteLine($"Payload recibido: {payloadRaw}");
    }
}
