# Guía para Implementar Sensores de CO2 y Luz

Esta guía es para tu colega que implementará los sensores de CO2 y Luz en una rama separada.

## Contexto

Ya están implementados:
- **TemperatureSensor**: Sensor antiguo (texto plano), varía ±1°C cada 30s
- **HumiditySensor**: Sensor moderno (JSON), disminuye 2% cada 60s

Tu colega debe implementar siguiendo la misma estructura:
- **CO2Sensor**: Especificaciones abajo
- **LightSensor**: Especificaciones abajo

---

## Sensor de CO2

### Especificaciones
- **Rango**: 300-1500 ppm
- **Rango ideal**: 400-800 ppm
- **Comportamiento**: Este sensor monitoreará niveles de dióxido de carbono
- **Variación sugerida**: 
  - Puede aumentar lentamente (respiración de plantas)
  - Puede disminuir cuando se activa ventilación (en Fase 3)
  - Sugerencia: ±50 ppm por lectura
- **Intervalo**: 45 segundos (diferente a los otros para evitar publicaciones simultáneas)
- **Formato**: A elección (puede ser JSON moderno o texto plano antiguo)
- **Topic**: `greenhouse/sensors/co2/co2-01`

### Estructura del archivo

**Ubicación**: `src/Greenhouse.Sensors/Simulators/CO2Sensor.cs`

**Plantilla**:
```csharp
namespace Greenhouse.Sensors.Simulators;

public class CO2Sensor : BaseSensor
{
    private const double CO2_MIN = 300.0;
    private const double CO2_MAX = 1500.0;
    private const double CO2_IDEAL_MIN = 400.0;
    private const double CO2_IDEAL_MAX = 800.0;
    private const double CO2_VARIATION = 50.0;  // ±50 ppm

    public int PublishIntervalMs => 45000;  // 45 segundos

    public CO2Sensor(string sensorId) 
        : base(sensorId, 
               initialValue: 600.0,  // Valor inicial en rango ideal
               minValue: CO2_MIN, 
               maxValue: CO2_MAX)
    {
        // Mensajes de inicialización aquí
    }

    public override void UpdateReading()
    {
        // Implementar lógica de variación
        // Puede usar Random para simular aumentos/disminuciones
    }

    public override string GeneratePayload()
    {
        // Decidir si envía texto plano o JSON
        // Si JSON, usar SensorReading como en HumiditySensor
    }

    public override string GetTopic()
    {
        return $"greenhouse/sensors/co2/{SensorId}";
    }
}
```

---

## Sensor de Luz

### Especificaciones
- **Rango**: 0-100%
- **Rango ideal**: 
  - **Día** (6:00 - 20:00): 70-90%
  - **Noche** (20:00 - 6:00): 0%
- **Comportamiento**: 
  - Simular ciclo día/noche
  - Durante el día, variar entre 70-90%
  - Durante la noche, debe ser 0%
  - Transiciones graduales al amanecer y atardecer
- **Variación sugerida**: 
  - Puede usar `DateTime.Now.Hour` para determinar si es día o noche
  - Variación: ±5% durante el día
- **Intervalo**: 20 segundos (más frecuente para capturar cambios de luz)
- **Formato**: A elección (puede ser JSON moderno o texto plano antiguo)
- **Topic**: `greenhouse/sensors/light/light-01`

### Estructura del archivo

**Ubicación**: `src/Greenhouse.Sensors/Simulators/LightSensor.cs`

**Plantilla**:
```csharp
namespace Greenhouse.Sensors.Simulators;

public class LightSensor : BaseSensor
{
    private const double LIGHT_MIN = 0.0;
    private const double LIGHT_MAX = 100.0;
    private const double LIGHT_IDEAL_DAY_MIN = 70.0;
    private const double LIGHT_IDEAL_DAY_MAX = 90.0;
    private const double LIGHT_VARIATION = 5.0;  // ±5%

    public int PublishIntervalMs => 20000;  // 20 segundos

    public LightSensor(string sensorId) 
        : base(sensorId, 
               initialValue: 80.0,  // Valor inicial día
               minValue: LIGHT_MIN, 
               maxValue: LIGHT_MAX)
    {
        // Mensajes de inicialización aquí
    }

    public override void UpdateReading()
    {
        // Determinar si es de día (6:00-20:00) o noche
        var currentHour = DateTime.Now.Hour;
        bool isDaytime = currentHour >= 6 && currentHour < 20;

        if (isDaytime)
        {
            // Variar entre 70-90%
            // Puedes usar Random para variaciones
        }
        else
        {
            // Noche: 0%
            CurrentValue = 0.0;
        }
    }

    public override string GeneratePayload()
    {
        // Decidir si envía texto plano o JSON
    }

    public override string GetTopic()
    {
        return $"greenhouse/sensors/light/{SensorId}";
    }

    private bool IsDaytime()
    {
        var currentHour = DateTime.Now.Hour;
        return currentHour >= 6 && currentHour < 20;
    }
}
```

---

## Actualizar Program.cs

En `src/Greenhouse.Sensors/Program.cs`, agregar las instancias de los nuevos sensores:

```csharp
// Crear instancias de los sensores
var temperatureSensor = new TemperatureSensor("temp-01");
var humiditySensor = new HumiditySensor("humidity-01");
var co2Sensor = new CO2Sensor("co2-01");           // NUEVO
var lightSensor = new LightSensor("light-01");      // NUEVO
```

Y en el loop de publicación, agregar la lógica para CO2 y Light:

```csharp
var lastTempPublish = DateTime.MinValue;
var lastHumidityPublish = DateTime.MinValue;
var lastCO2Publish = DateTime.MinValue;      // NUEVO
var lastLightPublish = DateTime.MinValue;    // NUEVO

while (true)
{
    var now = DateTime.UtcNow;

    // ... código existente para temperatura y humedad ...

    // Publicar CO2 cada 45 segundos
    if ((now - lastCO2Publish).TotalMilliseconds >= co2Sensor.PublishIntervalMs)
    {
        co2Sensor.UpdateReading();
        var payload = co2Sensor.GeneratePayload();
        var topic = co2Sensor.GetTopic();
        
        await mqttHelper.PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce);
        Console.WriteLine($"[PUBLICADO] {topic} → {payload}");
        lastCO2Publish = now;
    }

    // Publicar LUZ cada 20 segundos
    if ((now - lastLightPublish).TotalMilliseconds >= lightSensor.PublishIntervalMs)
    {
        lightSensor.UpdateReading();
        var payload = lightSensor.GeneratePayload();
        var topic = lightSensor.GetTopic();
        
        await mqttHelper.PublishAsync(topic, payload, MqttQualityOfServiceLevel.AtLeastOnce);
        Console.WriteLine($"[PUBLICADO] {topic} → {payload}");
        lastLightPublish = now;
    }

    await Task.Delay(1000);
}
```

---

## Actualizar Controller

En `src/Greenhouse.Controller/Program.cs`, agregar suscripciones y procesadores:

### Agregar suscripciones:
```csharp
// Sensores existentes
await mqttHelper.SubscribeAsync("greenhouse/sensors/temperature/#", MqttQualityOfServiceLevel.AtLeastOnce);
await mqttHelper.SubscribeAsync("greenhouse/sensors/humidity/#", MqttQualityOfServiceLevel.AtLeastOnce);

// NUEVOS sensores
await mqttHelper.SubscribeAsync("greenhouse/sensors/co2/#", MqttQualityOfServiceLevel.AtLeastOnce);
await mqttHelper.SubscribeAsync("greenhouse/sensors/light/#", MqttQualityOfServiceLevel.AtLeastOnce);
```

### Agregar procesadores en el handler:
```csharp
if (topic.Contains("/temperature/"))
{
    ProcessTemperatureSensor(topic, payloadRaw, qos, timestamp);
}
else if (topic.Contains("/humidity/"))
{
    ProcessHumiditySensor(topic, payloadRaw, qos, timestamp);
}
else if (topic.Contains("/co2/"))
{
    ProcessCO2Sensor(topic, payloadRaw, qos, timestamp);  // NUEVO
}
else if (topic.Contains("/light/"))
{
    ProcessLightSensor(topic, payloadRaw, qos, timestamp);  // NUEVO
}
```

### Implementar las funciones de procesamiento:

```csharp
static void ProcessCO2Sensor(string topic, string payloadRaw, MqttQualityOfServiceLevel qos, DateTime timestamp)
{
    // Similar a ProcessTemperatureSensor o ProcessHumiditySensor
    // Dependiendo del formato que elijas (texto plano o JSON)
    
    // Si usas JSON, deserializar:
    // var reading = JsonSerializer.Deserialize<SensorReading>(payloadRaw);
    
    // Si usas texto plano, parsear:
    // double.TryParse(payloadRaw, out double co2Value);
    
    // Determinar estado (OK si está entre 400-800 ppm)
    // Mostrar con formato similar
}

static void ProcessLightSensor(string topic, string payloadRaw, MqttQualityOfServiceLevel qos, DateTime timestamp)
{
    // Similar implementación
    // Determinar si es día o noche
    // Estado OK si es 70-90% durante el día, o 0% durante la noche
}
```

---

## Resumen de Topics

Después de la implementación completa, los topics serán:

```
greenhouse/sensors/
├── temperature/
│   └── temp-01          (30s, texto plano, ±1°C)
├── humidity/
│   └── humidity-01      (60s, JSON, -2%)
├── co2/
│   └── co2-01          (45s, formato a elección, ±50 ppm)
└── light/
    └── light-01         (20s, formato a elección, ciclo día/noche)
```

---

## Verificar Funcionamiento

### Prueba en MQTT Explorer:
1. Conectar a `localhost:1883`
2. Ver los 4 topics publicando datos
3. Verificar que cada uno publica en su intervalo correcto

### Prueba en CLI:
```bash
# Ver todos los sensores
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "greenhouse/sensors/#" -v

# Ver solo CO2
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "greenhouse/sensors/co2/#" -v

# Ver solo luz
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "greenhouse/sensors/light/#" -v
```

---

## Merge de Ramas

Una vez que ambas ramas estén completas:
1. Tu rama: Temperatura + Humedad
2. Rama del colega: CO2 + Luz

Merge de ambas para tener los 4 sensores funcionando juntos.

Deberían ver en controller:
```
[TEMPERATURA] 22.5°C [OK]
[HUMEDAD] 68.0% [OK]
[CO2] 650 ppm [OK]
[LUZ] 85% [OK] (Día)
```

---

## Contacto

Si tu colega tiene dudas, puede revisar:
- [docs/FASE2-RESUMEN.md](FASE2-RESUMEN.md) - Resumen completo de la Fase 2
- [src/Greenhouse.Sensors/Simulators/TemperatureSensor.cs](../src/Greenhouse.Sensors/Simulators/TemperatureSensor.cs) - Ejemplo de sensor simple
- [src/Greenhouse.Sensors/Simulators/HumiditySensor.cs](../src/Greenhouse.Sensors/Simulators/HumiditySensor.cs) - Ejemplo de sensor moderno con JSON

¡Éxito con la implementación!
