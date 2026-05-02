using System;
using System.Collections.Generic;
using System.Linq;
using SatelliteGrouping.Models;

namespace SatelliteGrouping.Services
{
    public static class Grouper
    {
        public static List<TimeSlot> GenerateTimeSlots(int n, int T_max, List<SatelliteLink> links)
        {
            const int slotDuration = 10;
            var slots = new List<TimeSlot>();

            for (int t = 0; t < T_max; t += slotDuration)
            {
                int start = t;
                int end = Math.Min(t + slotDuration, T_max);

                var activeLinks = links
                    .Where(l => l.StartTime < end && l.EndTime > start)
                    .ToList();

                var adj = new List<int>[n];
                for (int i = 0; i < n; i++) adj[i] = new List<int>();
                foreach (var link in activeLinks)
                {
                    adj[link.SatelliteA].Add(link.SatelliteB);
                    adj[link.SatelliteB].Add(link.SatelliteA);
                }

                bool[] visited = new bool[n];
                var groups = new List<HashSet<int>>();
                for (int i = 0; i < n; i++)
                {
                    if (!visited[i])
                    {
                        var group = new HashSet<int>();
                        var queue = new Queue<int>();
                        queue.Enqueue(i);
                        visited[i] = true;
                        while (queue.Count > 0)
                        {
                            int v = queue.Dequeue();
                            group.Add(v);
                            foreach (int neighbor in adj[v])
                                if (!visited[neighbor])
                                {
                                    visited[neighbor] = true;
                                    queue.Enqueue(neighbor);
                                }
                        }
                        groups.Add(group);
                    }
                }

                slots.Add(new TimeSlot
                {
                    Start = start,
                    End = end,
                    Groups = groups,
                    ActiveLinks = activeLinks
                });
            }
            return slots;
        }
    }
}