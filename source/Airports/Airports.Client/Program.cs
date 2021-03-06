﻿namespace Airports.Client
{
    using Airports.Data.MongoDb;
    using Airports.Data.MySql.Exporters;
    using Airports.Data.MySql.Importers;
    using Airports.Data.SqlServer;
    using Airports.Data.SqlServer.Exporters;
    using Airports.Data.SqlServer.Importers;
    using Airports.Models;
    using Airports.SqlServer.Data.Exporters;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class Program
    {
        private const string SampleFlightsArchivedFilePath = @"..\..\..\..\imports\Sample-Flights.zip";
        private const string SampleFlightsUnpackedDestinationPath = @"..\..\..\..\imports\Sample-Flights-Unpacked\";
        private const string SampleFlightsDataXmlFilePath = @"..\..\..\..\imports\CDG-Departures-01-Sep-2014.xml";
        private const string JsonReportsDestionationPath = @"..\..\..\..\exports\Json-Reports\";
        private const string ExcelReportsDestinationPath = @"..\..\..\..\exports\Excel-Reports\";
        private const string PdfReportsFolderPath = @"..\..\..\..\exports\PDF-Reports\";
        private const string PdfReportsFileName = @"flight-report";
        private const string XmlReportsFolderPath = @"..\..\..\..\exports\Xml-Reports\";

        static void Main()
        {
            IAirportsDataSqlServer airportsData = new AirportsDataSqlServer();
            IAirportsDataMongoDb mongoData = new AirportsDataMongoDb();
            
            /*Task 1:
             * a) Extract *.xls and *.xlsx files from a zip archive; read and load the data into SQL Server.
             * b) Import data from MongoDb to SQL Server. */
            ExtractZipAndImportDataFromExcelAndMongoDb(airportsData, mongoData);
            
            //Task 2: Generate PDF Reports
            GeneratePdfFlightsReport(airportsData);

            //Tast 3: Generate report in XML format 
            GenerateXmlFlightsReport(airportsData);

            // Task 4: a) Genetare JSON reports from SQL Server to file system.
            //         b) Import reports from file system (.json files) to MySQL.
            GenerateJsonFlightsReportsAndLoadToMySql(airportsData);

            /* Task 5: Load Data from XML and save it in SQL Server and MongoDb */
            ImportFlightsDataFromXmlAndLoadToMongoDb(airportsData, mongoData);

            /* Task 6: Export merged report from MySql and SQLite to Excel 2007 file */
            ComposeDataFromSQLiteAndMySqlAndExportToExcel();

            Console.WriteLine("Done.");
        }

        private static void ExtractZipAndImportDataFromExcelAndMongoDb(
            IAirportsDataSqlServer airportsData, 
            IAirportsDataMongoDb mongoData)
        {
            Console.WriteLine("Unpacking zip archive...");
            var zipExtractor = new ZipExtractor();
            zipExtractor.Extract(SampleFlightsArchivedFilePath, SampleFlightsUnpackedDestinationPath);

            Console.WriteLine("Importing flights data from directory with xls/xlsx files...");
            var excelDataImporter = new ExcelDataImporter();
            ICollection<Flight> importedFlightsFromExcel = excelDataImporter
                .ImportFlightsDataFromDirectory(SampleFlightsUnpackedDestinationPath);

            Console.WriteLine("Loading imported flights from Excel to SQL Server...");

            foreach (var flight in importedFlightsFromExcel)
            {
                airportsData.Flights.Add(flight);
            }

            airportsData.SaveChanges();

            var countriesFromMongo = mongoData.Countries.ToList();
            
            foreach (var country in countriesFromMongo)
            {
                airportsData.Countries.Add(country);
            }

            var citiesFromMongo = mongoData.Cities.ToList();

            foreach (var city in citiesFromMongo)
            {
                airportsData.Cities.Add(city);
            }

            airportsData.SaveChanges();
        }

        private static void GeneratePdfFlightsReport(IAirportsDataSqlServer airportsData)
        {
            Console.WriteLine("Exporting PDF flight report...");

            var pdfExporter = new PdfFileExporter();
            pdfExporter.GenerateAggregatedAirlineReports(PdfReportsFolderPath, PdfReportsFileName, airportsData);
        }

        private static void GenerateXmlFlightsReport(IAirportsDataSqlServer airportsData)
        {
            Console.WriteLine("Exporting XML airlines report...");
            XmlFileExporter.GenerateAirlinesReport(XmlReportsFolderPath, airportsData);
        }

        private static void GenerateJsonFlightsReportsAndLoadToMySql(IAirportsDataSqlServer airportsData)
        {
            var jsonExporter = new JsonExporter();
            jsonExporter.GenerateReportsForGivenPeriod(airportsData, JsonReportsDestionationPath);

            var mySqlImporter = new JsonDataImporter();
            mySqlImporter.ImportAirlineReportsFromDirectory(JsonReportsDestionationPath);
        }

        private static void ImportFlightsDataFromXmlAndLoadToMongoDb(IAirportsDataSqlServer airportsData, IAirportsDataMongoDb mongoData)
        {
            Console.WriteLine("Importing flights data from xml file...");

            XmlDataImporter xmlDataImporter = new XmlDataImporter();

            ICollection<Flight> importedFlightsFromXml = 
                xmlDataImporter.ImportFlightsDataFromFile(SampleFlightsDataXmlFilePath);

            Console.WriteLine("Loading imported flights to SQL Server and MongoDb...");

            foreach (var flight in importedFlightsFromXml)
            {
                airportsData.Flights.Add(flight);
                mongoData.Save(flight);
            }

            airportsData.SaveChanges();
        }

        private static void ComposeDataFromSQLiteAndMySqlAndExportToExcel()
        {
            ExcelExporter excelExporter = new ExcelExporter();
            excelExporter.GenerateCompositeAirlinesReport(ExcelReportsDestinationPath);
        }
    }
}