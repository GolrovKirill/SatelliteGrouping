namespace SatelliteGrouping.Models
{
    public class SatelliteLink
    {
        public int SatelliteA { get; set; }
        public int SatelliteB { get; set; }
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int Duration => EndTime - StartTime;

        public override string ToString() => $"[{StartTime}, {EndTime}]";
    }
}