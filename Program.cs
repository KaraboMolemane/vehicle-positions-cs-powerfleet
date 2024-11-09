﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

public class Program
{
    private const double M_PI = 3.14159265358979323846;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VehicleData
    {
        public int VehicleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string VehicleRegistration;
        public float Latitude;
        public float Longitude;
        public ulong RecordedTimeUTC;
    }

    public struct Position
    {
        public int PositionId;
        public double Latitude;
        public double Longitude;
        public int ClosestId;
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = (lat2 - lat1) * M_PI / 180.0;
        double dLon = (lon2 - lon1) * M_PI / 180.0;
        lat1 = lat1 * M_PI / 180.0;
        lat2 = lat2 * M_PI / 180.0;

        double a = Math.Pow(Math.Sin(dLat / 2), 2) + Math.Pow(Math.Sin(dLon / 2), 2) * Math.Cos(lat1) * Math.Cos(lat2);
        double c = 2 * Math.Asin(Math.Sqrt(a));
        double R = 6371; // Radius of Earth in kilometers
        return R * c;
    }

    public static int CompareLatitude(VehicleData a, VehicleData b)
    {
        return a.Latitude.CompareTo(b.Latitude);
    }

    public static VehicleData[] ReadVehicleData(string filename)
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", filename);
        byte[] fileBytes = File.ReadAllBytes(filePath);
        int size = Marshal.SizeOf(typeof(VehicleData));
        int numEntries = fileBytes.Length / size;

        VehicleData[] vehicles = new VehicleData[numEntries];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        for (int i = 0; i < numEntries; i++)
        {
            Marshal.Copy(fileBytes, i * size, ptr, size);
            vehicles[i] = (VehicleData)Marshal.PtrToStructure(ptr, typeof(VehicleData));
        }

        Marshal.FreeHGlobal(ptr);
        return vehicles;
    }


    public static void FindClosestRegistrations(Position[] positions, VehicleData[] vehicles)
    {
        Array.Sort(vehicles, CompareLatitude);

        for (int i = 0; i < positions.Length; i++)
        {
            var position = positions[i];
            double minDistance = double.MaxValue;
            int closestIndex = -1;

            int left = 0;
            int right = vehicles.Length - 1;
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                double distance = CalculateDistance(position.Latitude, position.Longitude, vehicles[mid].Latitude, vehicles[mid].Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = mid;
                }
                if (vehicles[mid].Latitude < position.Latitude)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            for (int j = closestIndex - 1; j >= 0 && Math.Abs(vehicles[j].Latitude - position.Latitude) < minDistance; j--)
            {
                double distance = CalculateDistance(position.Latitude, position.Longitude, vehicles[j].Latitude, vehicles[j].Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = j;
                }
            }
            for (int j = closestIndex + 1; j < vehicles.Length && Math.Abs(vehicles[j].Latitude - position.Latitude) < minDistance; j++)
            {
                double distance = CalculateDistance(position.Latitude, position.Longitude, vehicles[j].Latitude, vehicles[j].Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = j;
                }
            }

            if (closestIndex != -1)
            {
                positions[i].ClosestId = vehicles[closestIndex].VehicleId;
            }
        }
    }

    public static void Main()
{
    var startTime = DateTime.Now;

    Position[] positions = new Position[]
    {
        new Position { PositionId = 1, Latitude = 34.544909, Longitude = -102.100843 },
        new Position { PositionId = 2, Latitude = 32.345544, Longitude = -99.123124 },
        new Position { PositionId = 3, Latitude = 33.234235, Longitude = -100.214124 },
        new Position { PositionId = 4, Latitude = 35.195739, Longitude = -95.348899 },
        new Position { PositionId = 5, Latitude = 31.895839, Longitude = -97.789573 },
        new Position { PositionId = 6, Latitude = 32.895839, Longitude = -101.789573 },
        new Position { PositionId = 7, Latitude = 34.115839, Longitude = -100.225732 },
        new Position { PositionId = 8, Latitude = 32.335839, Longitude = -99.992232 },
        new Position { PositionId = 9, Latitude = 33.535339, Longitude = -94.792232 },
        new Position { PositionId = 10, Latitude = 32.234235, Longitude = -100.222222 }
    };

    var startTimeFile = DateTime.Now;
    VehicleData[] vehicles = ReadVehicleData("VehiclePositions.dat");
    var endTimeFile = DateTime.Now;
    Console.WriteLine($"File reading execution time: {(endTimeFile - startTimeFile).TotalMilliseconds} milliseconds");

    var startTimeClosest = DateTime.Now;
    FindClosestRegistrations(positions, vehicles);
    var endTimeClosest = DateTime.Now;
    Console.WriteLine($"Finding closest vehicle execution time: {(endTimeClosest - startTimeClosest).TotalMilliseconds} milliseconds");

    foreach (var position in positions)
    {
        Console.WriteLine($"Closest Position ID {position.PositionId}: {position.ClosestId}");
    }

    var endTime = DateTime.Now;
    Console.WriteLine($"Total execution time: {(endTime - startTime).TotalMilliseconds} milliseconds");
}

}
