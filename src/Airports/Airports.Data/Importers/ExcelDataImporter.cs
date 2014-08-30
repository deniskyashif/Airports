﻿namespace Airports.Data.Importers
{
    using Airports.Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.OleDb;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class ExcelDataImporter
    {
        private const string WorksheetFileExtensionPattern = @".xls[x]?\b";
        private const string FlightsWorksheetPattern = @"\b\w{3}-(Departures|Arrivals)-\d{2}-\w{3}-\d{4}.xls[x]?\b";

        public ICollection<Flight> ImportFlightsDataFromDirectory(string directoryPath)
        {
            IEnumerable<string> filePaths = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                                     .Where(p => Regex.IsMatch(p, FlightsWorksheetPattern));

            ICollection<Flight> flights = new HashSet<Flight>();

            foreach (var path in filePaths)
            {
                foreach (var flight in this.ImportFlightsDataFromFile(path))
                {
                    flights.Add(flight);
                }
            }

            return flights;
        }

        public ICollection<Flight> ImportFlightsDataFromFile(string filePath)
        {
            OleDbConnection connection = new OleDbConnection();
            connection.ConnectionString = string.Format(ConnectionStrings.Default.ExcelReaderConnectionString, filePath);

            connection.Open();

            using (connection)
            {
                var schema = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                var sheetName = schema.Rows[0]["TABLE_NAME"].ToString();

                OleDbCommand selectAllRowsCommand = new OleDbCommand("SELECT * FROM [" + sheetName + "]", connection);

                ICollection<Flight> flights = new HashSet<Flight>();

                using (OleDbDataAdapter adapter = new OleDbDataAdapter(selectAllRowsCommand))
                {
                    DataSet dataSet = new DataSet();
                    adapter.Fill(dataSet);

                    using (DataTableReader reader = dataSet.CreateDataReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string flightCode = reader["FlightCode"].ToString();
                                int airlineId = int.Parse(reader["AirlineId"].ToString());
                                int departureAirportId = int.Parse(reader["DepartureAirportId"].ToString());
                                int arrivalAirportId = int.Parse(reader["ArrivalAirportId"].ToString());
                                double durationHours = double.Parse(reader["Duration"].ToString());
                                DateTime date = DateTime.Parse(reader["DateTime"].ToString(), CultureInfo.InvariantCulture);
                                
                                var flight = new Flight()
                                {
                                    FlightCode = flightCode,
                                    AirlineId = airlineId,
                                    FlightDate = date,
                                    DurationHours = durationHours,
                                    DepartureAirportId = departureAirportId,
                                    ArrivalAirportId = arrivalAirportId
                                };

                                flights.Add(flight);
                            }
                            catch (FormatException)
                            { }
                        }
                    }
                }

                return flights;
            }
        }
    }
}