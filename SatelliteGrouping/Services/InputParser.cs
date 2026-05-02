using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SatelliteGrouping.Models;

namespace SatelliteGrouping.Services
{
    public static class InputParser
    {
        /// <summary>
        /// Читает входной файл. Формат:
        /// n m
        /// i j ti tj (m строк)
        /// Нумерация спутников с 1, T_max вычисляется как max(tj).
        /// </summary>
        public static (int n, int m, int T_max, List<SatelliteLink> links) Parse(string filePath)
        {
            var lines = File.ReadAllLines(filePath)
                            .Where(l => !string.IsNullOrWhiteSpace(l))
                            .ToArray();
            if (lines.Length < 2) throw new FormatException("Недостаточно строк в файле.");

            var first = lines[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (first.Length < 2)
                throw new FormatException("Первая строка должна содержать n и m.");
            int n = int.Parse(first[0]);
            int m = int.Parse(first[1]);

            var links = new List<SatelliteLink>();
            int T_max = 0;
            for (int i = 1; i <= m; i++)
            {
                if (i >= lines.Length)
                    throw new FormatException($"Ожидалось {m} строк с интервалами, но найдено меньше.");
                var parts = lines[i].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                    throw new FormatException($"Строка интервала должна содержать 4 числа: i j ti tj.");

                int satA = int.Parse(parts[0]) - 1;
                int satB = int.Parse(parts[1]) - 1;
                int ti = int.Parse(parts[2]);
                int tj = int.Parse(parts[3]);

                if (tj > T_max) T_max = tj;

                links.Add(new SatelliteLink
                {
                    SatelliteA = satA,
                    SatelliteB = satB,
                    StartTime = ti,
                    EndTime = tj
                });
            }
            return (n, m, T_max, links);
        }
    }
}