using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Snowflake.Data.Client;
using System.Data;

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
                    command.CommandText = "CALL ADD_USER (:email, :name, :password, :is_locked, :failed_attempts, :last_password_reset, :is_new, :salt)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "name", Value = user.Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "password", Value = hashedPassword, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_locked", Value = false, DbType = DbType.Boolean });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "failed_attempts", Value = 0, DbType = DbType.Int32 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "last_password_reset", Value = DateTime.Now, DbType = DbType.DateTime });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_new", Value = true, DbType = DbType.Boolean });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "salt", Value = salt, DbType = DbType.String });

                    isDuplicate = Convert.ToBoolean(command.ExecuteScalar());
                }

                if (isDuplicate)
                {
                    throw new DuplicateNameException("Account already exists.");
                }
            }
        }

        public async Task<IDataReader> GetEmailSettings()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                
                IDbCommand command = conn.CreateCommand();
                command.CommandText = "CALL GET_EMAIL_SETTINGS()";
                return command.ExecuteReader();
            }
        }

        public async Task<ApplicationUser> GetUser(string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                Console.WriteLine("get user connection opened");
                IDbCommand getSetting = conn.CreateCommand();
                getSetting.CommandText = "SELECT value FROM system_settings WHERE setting_type = 'MAX_DAYS_BEFORE_LOCKED'";
                int maxDaysBeforeLocked = Convert.ToInt32(getSetting.ExecuteScalar());
                Console.WriteLine("max days pulled");

                ApplicationUser currUser = new ApplicationUser();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL GET_USER_INFO ('{email}')";
                Console.WriteLine("command created");
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Console.WriteLine("reading the result");
                        currUser.Name = reader.GetString(0);
                        currUser.Email = reader.GetString(1);
                        currUser.FailedAttempts = reader.GetInt32(3);
                        currUser.IsNew = reader.GetBoolean(5);
                        currUser.LastReset = reader.GetDateTime(4);

                        int daysSinceLastChange = (DateTime.Now - currUser.LastReset).Days;
                        currUser.IsLocked = daysSinceLastChange >= maxDaysBeforeLocked ? true : reader.GetBoolean(2);
                        Console.WriteLine("get user connection opened");
                    } else
                    {
                        throw new InvalidOperationException("User does not exist.");
                    }
                }
                Console.WriteLine("user pulled from database");
                return currUser;
            }
        }

        public void ResetPassword(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"UPDATE user_information SET password = '{user.Password}' WHERE email = '{user.Email}";
                command.ExecuteScalar();
            }
        }

        public async Task<string> GetStringField(ApplicationUser user, string field)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"CALL GET_STRING_FIELD(:email, :field)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "field", Value = field, DbType = DbType.String });
                    Console.WriteLine("field retrieved");
                    return command.ExecuteScalar().ToString();
                }
            }
        }

        public async Task<string> UpdateAttempts(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand getSetting = conn.CreateCommand();
                getSetting.CommandText = "CALL GET_MAX_FAILED_ATTEMPTS()";
                Console.WriteLine("command ready to execute");
                Console.WriteLine(getSetting.ExecuteScalar());
                int maxAttempts = Convert.ToInt32(getSetting.ExecuteScalar());
                Console.WriteLine("max attempts obtained");
                int updatedAttempts = user.FailedAttempts + 1;

                using (IDbCommand command = conn.CreateCommand())
                {
                    Console.WriteLine("command created");
                    command.CommandText = "CALL UPDATE_ATTEMPTS(:email, :updated, :max_attempts)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "updated", Value = updatedAttempts, DbType = DbType.Int32 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "max_attempts", Value = maxAttempts - 1, DbType = DbType.Int32 });

                    Console.WriteLine("command ready to execute");
                    return command.ExecuteScalar().ToString();
                }
            }
        }
    }
}