
public class SensorData
{
    public SensorData()
    {

    }
    public required string sensorID { get; set; }
    public required string sensorType { get; set; }
    public int value { get; set; }
    public required string unit { get; set; }
    public required string timestamp { get; set; } // ISO 8601
}