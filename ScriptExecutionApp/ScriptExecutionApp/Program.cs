using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;



namespace SqlScriptExecutor
{
    class Program
    {
        // See https://aka.ms/new-console-template for more information
        private static IServiceProvider _serviceProvider;
        private static ILogger _log;


        static void Main(string[] args)
        {
            Console.WriteLine("DBScriptExecutionApp =====APPLICATION STARTED!=============");
            

            bool repeat =true;
            while (repeat)
            {
                Console.WriteLine("DBScriptExecutionApp: Enter the path to the SQL script file:");
                string scriptFilePath = Console.ReadLine();

                if (!File.Exists(scriptFilePath))
                {
                    Console.WriteLine("DBScriptExecutionApp:File does not exist!");
                    return;
                }

                string script = File.ReadAllText(scriptFilePath);
                if (string.IsNullOrEmpty(script))
                {
                    Console.WriteLine("DBScriptExecutionApp:Invalid query detected!");
                    return;
                }


                SqlParserResult result = ParseSqlStatement(script);

                if (!IsSQLValid(result))
                {
                    Console.WriteLine("DBScriptExecutionApp:Critical! Are you sure you want to execute this query? Enter yes to proceed...");
                    string yesorno = Console.ReadLine();
                    if (yesorno.ToLower() != "yes")
                        return;
                }

                if (!string.IsNullOrEmpty(script) && (script.ToLower().Contains("update") || script.ToLower().Contains("delete") || script.ToLower().Contains("truncate")) && !script.ToLower().Contains("where"))
                {
                    Console.WriteLine("DBScriptExecutionApp:Invalid query detected!");
                    return;
                }
                if (!string.IsNullOrEmpty(script) && (script.ToLower().Contains("drop")) || script.ToLower().Contains("truncate") || script.ToLower().Contains("delete") || script.ToLower().Contains("update"))
                {
                    Console.WriteLine("DBScriptExecutionApp:Critical! Are you sure you want to execute this query? Enter yes to proceed...");
                    string yesorno = Console.ReadLine();
                    if (yesorno.ToLower() != "yes")
                        return;
                }

                Console.WriteLine("DBScriptExecutionApp:Fetching the connection string:");
                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = configuration.GetConnectionString("DigitalOperationContext");

                int resultCount = 0;
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();

                        // Extract parameter names from the script
                        List<string> parameterNames = ExtractParameterNames(script);
                        Console.WriteLine("DBScriptExecutionApp: ===CON OPENED & EXECUTION MODE ON===!");
                        // Prompt for parameter values
                        Dictionary<string, string> parameterValues = PromptForParameterValues(parameterNames);

                        using (SqlCommand command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = script;

                            // Set parameter values
                            foreach (KeyValuePair<string, string> parameter in parameterValues)
                            {
                                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                            }
                            Console.WriteLine($"DBScriptExecutionApp:SQL script to execute below:");
                            Console.WriteLine("Original SQL script:");
                            Console.WriteLine("                              ");
                            Console.WriteLine($"{command.CommandText}");
                            string updatedScript = GetUpdatedScript(command);
                            Console.WriteLine("Updated SQL script:");
                            Console.WriteLine("                              ");
                            Console.WriteLine(updatedScript);

                            Console.WriteLine("Press 'X' to cancel or Enter to Execute!");
                            string decision = Console.ReadLine();
                            if (decision != "" && decision.ToLower() == "x")
                            {
                                return;
                            }

                            resultCount = command.ExecuteNonQuery();
                        }

                        Console.WriteLine($"DBScriptExecutionApp:SQL script executed successfully. Count:{resultCount}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DBScriptExecutionApp:An error occurred: " + ex.Message);
                }

                Console.WriteLine("Do you want to continue? (Y/N)");
                string choice = Console.ReadLine();

                if (choice.Equals("N", StringComparison.OrdinalIgnoreCase))
                {
                    repeat = false;
                }
            }

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            
        }

        static string GetUpdatedScript(SqlCommand command)
        {
            string updatedScript = command.CommandText;

            foreach (SqlParameter parameter in command.Parameters)
            {
                string parameterName = parameter.ParameterName;
                string value = GetParameterValueString(parameter.Value);

                updatedScript = updatedScript.Replace(parameterName, value);
            }

            return updatedScript;
        }

        static string GetParameterValueString(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return "NULL";
            }
            else if (value is string)
            {
                return $"'{value}'";
            }
            else
            {
                return value.ToString();
            }
        }
        static bool IsSQLValid(SqlParserResult result)
            {
            if (result.IsValid)
            {
                Console.WriteLine("SQL statement is valid.");
                Console.WriteLine("Table: " + result.Table);
                Console.WriteLine("Columns: " + string.Join(", ", result.Columns));
                return true;
            }
            else
            {
                Console.WriteLine("SQL statement is not valid.");
                Console.WriteLine("Error: " + result.ErrorMessage);
                return false;
            }
        }
        static List<string> ExtractParameterNames(string script)
        {
            List<string> parameterNames = new List<string>();

            // Use a regular expression to find parameter names in the script
            Regex regex = new Regex(@"\@\w+");
            MatchCollection matches = regex.Matches(script);

            foreach (Match match in matches)
            {
                string parameterName = match.Value;
                if (!parameterNames.Contains(parameterName))
                {
                    parameterNames.Add(parameterName);
                }
            }

            return parameterNames;
        }

        static Dictionary<string, string> PromptForParameterValues(List<string> parameterNames)
        {
            Dictionary<string, string> parameterValues = new Dictionary<string, string>();

            foreach (string parameterName in parameterNames)
            {
                Console.WriteLine($"DBScriptExecutionApp: Enter the value for parameter '{parameterName}':");
                string value = Console.ReadLine();
                parameterValues.Add(parameterName, value);
            }

            return parameterValues;
        }

        static SqlParserResult ParseSqlStatement(string sql)
        {
            SqlParserResult result = new SqlParserResult();

            // Extract statement type
            if (Regex.IsMatch(sql, @"^\s*SELECT", RegexOptions.IgnoreCase))
            {
                result.StatementType = SqlStatementType.Select;
            }
            else if (Regex.IsMatch(sql, @"^\s*UPDATE", RegexOptions.IgnoreCase))
            {
                result.StatementType = SqlStatementType.Update;
            }
            else if (Regex.IsMatch(sql, @"^\s*DELETE", RegexOptions.IgnoreCase))
            {
                result.StatementType = SqlStatementType.Delete;
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid SQL statement: Unknown statement type.";
                return result;
            }

            // Extract table name
            Match match = Regex.Match(sql, @"FROM\s+([^\s]+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                result.Table = match.Groups[1].Value;
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid SQL statement: Missing FROM clause.";
                return result;
            }

            // Extract column names
            if (result.StatementType == SqlStatementType.Select)
            {
                match = Regex.Match(sql, @"SELECT\s+([\w\s\*,]+)\s+FROM", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string columnsStr = match.Groups[1].Value.Trim();
                    result.Columns = columnsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Invalid SQL statement: Missing SELECT clause.";
                    return result;
                }
            }

            return result;
        }


    }
    

    enum SqlStatementType
    {
        Select,
        Update,
        Delete
    }
    class SqlParserResult
    {
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; }
    public SqlStatementType StatementType { get; set; }
    public string Table { get; set; }
    public string[] Columns { get; set; }
  }





}

