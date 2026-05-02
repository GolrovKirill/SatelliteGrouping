using System.Collections.Generic;

namespace SatelliteGrouping.Models
{
    public class TimeSegment
    {
        public int Start { get; set; }
        public int End { get; set; }
        public List<HashSet<int>> Groups { get; set; } = new List<HashSet<int>>();
        public List<SatelliteLink> ActiveLinks { get; set; } = new List<SatelliteLink>();
    }
}