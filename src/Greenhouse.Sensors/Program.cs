using Greenhouse.Shared.Configuration;
using Greenhouse.Shared.Helpers;
using Greenhouse.Sensors.Simulators;
using MQTTnet.Protocol;
using System.Text.Json;

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
            // Útil para: datos no críticos, actualizaciones frecuentes (temperatura cada segundo)
    Console.WriteLine("\n--- CO2 ---");
    Console.WriteLine("Sin confirmación del broker, el mensaje puede perderse");

    Random randomNumber = new Random();

    // Generar un número aleatorio decimal entre 300 y 1500 (lo ideal 400-800)
    int C02ppm = randomNumber.Next(300, 1500);

    //Generamos variable con los datos del sensor
    SensorData dataC02 = new SensorData()
    {
        sensorID = "CO2-1",
        sensorType = "CO2",
        value = C02ppm,
        unit = "ppm",
        timestamp = DateTime.UtcNow.ToString("o")

    };

    // Convertimos el objeto a JSON
    string payloadJsonCO2 = JsonSerializer.Serialize(dataC02);

    await mqttHelper.PublishAsync(
        topic: "greenhouse/sensors/co2/co2-01",
        payload: payloadJsonCO2,
        qos: MqttQualityOfServiceLevel.AtMostOnce
    );

    await Task.Delay(5000);  // Esperar 1 segundo entre pruebas

    // LUZ
    // El broker confirma que recibió el mensaje
    // Puede haber duplicados si la confirmación se pierde
    // Útil para: datos importantes que no deben perderse
    Console.WriteLine("\n--- LIGHT ---");
    Console.WriteLine("El broker confirma recepción, puede haber duplicados");

    int LightPercentage = randomNumber.Next(0, 100);

    //Generamos variable con los datos del sensor
    var dataLight = new SensorData
    {
        sensorID = "Light-1",
        sensorType = "Light",
        value = LightPercentage,
        unit = "%",
        timestamp = DateTime.UtcNow.ToString("o")
    };

    // Convertimos el objeto a JSON
    string payloadJsonLuz = JsonSerializer.Serialize(dataLight);

    await mqttHelper.PublishAsync(
        topic: "greenhouse/sensors/light/light-01",
        payload: payloadJsonLuz,
        qos: MqttQualityOfServiceLevel.AtLeastOnce
    );

    await Task.Delay(5000);

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
