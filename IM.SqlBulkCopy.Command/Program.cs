using E6.Metrics.BI.Helpers;
using IM.BulkInsert.Tutorial.Entities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using static System.Console;

namespace IM.BulkInsert.Tutorial
{
    class Program
    {
        private const int BatchSize = 2500;
        private const int NotifyAfter = 100;
        private const string DestinationTableName = "Employees";

        static void Main(string[] args)
        {
            BulkInsert();

            WriteLine();
            WriteLine("Press any key to continue");
            ReadKey();
        }

        private static void BulkInsert()
        {
            try
            {
                WriteLine("Bulk insert");

                var employees = GetBulkEmployees();

                var connectionString = ConfigurationManager.ConnectionStrings["SqlBulkCopyDbContext"].ConnectionString;

                using (var dbConnection = new SqlConnection(connectionString))
                {
                    try
                    {
                        dbConnection.Open();

                        using (var bcp = new SqlBulkCopy(dbConnection))
                        {
                            SetupColumnMappings(bcp);

                            // Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server
                            bcp.BatchSize = BatchSize;
                            // Defines the number of rows to be processed before generating a notification event
                            bcp.NotifyAfter = NotifyAfter;
                            // Name of the destination table on the server
                            bcp.DestinationTableName = DestinationTableName;
                            // Occurs every time that the number of rows specified by the NotifyAfter property have been processed
                            bcp.SqlRowsCopied += OnSqlRowsTransfer;
                            //  Copies all rows in the supplied System.Data.DataTable to a destination table
                            bcp.WriteToServer(employees.AsDataTable());
                        }
                    }
                    finally
                    {
                        dbConnection.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Set up the column mappings by name, if your Model columns order does not match Table columns order
        /// </summary>
        private static void SetupColumnMappings(SqlBulkCopy bcp)
        {
            bcp.ColumnMappings.Add("Id", "Id");
            bcp.ColumnMappings.Add("LastName", "LastName");
            bcp.ColumnMappings.Add("FirstName", "FirstName");
            bcp.ColumnMappings.Add("MyAddress", "Address");
            bcp.ColumnMappings.Add("City", "City");
        }

        private static void OnSqlRowsTransfer(object sender, SqlRowsCopiedEventArgs e)
        {
            Write(".");
        }

        private static IEnumerable<Employee> GetBulkEmployees()
        {
            var employees = new List<Employee>();

            for (int i = 0; i < 10000; i++)
            {
                var employee = new Employee
                {
                    FirstName = $"FirstName{i}",
                    LastName = $"LastName{i}",
                    MyAddress = $"Address{i}",
                    City = $"City{i}",
                };

                employees.Add(employee);
            }

            return employees;
        }
    }
}
