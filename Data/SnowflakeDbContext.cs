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
using Microsoft.IdentityModel.Tokens;

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

        public void RegisterUser(ApplicationUser user, string hashedPassword, string salt)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                bool isDuplicate = false;
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT COUNT(*) FROM user_information WHERE email='{user.Email}'";
                    isDuplicate = Convert.ToInt32(command.ExecuteScalar()) > 0;
                }

                if (!isDuplicate)
                {
                    using (IDbCommand command = conn.CreateCommand())
                    {
                        command.CommandText = "CALL ADD_NEW_USER (:email, :name, :password, :is_locked, :failed_attempts, :last_password_reset, :is_new, :salt)";

                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "name", Value = user.Name, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "password", Value = hashedPassword, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_locked", Value = false, DbType = DbType.Boolean });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "failed_attempts", Value = 0, DbType = DbType.Int32 });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "last_password_reset", Value = DateTime.Now, DbType = DbType.DateTime });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_new", Value = true, DbType = DbType.Boolean });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "salt", Value = salt, DbType = DbType.String });
                        
                        command.ExecuteNonQuery();
                    }
                } else
                {
                    throw new DuplicateNameException("Email has already been used for an account.");
                }
            }
        }

        public async Task<ApplicationUser> GetUser(string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                Console.WriteLine("connection started");
                ApplicationUser currUser = new ApplicationUser();
                using (IDbCommand command = conn.CreateCommand())
                {
                    Console.WriteLine("command created");
                    command.CommandText = "CALL GET_SECURE_USER_INFO (:email)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = email, DbType = DbType.String });
                    Console.WriteLine("command ready to run");
                    using (var reader = command.ExecuteReader())
                    {
                        Console.WriteLine("command executed");
                        if (reader.Read())
                        {
                            currUser.Name = reader.GetString(0);
                            currUser.Email = reader.GetString(1);
                            currUser.IsLocked = reader.GetBoolean(3);
                            currUser.FailedAttempts = reader.GetInt32(4);
                            currUser.LastReset = reader.GetDateTime(5);
                            currUser.IsNew = reader.GetBoolean(6);
                            Console.WriteLine("user updated from database");
                        } else
                        {
                            throw new InvalidOperationException("User does not exist.");
                        }
                    }
                }
                Console.WriteLine("user ready to be returned");
                return currUser;
            }
        }

        public async Task<string> RetrieveHash(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT password FROM user_information WHERE email = {user.Email}";
                    return Convert.ToString(command.ExecuteScalar());
                }
            }
        }

        public async Task<string> RetrieveSalt(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT salt FROM user_information WHERE email = {user.Email}";
                    return Convert.ToString(command.ExecuteScalar());
                }
            }
        }

        public async void UpdateAttempts(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    if (user.FailedAttempts < 5)
                    {
                        command.CommandText = $"UPDATE user_information SET failed_attempts = {user.FailedAttempts} WHERE email = {user.Email}";
                    } else
                    {
                        command.CommandText = $"UPDATE user_information SET is_locked = true WHERE email = {user.Email}";
                    }
                    command.ExecuteScalar();
                }
            }
        }
    }
}