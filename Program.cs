using System;
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
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] // Assuming max length of 256 for simplicity
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
        public string VehicleRegistration;
    }

    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula to calculate the distance between two points on the Earth
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
        // Read binary data from file and convert to array of VehicleData
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", filename);
        byte[] fileBytes = File.ReadAllBytes(filePath);
        int offset = 0;
        int numEntries = 0;

        // First, count the number of entries
        while (offset < fileBytes.Length)
        {
            // Skip VehicleId
            offset += sizeof(int);

            // Find the end of the null-terminated string
            int stringEnd = Array.IndexOf(fileBytes, (byte)0, offset);
            if (stringEnd == -1) break; // No null terminator found, stop reading
            offset = stringEnd + 1;

            // Skip Latitude, Longitude, and RecordedTimeUTC
            offset += sizeof(float) + sizeof(float) + sizeof(ulong);

            numEntries++;
        }

        // Reset offset for actual reading
        offset = 0;
        VehicleData[] vehicles = new VehicleData[numEntries];

        for (int i = 0; i < numEntries; i++)
        {
            vehicles[i].VehicleId = BitConverter.ToInt32(fileBytes, offset);
            offset += sizeof(int);

            // Read the null-terminated string
            int stringEnd = Array.IndexOf(fileBytes, (byte)0, offset);
            vehicles[i].VehicleRegistration = System.Text.Encoding.ASCII.GetString(fileBytes, offset, stringEnd - offset);
            offset = stringEnd + 1;

            vehicles[i].Latitude = BitConverter.ToSingle(fileBytes, offset);
            offset += sizeof(float);

            vehicles[i].Longitude = BitConverter.ToSingle(fileBytes, offset);
            offset += sizeof(float);

            vehicles[i].RecordedTimeUTC = BitConverter.ToUInt64(fileBytes, offset);
            offset += sizeof(ulong);
        }

        return vehicles;
    }


    public static void FindClosestRegistrations(Position[] positions, VehicleData[] vehicles)
    {
        // Sort vehicles by latitude for binary search
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
                positions[i].VehicleRegistration = vehicles[closestIndex].VehicleRegistration;
            }
        }
    }

    public static void Main()
    {
        var startTime = DateTime.Now;

        // 10 pre-defined co-ordinates or positions
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
        Console.WriteLine($"File reading execution time: {Math.Round((endTimeFile - startTimeFile).TotalMilliseconds)} milliseconds");

        // Print the number of items read from the file
        Console.WriteLine($"Number of items read from the file: {vehicles.Length}");

        // Find the closest registrations
        var startTimeClosest = DateTime.Now;
        FindClosestRegistrations(positions, vehicles);
        var endTimeClosest = DateTime.Now;
        Console.WriteLine($"Finding closest vehicle execution time: {Math.Round((endTimeClosest - startTimeClosest).TotalMilliseconds)} milliseconds");

        // Print the results
        foreach (var position in positions)
        {
            Console.WriteLine($"Pos {position.PositionId}: {{ID: {position.ClosestId}, Registration: {position.VehicleRegistration}}}");
        }

        // Print total execution time
        var endTime = DateTime.Now;
        Console.WriteLine($"Total execution time: {Math.Round((endTime - startTime).TotalMilliseconds)} milliseconds");
    }
}
