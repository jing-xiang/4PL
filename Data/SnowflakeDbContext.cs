using Snowflake.Data;
using Snowflake.Data.Client;
using Microsoft.Extensions.Configuration;
using Amazon.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.CodeAnalysis.CSharp;
using System.Data;
using System.Diagnostics.Contracts;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore.Storage;

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

        public void RegisterUser(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL ADD_NEW_USER (:email, :name, :password, :is_locked, :failed_attempts, :last_password_reset, :is_new_user)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "name", Value = user.Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "password", Value = user.Password, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_locked", Value = false, DbType = DbType.Boolean });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "failed_attempts", Value = 0, DbType = DbType.Int32 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "last_password_reset", Value = DateTime.Now, DbType = DbType.DateTime }) ;
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_new_user", Value = true, DbType = DbType.Boolean });
                   
                    command.ExecuteNonQuery();
                }
            }
        }

        public string GetFieldByEmail(ApplicationUser user, string field)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL GetFieldByEmail(:email, :field)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "field", Value = field });
                    
                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return result.ToString();
                    } 
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}