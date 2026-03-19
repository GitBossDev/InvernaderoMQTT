
public class SensorData
{
    public SensorData()
    {

    }
    public required string SensorID { get; set; }
    public required string SensorType { get; set; }
    public int Value { get; set; }
    public required string Unit { get; set; }
    public required string Timestamp { get; set; } // ISO 8601
}