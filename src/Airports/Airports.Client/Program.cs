﻿namespace Airports.Client
{
    using System;
    using System.Linq;
    using Airports.Data;
    using Airports.Data.Migrations;
    using Airports.Data.Exporters;
    using Airports.Models;
    using System.Data.Entity;
    using Airports.Data.Importers;
    using Airports.Data.Loaders;

    class Program
    {
        private const string SampleFlightsArchivedFilePath = @"..\..\..\..\Imports\Sample-Flights.zip";
        private const string SampleFlightsUnpackedDestinationPath = @"..\..\..\..\Imports\Sample-Flights-Unpacked\";

        static void Main()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<AirportsDbContext, Configuration>());
            
            /*Task 1:
             * a) Extract *.xls and *.xlsx files from a zip archive; read and load the data into SQL Server.
             * b) Import data MongoDb into SQL Server. */
            using (AirportsDbContext dbContext = new AirportsDbContext())
            {
                var sqlServerLoader = new SqlServerDataLoader();

                var zipExtractor = new ZipExtractor();
                Console.WriteLine("Unpacking zip archive...");
                zipExtractor.Extract(SampleFlightsArchivedFilePath, SampleFlightsUnpackedDestinationPath);
                
                var excelDataImporter = new ExcelDataImporter();
                Console.WriteLine("Importing xls flight data from directory...");
                var importedFlights = excelDataImporter.ImportFlightsDataFromDirectory(SampleFlightsUnpackedDestinationPath);
                
                Console.WriteLine("Loading imported flights to SQL Server...");
                foreach (var flight in importedFlights)
                {
                    sqlServerLoader.LoadFlight(flight, dbContext);
                }

                Console.WriteLine("Done.");
            }
        }
    }
}

