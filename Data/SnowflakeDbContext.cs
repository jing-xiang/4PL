using Snowflake.Data;
using Snowflake.Data.Client;
using Microsoft.Extensions.Configuration;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.CSharp;
using System.Data;
using System.Diagnostics.Contracts;
using Microsoft.Identity.Client;

namespace _4PL.Data
{
    public class SnowflakeDbContext
    {
        private readonly string _connectionString;

        public SnowflakeDbContext()
        {
            var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .Build();

            _connectionString = configuration.GetConnectionString("SnowflakeConnection");
        }

        public void ExecuteStoredProc(string storedProc, ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                Console.WriteLine("connection open successful");
                using (IDbCommand command = conn.CreateCommand())
                {
                    Console.WriteLine("command created");
                    command.CommandText = $"CALL {storedProc}(?, ?, ?, ?, ?, ?, ?)";
                    Console.WriteLine("command text updated");

                    var email = command.CreateParameter();
                    email.ParameterName = "email";
                    email.Value = user.Email;
                    email.DbType = DbType.String;
                    command.Parameters.Add(email);
                    Console.WriteLine("email added");

                    var name = command.CreateParameter();
                    email.ParameterName = "name";
                    email.Value = user.Name;
                    email.DbType = DbType.String;
                    command.Parameters.Add(name);
                    Console.WriteLine("name added");

                    var password = command.CreateParameter();
                    email.ParameterName = "password";
                    email.Value = user.Password;
                    email.DbType = DbType.String;
                    command.Parameters.Add(password);
                    Console.WriteLine("password added");

                    var isLocked = command.CreateParameter();
                    email.ParameterName = "is_locked";
                    email.Value = user.IsLocked;
                    email.DbType = DbType.Boolean;
                    command.Parameters.Add(isLocked);
                    Console.WriteLine("isLocked added");

                    var failedAttempts = command.CreateParameter();
                    email.ParameterName = "failed_attempts";
                    email.Value = user.FailedAttempts;
                    email.DbType = DbType.Int32;
                    command.Parameters.Add(failedAttempts);
                    Console.WriteLine("attempts added");

                    var lastReset = command.CreateParameter();
                    email.ParameterName = "last_password_reset";
                    email.Value = user.LastReset;
                    email.DbType = DbType.DateTime;
                    command.Parameters.Add(lastReset);
                    Console.WriteLine("lastReset added");

                    var isNew = command.CreateParameter();
                    email.ParameterName = "is_new_user";
                    email.Value = user.IsNew;
                    email.DbType = DbType.Boolean;
                    command.Parameters.Add(isNew);
                    Console.WriteLine("isNew added");

                    Console.WriteLine("all command parameters added");


                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"EXECUTION ERROR: {ex.Message}");
                    }
                    Console.WriteLine("command executed");
                    command.ExecuteNonQuery();
                }
            }
        }

        private SnowflakeDbParameter CreateParameter(object paramValue, DbType paramType)
        {
            return new SnowflakeDbParameter
            {
                Value = paramValue,
                DbType = paramType
            };
        }
    }
}