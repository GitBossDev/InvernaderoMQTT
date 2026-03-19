using Greenhouse.Shared.Configuration;
using Greenhouse.Shared.Helpers;
using MQTTnet.Protocol;

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

    // PRUEBA 1: QoS 0 - At Most Once (Máximo una vez)
    // El mensaje se envía sin confirmación, puede perderse
    // Es el más rápido pero menos confiable
    // Útil para: datos no críticos, actualizaciones frecuentes (temperatura cada segundo)
    Console.WriteLine("\n--- Prueba 1: QoS 0 (At Most Once) ---");
    Console.WriteLine("Sin confirmación del broker, el mensaje puede perderse");
    Console.WriteLine("Ideal para: datos no críticos que se actualizan frecuentemente\n");
    
    await mqttHelper.PublishAsync(
        topic: "greenhouse/test/qos0",
        payload: $"Mensaje QoS 0 - Timestamp: {DateTime.Now:HH:mm:ss.fff}",
        qos: MqttQualityOfServiceLevel.AtMostOnce
    );
    
    await Task.Delay(1000);  // Esperar 1 segundo entre pruebas

    // PRUEBA 2: QoS 1 - At Least Once (Al menos una vez)
    // El broker confirma que recibió el mensaje
    // Puede haber duplicados si la confirmación se pierde
    // Útil para: datos importantes que no deben perderse
    Console.WriteLine("\n--- Prueba 2: QoS 1 (At Least Once) ---");
    Console.WriteLine("El broker confirma recepción, puede haber duplicados");
    Console.WriteLine("Ideal para: datos importantes que requieren confirmación\n");
    
    await mqttHelper.PublishAsync(
        topic: "greenhouse/test/qos1",
        payload: $"Mensaje QoS 1 - Timestamp: {DateTime.Now:HH:mm:ss.fff}",
        qos: MqttQualityOfServiceLevel.AtLeastOnce
    );
    
    await Task.Delay(1000);

    // PRUEBA 3: QoS 2 - Exactly Once (Exactamente una vez)
    // Handshake de 4 pasos garantiza que el mensaje llegue una sola vez
    // Es el más lento pero más confiable
    // Útil para: comandos críticos (abrir válvula, activar alarma)
    Console.WriteLine("\n--- Prueba 3: QoS 2 (Exactly Once) ---");
    Console.WriteLine("Handshake completo, garantiza entrega única");
    Console.WriteLine("Ideal para: comandos críticos que no deben duplicarse\n");
    
    await mqttHelper.PublishAsync(
        topic: "greenhouse/test/qos2",
        payload: $"Mensaje QoS 2 - Timestamp: {DateTime.Now:HH:mm:ss.fff}",
        qos: MqttQualityOfServiceLevel.ExactlyOnce
    );
    
    await Task.Delay(1000);

    // PRUEBA 4: Retained Message (Mensaje retenido)
    // El broker guarda el último mensaje con retain=true
    // Nuevos suscriptores reciben inmediatamente este mensaje al conectarse
    // Útil para: estado actual de dispositivos, configuraciones
    Console.WriteLine("\n--- Prueba 4: Retained Message ---");
    Console.WriteLine("El broker guarda este mensaje");
    Console.WriteLine("Nuevos suscriptores lo recibirán inmediatamente\n");
    
    await mqttHelper.PublishAsync(
        topic: "greenhouse/test/status",
        payload: "Publisher activo y funcionando",
        qos: MqttQualityOfServiceLevel.AtLeastOnce,
        retain: true  // Este mensaje se retiene en el broker
    );

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
