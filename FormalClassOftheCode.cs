// Decompiled with JetBrains decompiler
// Type: Program
// Assembly: ScriptExecutionTool, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FFAFD0DF-D7F5-45A3-8358-4DC5748D702B
// Assembly location: C:\Users\sunday.oladiran\Desktop\Projects\Release\AnyScriptExecutionTool\ScriptExecutionTool.dll

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


#nullable enable
public class Program
{
    private static IServiceProvider _serviceProvider;
    private static ILogger _log;

    public static void ConfigureServices(
      HostBuilderContext context,
      IServiceCollection serviceCollection)
    {
        new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", true, true).AddEnvironmentVariables().Build();
        serviceCollection.AddEntityFrameworkSqlServer();
    }

    public static async Task<int> Main(string[] args)
    {
        using (IHost host = Program.CreateHostBuilder(args).Build())
        {
            IConfiguration config = (IConfiguration)new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true).Build();
            await host.StartAsync();
            IHostApplicationLifetime requiredService = host.Services.GetRequiredService<IHostApplicationLifetime>();
            Console.WriteLine("DBScriptExecutionApp =====APPLICATION STARTED!=============");
            ServiceCollection services = new ServiceCollection();
            Program.ConfigureServices((HostBuilderContext)null, (IServiceCollection)services);
            Program._serviceProvider = (IServiceProvider)services.BuildServiceProvider();
            Console.WriteLine("DBScriptExecutionApp =====BEGINS THE EXECUTION SERIES===!");
            string connectionString1 = config.GetConnectionString("DigitalOperationContext");
            Console.WriteLine("DBScriptExecutionApp =====CONNECTIONSTRING:" + connectionString1 + "===");
            string path2 = "Data.sql";
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), path2);
            string path = Program.GetFilesByExtension(AppDomain.CurrentDomain.BaseDirectory, ".sql", SearchOption.AllDirectories).First<string>().ToString();
            string connectionString2 = connectionString1;
            string rawquery = File.ReadAllText(path).Replace("\r\n", "");
            SqlConnection sqlConnection = new SqlConnection(connectionString2);
            using (SqlConnection connection = new SqlConnection(connectionString2))
            {
                Console.WriteLine("DBScriptExecutionApp =====The script to execute: " + rawquery + "=============");
                int num1 = rawquery.ToLower().Contains("update") ? 1 : (rawquery.ToLower().Contains("delete") ? 2 : 3);
                Console.WriteLine("DBScriptExecutionApp =====About to open connection...");
                connection.Open();
                Console.WriteLine("DBScriptExecutionApp =====Connection opened successfully!");
                Console.WriteLine("DBScriptExecutionApp =====Script successfully executed!");
                try
                {
                    switch (num1)
                    {
                        case 1:
                            int num2 = rawquery.ToLower().Contains("where") && rawquery.ToLower().Contains("@") ? Program.ExecuteQueryAsParameterized(rawquery, connection) : throw new Exception("Update Op: Invalid Query supplied. Check your query and try again!");
                            DefaultInterpolatedStringHandler interpolatedStringHandler1 = new DefaultInterpolatedStringHandler(85, 1);
                            interpolatedStringHandler1.AppendLiteral("DBScriptExecutionApp =====Status update successfully performed - NoOfRecordAffected:");
                            interpolatedStringHandler1.AppendFormatted<int>(num2);
                            interpolatedStringHandler1.AppendLiteral("!");
                            Console.WriteLine(interpolatedStringHandler1.ToStringAndClear());
                            break;
                        case 2:
                            int num3 = rawquery.ToLower().Contains("where") ? Program.ExecuteQueryAsParameterized(rawquery, connection) : throw new Exception("Delete Op: Invalid Query supplied. Check your query and try again!");
                            DefaultInterpolatedStringHandler interpolatedStringHandler2 = new DefaultInterpolatedStringHandler(85, 1);
                            interpolatedStringHandler2.AppendLiteral("DBScriptExecutionApp =====Status delete successfully performed - NoOfRecordAffected:");
                            interpolatedStringHandler2.AppendFormatted<int>(num3);
                            interpolatedStringHandler2.AppendLiteral("!");
                            Console.WriteLine(interpolatedStringHandler2.ToStringAndClear());
                            break;
                        default:
                            Program.ExecuteQueryAsParameterized(rawquery, connection, 2);
                            break;
                    }
                }
                finally
                {
                    connection.Close();
                    Console.WriteLine("DBScriptExecutionApp =====Connection closed successfully!");
                }
            }
            Console.WriteLine("DBScriptExecutionApp =====COMPLETED THE EXECUTION SERIES!=======");
            requiredService.StopApplication();
            await host.WaitForShutdownAsync();
            config = (IConfiguration)null;
        }
        return 0;
    }

    public static IEnumerable<string> GetFilesByExtension(
      string directoryPath,
      string extension,
      SearchOption searchOption)
    {
        return Directory.EnumerateFiles(directoryPath, "*" + extension, searchOption).Where<string>((Func<string, bool>)(x => string.Equals(Path.GetExtension(x), extension, StringComparison.InvariantCultureIgnoreCase)));
    }

    public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args).UseConsoleLifetime().ConfigureServices(new Action<HostBuilderContext, IServiceCollection>(Program.ConfigureServices));

    private static int ExecuteQueryAsParameterized(
      string rawquery,
      SqlConnection connection,
      int type = 1)
    {
        int num1 = 0;
        string empty1 = string.Empty;
        string empty2 = string.Empty;
        if (rawquery.Contains(":"))
        {
            string[] strArray1 = rawquery.Trim().Split(':');
            string str = strArray1[0];
            string[] strArray2 = strArray1[1].Split('@');
            if (str.Count<char>((Func<char, bool>)(x => x == '@')) == strArray1[1].Count<char>((Func<char, bool>)(x => x == '@')) + 1)
            {
                using (SqlCommand selectCommand = new SqlCommand(str, connection))
                {
                    MatchCollection matchCollection = new Regex("\\B@\\w+\\b").Matches(str);
                    int num2 = 0;
                    foreach (Match match in matchCollection)
                        selectCommand.Parameters.AddWithValue(match.Value, (object)strArray2[num2++]);
                    if (type == 1)
                    {
                        num1 = selectCommand.ExecuteNonQuery();
                    }
                    else
                    {
                        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(selectCommand);
                        DataSet dataSet1 = new DataSet();
                        DataSet dataSet2 = dataSet1;
                        sqlDataAdapter.Fill(dataSet2);
                        connection.Close();
                        try
                        {
                            DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(73, 1);
                            interpolatedStringHandler.AppendLiteral("DBScriptExecutionApp =====Successfully retrieved ===>Datasets ");
                            interpolatedStringHandler.AppendFormatted<int>(dataSet1.Tables.Count);
                            interpolatedStringHandler.AppendLiteral(" record(s)!");
                            Console.WriteLine(interpolatedStringHandler.ToStringAndClear());
                            if (dataSet1.Tables.Count > 0)
                            {
                                interpolatedStringHandler = new DefaultInterpolatedStringHandler(77, 1);
                                interpolatedStringHandler.AppendLiteral("DBScriptExecutionApp =====Successfully retrieved ===>RecordTable1 ");
                                interpolatedStringHandler.AppendFormatted<int>(dataSet1.Tables[0].Rows.Count);
                                interpolatedStringHandler.AppendLiteral(" record(s)!");
                                Console.WriteLine(interpolatedStringHandler.ToStringAndClear());
                                DataTable table1 = dataSet1.Tables[0];
                                DataTable table2 = dataSet1.Tables[1];
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
        return num1;
    }
}
