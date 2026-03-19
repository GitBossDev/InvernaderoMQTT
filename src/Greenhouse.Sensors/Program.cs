using Greenhouse.Shared.Configuration;
using Greenhouse.Shared.Helpers;
using MQTTnet.Protocol;
using System.Text.Json;

/*
 * GREENHOUSE SENSORS - PUBLISHER (FASE 1)
 * 
 * Este programa actúa como PUBLISHER (Publicador) en MQTT
 * Su trabajo es ENVIAR mensajes al broker en topics específicos
 * 
 * CONCEPTOS CLAVE:
 * - Publisher: Cliente que PUBLICA/ENVÍA mensajes
 * - Topic: "Dirección" o "canal" donde se publ ica el mensaje (ej: "test/hello")
 * - Payload: El contenido del mensaje (texto, JSON, binario, etc.)
 * - QoS: Calidad de Servicio - define la garantía de entrega del mensaje
 */

Console.WriteLine("==============================================");
Console.WriteLine("  GREENHOUSE MQTT - PUBLISHER (Fase 1)");
Console.WriteLine("==============================================\n");

// Configurar parámetros de conexión MQTT
var settings = new MqttSettings
{
    BrokerHost = "localhost",  // Mosquitto está corriendo en Docker en nuestra máquina
    BrokerPort = 1883,          // Puerto estándar MQTT
    ClientId = "greenhouse-publisher-01"  // ID único para este cliente
};

Console.WriteLine($"Configuración:");
Console.WriteLine($"  Broker: {settings.BrokerHost}:{settings.BrokerPort}");
Console.WriteLine($"  Client ID: {settings.ClientId}\n");

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
    Console.WriteLine("  FASE 1: PRUEBAS DE QoS");
    Console.WriteLine("==============================================\n");

    // CO2
    // El mensaje se envía sin confirmación, puede perderse
    // Es el más rápido pero menos confiable
    // Útil para: datos no críticos, actualizaciones frecuentes (temperatura cada segundo)
    Console.WriteLine("\n--- CO2 ---");
    Console.WriteLine("Sin confirmación del broker, el mensaje puede perderse");

    Random randomNumber = new Random();

    // Generar un número aleatorio decimal entre 300 y 1500 (lo ideal 400-800)
    int C02ppm = randomNumber.Next(300, 1500);

    //Generamos variable con los datos del sensor
    var dataC02 = new

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
        topic: "greenhouse/sensor/co2",
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
    var dataLight = new
    {
        sensorID = "Light-1",
        sensorType = "Light",
        value = LightPercentage,
        unit = "%",
        timestamp = DateTime.Now
    };

    // Convertimos el objeto a JSON
    string payloadJsonLuz = JsonSerializer.Serialize(dataLight);

    await mqttHelper.PublishAsync(
        topic: "greenhouse/sensor/light",
        payload: payloadJsonLuz,
        qos: MqttQualityOfServiceLevel.AtLeastOnce
    );

    await Task.Delay(5000);

    Console.WriteLine("\n==============================================");
    Console.WriteLine("  PUBLICACIÓN CONTINUA");
    Console.WriteLine("  Presiona Ctrl+C para detener");
    Console.WriteLine("==============================================\n");

    // Publicar mensajes continuamente cada 3 segundos
    // Esto simula un sensor enviando datos periódicamente
    int messageCount = 0;

    while (true)
    {
        messageCount++;

        var message = $"Mensaje #{messageCount} desde Publisher - {DateTime.Now:HH:mm:ss}";

        await mqttHelper.PublishAsync(
            topic: "greenhouse/test/heartbeat",
            payload: message,
            qos: MqttQualityOfServiceLevel.AtLeastOnce
        );

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Publicado: {message}");

        // Esperar 3 segundos antes del siguiente mensaje
        await Task.Delay(3000);
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
