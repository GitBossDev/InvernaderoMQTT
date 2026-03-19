# Resumen FASE 2 - Sensores de Temperatura y Humedad

## Implementación Completada

Esta fase implementa dos sensores simulados con características diferentes:

### 1. Sensor de Temperatura (Antiguo)

**Características:**
- Modelo: Sensor antiguo/legacy
- Formato: Texto plano (solo el valor numérico)
- Rango: 15-35°C
- Rango ideal: 20-25°C
- Variación: ±1°C por lectura (puede subir, bajar, o mantenerse)
- Intervalo: 30 segundos
- Topic: `greenhouse/sensors/temperature/temp-01`

**Comportamiento:**
- Cada 30 segundos, la temperatura puede cambiar en -1°C, 0°C, o +1°C aleatoriamente
- El valor se mantiene dentro del rango 15-35°C
- Ejemplo de payload: `22.5`

**Procesamiento en Controller:**
- El Controller recibe el texto plano
- Convierte el valor a formato JSON estructurado (SensorReading)
- Muestra estado: OK, BAJA, o ALTA según el rango ideal

### 2. Sensor de Humedad (Moderno)

**Características:**
- Modelo: Sensor moderno con procesamiento local
- Formato: JSON estructurado
- Rango: 30-90%
- Rango ideal: 60-80%
- Variación: Solo disminuye 2% por lectura (evaporación)
- Intervalo: 60 segundos (1 minuto)
- Topic: `greenhouse/sensors/humidity/humidity-01`

**Comportamiento:**
- Cada 60 segundos, la humedad disminuye 2% (evaporación natural)
- Cuando alcanza el valor mínimo (30%), se reinicia al máximo (90%)
  - Esto simula la activación del sistema de riego
- El valor siempre se mantiene dentro del rango 30-90%

**Formato del Payload:**
```json
{
  "sensorId": "humidity-01",
  "sensorType": "humidity",
  "value": 70.0,
  "unit": "percent",
  "timestamp": "2026-03-19T14:30:00Z"
}
```

**Procesamiento en Controller:**
- El Controller deserializa el JSON directamente
- Valida y muestra el estado: OK, BAJA, o ALTA según el rango ideal

---

## Estructura de Archivos Creados

### Greenhouse.Shared
```
src/Greenhouse.Shared/
├── Models/
│   ├── SensorType.cs          # Enum con tipos de sensores
│   ├── SensorReading.cs       # Modelo JSON estándar para lecturas
│   └── MqttMessage.cs         # (Existente de Fase 1)
├── Configuration/
│   └── MqttSettings.cs        # (Existente de Fase 1)
└── Helpers/
    └── MqttClientHelper.cs    # (Existente de Fase 1)
```

### Greenhouse.Sensors
```
src/Greenhouse.Sensors/
├── Simulators/
│   ├── BaseSensor.cs          # Clase base abstracta para sensores
│   ├── TemperatureSensor.cs   # Simulador de temperatura
│   └── HumiditySensor.cs      # Simulador de humedad
└── Program.cs                  # Publicador de sensores
```

### Greenhouse.Controller
```
src/Greenhouse.Controller/
└── Program.cs                  # Procesador de sensores con formateo
```

---

## Conceptos Técnicos Aplicados

### 1. Herencia y Abstracción
- `BaseSensor`: Clase base abstracta que define el contrato para todos los sensores
- `TemperatureSensor` y `HumiditySensor`: Implementaciones concretas

### 2. Polimorfismo
- Cada sensor implementa su propia lógica de `UpdateReading()` y `GeneratePayload()`
- El mismo método tiene diferentes comportamientos según el tipo de sensor

### 3. Serialización JSON
- `System.Text.Json` para serializar/deserializar
- Atributos `[JsonPropertyName]` para mapear propiedades

### 4. Manejo de Diferentes Formatos en MQTT
- **Sensor antiguo**: Texto plano → Requiere procesamiento en el receptor
- **Sensor moderno**: JSON → Procesamiento directo

Esta diferencia simula un escenario real donde tienes equipos legacy y modernos comunicándose.

---

## Cómo Probar

### 1. Asegurarse que Mosquitto está corriendo
```bash
docker-compose up -d
docker ps
```

### 2. Terminal 1 - Ejecutar Controller
```bash
dotnet run --project src/Greenhouse.Controller
```

Deberías ver:
```
==============================================
  GREENHOUSE MQTT - CONTROLLER (Fase 2)
==============================================

Configuración:
  Broker: localhost:1883
  Client ID: greenhouse-controller

Conectando al broker MQTT...
[MQTT] Conectado exitosamente al broker localhost:1883
[MQTT] Cliente ID: greenhouse-controller

==============================================
  SUSCRIPCIONES ACTIVAS
==============================================

[MQTT] Suscrito exitosamente a 'greenhouse/sensors/temperature/#'
[MQTT] Suscrito exitosamente a 'greenhouse/sensors/humidity/#'

==============================================
  PROCESANDO LECTURAS DE SENSORES
  Presiona Ctrl+C para detener
==============================================
```

### 3. Terminal 2 - Ejecutar Sensors
```bash
dotnet run --project src/Greenhouse.Sensors
```

Deberías ver:
```
==============================================
  GREENHOUSE MQTT - SENSORES (Fase 2)
==============================================

Configuración:
  Broker: localhost:1883
  Client ID: greenhouse-sensors-publisher

[temp-01] Sensor de temperatura inicializado
[temp-01] Rango: 15°C - 35°C (Ideal: 20°C - 25°C)
[temp-01] Variación: ±1°C por lectura
[temp-01] Intervalo: 30 segundos
[temp-01] Formato: Texto plano (sensor antiguo)

[humidity-01] Sensor de humedad inicializado
[humidity-01] Rango: 30% - 90% (Ideal: 60% - 80%)
[humidity-01] Variación: -2% por lectura (solo disminuye)
[humidity-01] Intervalo: 60 segundos
[humidity-01] Formato: JSON (sensor moderno con procesamiento)

Conectando al broker MQTT...
[MQTT] Conectado exitosamente al broker localhost:1883

==============================================
  PUBLICACIÓN ACTIVA DE SENSORES
  Presiona Ctrl+C para detener
==============================================

[temp-01] Temperatura actualizada: 22.0°C (Cambio: 0°C) [OK]
[PUBLICADO] greenhouse/sensors/temperature/temp-01 → 22.0
[humidity-01] Humedad actualizada: 70.0% (Cambio: -2%) [OK]
[PUBLICADO] greenhouse/sensors/humidity/humidity-01 → {"sensorId":"humidity-01","sensorType":"humidity","value":70.0,"unit":"percent","timestamp":"2026-03-19T14:30:00Z"}
```

### 4. En MQTT Explorer

**Conectar:**
- Host: `localhost`
- Port: `1883`

**Observar topics:**
```
greenhouse/
└── sensors/
    ├── temperature/
    │   └── temp-01        → "22.5" (texto plano)
    └── humidity/
        └── humidity-01    → JSON estructurado
```

### 5. Desde CLI (Mosquitto)

**Ver todos los mensajes:**
```bash
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "greenhouse/sensors/#" -v
```

**Ver solo temperatura:**
```bash
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "greenhouse/sensors/temperature/#" -v
```

**Ver solo humedad:**
```bash
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "greenhouse/sensors/humidity/#" -v
```

---

## Salida Esperada en Controller

### Cuando llega un mensaje de Temperatura:
```
┌── TEMPERATURA PROCESADA ──────────────────────
│ Timestamp:  14:30:15
│ Topic:      greenhouse/sensors/temperature/temp-01
│ QoS:        AtLeastOnce
│ Sensor ID:  temp-01
│ Raw Payload: 22.5 (texto plano - sensor antiguo)
│ Temperatura: 22.5°C [OK ✓]
│
│ JSON Formateado:
│   {
│     "sensorId": "temp-01",
│     "sensorType": "temperature",
│     "value": 22.5,
│     "unit": "celsius",
│     "timestamp": "2026-03-19T14:30:15.123Z"
│   }
└───────────────────────────────────────────────
```

### Cuando llega un mensaje de Humedad:
```
┌── HUMEDAD PROCESADA ──────────────────────────
│ Timestamp:  14:30:45
│ Topic:      greenhouse/sensors/humidity/humidity-01
│ QoS:        AtLeastOnce
│ Sensor ID:  humidity-01
│ Tipo:       humidity
│ Humedad:    68.0% [OK ✓]
│ Unidad:     percent
│ Timestamp Sensor: 14:30:45
│ Formato:    JSON (sensor moderno)
└───────────────────────────────────────────────
```

---

## Diferencias Clave entre Sensores

| Aspecto | Temperatura (Antiguo) | Humedad (Moderno) |
|---------|----------------------|-------------------|
| Formato | Texto plano | JSON estructurado |
| Procesamiento | En el Controller | En el Sensor |
| Variación | ±1°C (aleatorio) | -2% (solo disminuye) |
| Intervalo | 30 segundos | 60 segundos |
| Payload | `"22.5"` | `{"sensorId":...}` |
| Simulación realismo | Cambios graduales | Evaporación constante |

---

## Próximos Pasos

Tu colega implementará:
- **CO2 Sensor** (300-1500 ppm, ideal: 400-800 ppm)
- **Light Sensor** (0-100%, ideal: 70-90% día, 0% noche)

Después de eso, se combinarán ambas ramas para tener los 4 sensores funcionando juntos.

Una vez completados todos los sensores, la **FASE 3** implementará el controlador automático que reaccionará a estas lecturas activando actuadores.

---

## Comandos Útiles

```bash
# Compilar proyecto
dotnet build

# Ejecutar sensores
dotnet run --project src/Greenhouse.Sensors

# Ejecutar controller
dotnet run --project src/Greenhouse.Controller

# Ver logs de Mosquitto
docker logs -f greenhouse-mosquitto

# Reiniciar Mosquitto
docker-compose restart mosquitto

# Ver topics activos en CLI
docker exec -it greenhouse-mosquitto mosquitto_sub -h localhost -t "#" -v
```
