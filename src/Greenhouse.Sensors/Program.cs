using Greenhouse.Shared.Configuration;
using Greenhouse.Shared.Helpers;
using Greenhouse.Sensors.Simulators;
using MQTTnet.Protocol;

/*
 * GREENHOUSE SENSORS - SIMULADORES (FASE 2)
 * 
 * Este programa simula sensores del invernadero publicando datos a MQTT
 * 
 * SENSORES IMPLEMENTADOS:
 * - Temperatura: Sensor antiguo que envía texto plano, varía ±1°C cada 30s
 * - Humedad: Sensor moderno que envía JSON, disminuye 2% cada 60s
 * 
 * Cada sensor publica en su propio topic:
 * - greenhouse/sensors/temperature/temp-01
 * - greenhouse/sensors/humidity/humidity-01
 */

Console.WriteLine("==============================================");
Console.WriteLine("  GREENHOUSE MQTT - SENSORES (Fase 2)");
Console.WriteLine("==============================================\n");

// Configurar parámetros de conexión MQTT
var settings = new MqttSettings
{
    BrokerHost = "localhost",
    BrokerPort = 1883,
    ClientId = "greenhouse-sensors-publisher"
};

Console.WriteLine($"Configuración:");
Console.WriteLine($"  Broker: {settings.BrokerHost}:{settings.BrokerPort}");
Console.WriteLine($"  Client ID: {settings.ClientId}\n");

// Crear instancias de los sensores
var temperatureSensor = new TemperatureSensor("temp-01");
var humiditySensor = new HumiditySensor("humidity-01");

Console.WriteLine();

// Crear helper MQTT y conectar al broker
var mqttHelper = new MqttClientHelper(settings);

try
{
    Console.WriteLine("Conectando al broker MQTT...");
    await mqttHelper.ConnectAsync();

    if (!mqttHelper.IsConnected)
    {
        Console.WriteLine("\n[ERROR] No se pudo conectar al broker.");
        Console.WriteLine("Asegúrate de que Docker Desktop está corriendo");
        Console.WriteLine("y ejecuta: docker-compose up -d");
        return;
    }

    Console.WriteLine("\n==============================================");
    Console.WriteLine("  PUBLICACIÓN ACTIVA DE SENSORES");
    Console.WriteLine("  Presiona Ctrl+C para detener");
    Console.WriteLine("==============================================\n");

    // Variables para controlar los timers de cada sensor
    var lastTempPublish = DateTime.MinValue;
    var lastHumidityPublish = DateTime.MinValue;

    // Loop principal de publicación
    while (true)
    {
        var now = DateTime.UtcNow;

        // Publicar TEMPERATURA cada 30 segundos
        if ((now - lastTempPublish).TotalMilliseconds >= temperatureSensor.PublishIntervalMs)
        {
            // Actualizar lectura del sensor
            temperatureSensor.UpdateReading();

            // Generar payload y publicar
            var tempPayload = temperatureSensor.GeneratePayload();
            var tempTopic = temperatureSensor.GetTopic();

            await mqttHelper.PublishAsync(
                topic: tempTopic,
                payload: tempPayload,
                qos: MqttQualityOfServiceLevel.AtLeastOnce
            );

            Console.WriteLine($"[PUBLICADO] {tempTopic} → {tempPayload}");
            lastTempPublish = now;
        }

        // Publicar HUMEDAD cada 60 segundos
        if ((now - lastHumidityPublish).TotalMilliseconds >= humiditySensor.PublishIntervalMs)
        {
            // Actualizar lectura del sensor
            humiditySensor.UpdateReading();

            // Generar payload y publicar
            var humidityPayload = humiditySensor.GeneratePayload();
            var humidityTopic = humiditySensor.GetTopic();

            await mqttHelper.PublishAsync(
                topic: humidityTopic,
                payload: humidityPayload,
                qos: MqttQualityOfServiceLevel.AtLeastOnce
            );

            Console.WriteLine($"[PUBLICADO] {humidityTopic} → {humidityPayload}");
            lastHumidityPublish = now;
        }

        // Esperar 1 segundo antes de verificar nuevamente
        // Esto evita consumir CPU innecesariamente
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
    // Importante: Siempre desconectar limpiamente antes de salir
    Console.WriteLine("\n\nDesconectando del broker...");
    await mqttHelper.DisconnectAsync();
    Console.WriteLine("Aplicación finalizada.");
}
