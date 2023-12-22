using Snowflake.Data.Client;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics.Contracts;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using _4PL.Components.Account.Pages.Manage;
using System.ComponentModel;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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

        public async Task RegisterUser(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                bool isDuplicate = false;
                List<string> access_types = new List<string>();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL ADD_USER(:email, :name, :password, :is_locked, :failed_attempts, :last_password_reset, :is_new, :salt, :token)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "name", Value = user.Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "password", Value = user.Hash, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_locked", Value = false, DbType = DbType.Boolean });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "failed_attempts", Value = 0, DbType = DbType.Int32 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "last_password_reset", Value = DateTime.Now, DbType = DbType.DateTime });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "is_new", Value = true, DbType = DbType.Boolean });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "salt", Value = Convert.ToBase64String(user.Salt), DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "token", Value = user.Token, DbType = DbType.String });

                    isDuplicate = Convert.ToBoolean(command.ExecuteScalar());

                }
                if (isDuplicate)
                {
                    throw new DuplicateNameException("Account already exists.");
                }
                using (IDbCommand command1 = conn.CreateCommand())
                {
                    command1.CommandText = $"SELECT * FROM access_model";
                    using (var reader = command1.ExecuteReader())
                    {

                        while (reader.Read())
                        {
                            //fetch access rights
                            var accesstype = reader.GetString(0);
                            //append to array
                            access_types.Add(accesstype);
                            Console.WriteLine(accesstype);
                        }
                    }
                }
                    for (int i = 0; i < access_types.Count; i++)
                        {
                            IDbCommand command2 = conn.CreateCommand();
                            command2.CommandText = $"INSERT INTO access_control (email, access_type, is_accessible) VALUES ('{user.Email}', '{access_types[i]}', false)";
                            command2.ExecuteScalar();
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

        public async Task<List<ApplicationSetting>> GetSystemSettings()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = "CALL GET_SYSTEM_SETTINGS()";
                IDataReader reader = command.ExecuteReader();
                List<ApplicationSetting> settings = new();

                while (reader.Read())
                {
                    ApplicationSetting setting = new()
                    {
                        SettingType = reader.GetString(0),
                        Value = reader.GetString(1),
                    };
                    settings.Add(setting);
                }
                return settings;
            }
        }

        public void UpdateSetting(ApplicationSetting setting)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = "CALL UPDATE_SYSTEM_SETTING(:setting_type, :new_value)";
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "setting_type", Value = setting.SettingType, DbType = DbType.String });
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "new_value", Value = setting.Value, DbType = DbType.String });

                command.ExecuteScalar();
            }
        }

        public async Task<List<ApplicationUser>> GetUsersByFieldAsync(string field, string value)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL GET_USERS_BY_FIELD('{field}', '{value}')";
                IDataReader reader = command.ExecuteReader();
                List<ApplicationUser> users = new();

                while (reader.Read())
                {
                    ApplicationUser? user = new ApplicationUser(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetInt32(2),
                        reader.GetBoolean(3),
                        reader.GetDateTime(4)
                    );
                    users.Add(user);
                }
                return users;
            }
        }

        public async Task<List<ApplicationUser>> GetUsersByBothAsync(string name, string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL GET_USERS_BY_BOTH('{name}', '{email}')";
                IDataReader reader = command.ExecuteReader();
                List<ApplicationUser> users = new();

                while (reader.Read())
                {
                    ApplicationUser? user = new ApplicationUser(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetInt32(2),
                        reader.GetBoolean(3),
                        reader.GetDateTime(4)
                    );
                    users.Add(user);
                }
                return users;
            }
        }

        public async Task<List<ApplicationUser>> GetAllUsers()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = "CALL GET_ALL_USERS()";
                IDataReader reader = command.ExecuteReader();
                List<ApplicationUser> users = new();

                while (reader.Read())
                {
                    ApplicationUser? user = new ApplicationUser(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetInt32(2),
                        reader.GetBoolean(3),
                        reader.GetDateTime(4)
                    );
                    users.Add(user);
                }
                return users;
            }
        }

        public async Task<ApplicationUser?> VerifyUserExist(string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand getMaxDays = conn.CreateCommand();
                getMaxDays.CommandText = "CALL GET_SETTING('MAX DAYS BEFORE LOCKED')";
                int maxDaysBeforeLocked = Convert.ToInt32(getMaxDays.ExecuteScalar());

                ApplicationUser currUser = new();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL GET_USER_BY_EMAIL('{email}')";
                IDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    currUser.Name = reader.GetString(0);
                    currUser.Email = reader.GetString(1);
                    currUser.FailedAttempts = reader.GetInt32(3);
                    currUser.IsNew = reader.GetBoolean(5);
                    currUser.LastReset = reader.GetDateTime(4);
                    currUser.Token = reader.GetString(6);

                    // check if exceeded maximum number of days without password reset
                    int daysSinceLastChange = (DateTime.Now - currUser.LastReset).Days;
                    currUser.IsLocked = daysSinceLastChange >= maxDaysBeforeLocked ? true : reader.GetBoolean(2);
                }
                else
                {
                    return null;
                }
                return currUser;
            }
        }

        public async Task<ApplicationUser?> GetUserByTokenAsync(string token)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                IDbCommand getSetting = conn.CreateCommand();
                getSetting.CommandText = "CALL GET_SETTING('MAX DAYS BEFORE LOCKED')";
                int maxDaysBeforeLocked = Convert.ToInt32(getSetting.ExecuteScalar());

                ApplicationUser currUser = new ApplicationUser();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL GET_USER_BY_TOKEN('{token}')";

                IDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    currUser.Name = reader.GetString(0);
                    currUser.Email = reader.GetString(1);
                    currUser.FailedAttempts = reader.GetInt32(3);
                    currUser.IsNew = reader.GetBoolean(5);
                    currUser.LastReset = reader.GetDateTime(4);

                    // check if exceeded maximum number of days without password reset
                    int daysSinceLastChange = (DateTime.Now - currUser.LastReset).Days;
                    currUser.IsLocked = daysSinceLastChange >= maxDaysBeforeLocked ? true : reader.GetBoolean(2);
                }
                else
                {
                    return null;
                }

                return currUser;
            }
        }

        public async Task<string> GetStringFieldByEmail(string email, string field)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"CALL GET_STRING_FIELD(:email, :field)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "field", Value = field, DbType = DbType.String });
                    return command.ExecuteScalar().ToString();
                }
            }
        }

        public async Task ResetPassword(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand(); 
                command.CommandText = $"CALL RESET_PASSWORD(:email, :password, :salt, :reset_date)";
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "password", Value = user.Hash, DbType = DbType.String });
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "salt", Value = Convert.ToBase64String(user.Salt), DbType = DbType.String });
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "reset_date", Value = DateTime.Now, DbType = DbType.DateTime });

                command.ExecuteScalar();
            }
        }

        public async Task UpdateEmail(ApplicationUser emailModel)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL UPDATE_EMAIL(:email, :new_email)";
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = emailModel.Email, DbType = DbType.String });
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "new_email", Value = emailModel.Name, DbType = DbType.String });

                bool isDuplicate = Convert.ToBoolean(command.ExecuteScalar());
                if (isDuplicate)
                {
                    throw new DuplicateNameException("Email already in use for another account.");
                }
            }
        }

        public async Task<string> UpdateAttempts(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand getSetting = conn.CreateCommand();

                getSetting.CommandText = "CALL GET_SETTING('MAX FAILED ATTEMPTS')";
                int maxAttempts = Convert.ToInt32(getSetting.ExecuteScalar());
                int updatedAttempts = user.FailedAttempts + 1;

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL UPDATE_ATTEMPTS(:email, :updated, :max_attempts)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "updated", Value = updatedAttempts, DbType = DbType.Int32 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "max_attempts", Value = maxAttempts, DbType = DbType.Int32 });

                    return command.ExecuteScalar().ToString();
                }
            }
        }

        public async Task UpdateToken(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL UPDATE_TOKEN(:email, :updated)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "updated", Value = user.Token, DbType = DbType.String });

                    command.ExecuteScalar();
                }
            }
        }

        public async void ResetAttempts(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL RESET_ATTEMPTS(:email)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                    command.ExecuteScalar();
                }
            }
        }
        public void LockUser(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL LOCK_USER(:email)";
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });
                command.ExecuteScalar();
            }
        }

        public void UnlockUser(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL UNLOCK_USER(:email)";
                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = user.Email, DbType = DbType.String });

                command.ExecuteScalar();
            }
        }

        public void DeleteUser(string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();

                command.CommandText = $"CALL DELETE_USER(:email)";

                command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = email, DbType = DbType.String });
                Console.WriteLine("command created");
                command.ExecuteScalar();
                Console.WriteLine("command executed");
                command.CommandText = $"DELETE FROM access_control WHERE email = '{email}'";
                command.ExecuteScalar();
            }
        }

        public async Task<bool[]> FetchAccessRights(string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                ApplicationUser currUser = new ApplicationUser();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM access_control WHERE email = '{email}'";
                    using (var reader = command.ExecuteReader())
                    {
                        //new list
                        List<bool> accessRights = new List<bool>(); 
                        while (reader.Read())
                        {
                            //fetch access rights
                            var right = reader.GetBoolean(2);
                            Console.WriteLine("access rights fetched");
                            //append to array
                            accessRights.Add(right);
                        }
                            return accessRights.ToArray();
                    }
                }
            }
        }

        public async Task<string[]> FetchAccessRightsHeadings(string email)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                ApplicationUser currUser = new ApplicationUser();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM access_control WHERE email = '{email}'";
                    using (var reader = command.ExecuteReader())
                    {
                        List<string> headings = new List<string>();
                        while (reader.Read())
                        {
                            //fetch access rights
                            var heading = reader.GetString(1);
                            Console.WriteLine("headings fetched");
                            //append to array
                            headings.Add(heading);
                        }
                        return headings.ToArray();
                    }
                }
            }
        }

        public async Task<string[]> FetchAvailableAccounts()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                ApplicationUser currUser = new ApplicationUser();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM user_information";
                    using (var reader = command.ExecuteReader())
                    {
                        List<string> availableAccounts = new List<string>();
                        while (reader.Read())
                        {
                            //fetch access rights
                            var email = reader.GetString(0);
                            Console.WriteLine("available accounts fetched");
                            //append to array
                            availableAccounts.Add(email);
                            Console.WriteLine(email);
                        }
                        return availableAccounts.ToArray();
                    }
                }
            }
        }
        
        public async Task CopyAccessRights(string email, string[] access_type, bool[] is_accessible)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                        command.CommandText = $"DELETE FROM access_control WHERE email = '{email}'";
                        command.ExecuteScalar();
                    for (int i = 0; i < access_type.Length; i++)
                    {
                        command.CommandText = $"INSERT INTO access_control (email, access_type, is_accessible) VALUES ('{email}', '{access_type[i]}', {is_accessible[i]})";
                        command.ExecuteScalar();
                    }
                }
            }
        }

        public async Task DeleteAccessRights(string email, string access_type)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"DELETE FROM access_control WHERE email = '{email}' AND access_type = '{access_type}'";
                    command.ExecuteScalar();
                    Console.WriteLine("access rights deleted");
                }
            }
        }

        public async Task SaveAccessRights(List<string> parameterList, string[] access_type)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"DELETE FROM access_control WHERE email = '{parameterList[0]}'";
                    command.ExecuteScalar();
                    Console.WriteLine("access rights deleted");
                    for (int i = 1; i < parameterList.Count; i++)
                    {
                        command.CommandText = $"INSERT INTO access_control (email, access_type, is_accessible) VALUES ('{parameterList[0]}', '{access_type[i-1]}', '{parameterList[i]}')";
                        command.ExecuteScalar();
                        Console.WriteLine("access rights saved");
                    }
                }
            }
        }

        public async Task AddAccessRights(string access_type, string description, string[] accounts)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"UPDATE access_model SET description = '{description}' WHERE access_type = '{access_type}'";
                    int rowsAffected = command.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        command.CommandText = $"INSERT INTO access_model (access_type, description) VALUES ('{access_type}', '{description}')";
                        command.ExecuteScalar();
                        for (int i = 0; i < accounts.Length; i++)
                        {
                            command.CommandText = $"INSERT INTO access_control (email, access_type, is_accessible) VALUES ('{accounts[i]}', '{access_type}', false)";
                            command.ExecuteScalar();
                        }
                        Console.WriteLine("access rights added");
                    }
                    else
                    {
                        Console.WriteLine($"Access type '{access_type}' description updated.");
                    }
                }
            }
        }



        /*
         * Ratecard
         */

        /**
         * 1. Creates an empty RC transaction.
         * 2. Creates ratecard (references transactionId)
         * 3. Creates individual charges (references transactionId and ratecardId)
         */
        public async Task<List<string>> CreateRcTransaction(ApplicationUser user, List<RateCard> ratecards)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                //1. Creates an empty RC transaction.
                string transactionId;
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL DEV_RL_DB.HWL_4PL.CREATE_RC_TRANSACTION(:email)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = "123@example.com", DbType = DbType.String });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        transactionId = result.ToString();
                        //return result.ToString();
                    }
                    else
                    {
                        throw new Exception("Failed to create rc transaction!");
                    }
                }

                //2. Creates ratecard (references transactionId)
                List<string> ratecardIds = new List<string>();
                foreach (RateCard ratecard in ratecards)
                {
                    //Validate
                    if (Search(100, 0, ratecard).Count > 0)
                    {
                        continue;
                    }

                    string ratecardId;
                    using (IDbCommand command = conn.CreateCommand())
                    {

                        command.CommandText = @"CALL DEV_RL_DB.HWL_4PL.CREATE_RATECARD(
                        :transaction_id,
	                    :LANE_ID,
	                    :CONTROLLING_CUSTOMER_MATCHCODE,
	                    :CONTROLLING_CUSTOMER_NAME,
	                    :TRANSPORT_MODE,
	                    :FUNC,
	                    :RATE_VALIDITY_FROM,
	                    :RATE_VALIDITY_TO,
	                    :POL_NAME,
	                    :POL_COUNTRY,
	                    :POL_PORT,
	                    :POD_NAME,
	                    :POD_COUNTRY,
	                    :POD_PORT,
	                    :CREDITOR_MATCHCODE,
	                    :CREDITOR_NAME,
	                    :PICKUP_ADDRESS_NAME,
	                    :DELIVERY_ADDRESS_NAME,
	                    :DANGEROUS_GOODS,
	                    :TEMPERATURE_CONTROLLED,
	                    :CONTAINER_MODE,
	                    :CONTAINER_TYPE
                    )";

                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.Guid });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "LANE_ID", Value = ratecard.Lane_ID, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTROLLING_CUSTOMER_MATCHCODE", Value = ratecard.Controlling_Customer_Matchcode, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTROLLING_CUSTOMER_NAME", Value = ratecard.Controlling_Customer_Name, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "TRANSPORT_MODE", Value = ratecard.Transport_Mode, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "FUNC", Value = ratecard.Function, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "RATE_VALIDITY_FROM", Value = ratecard.Rate_Validity_From, DbType = DbType.DateTime });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "RATE_VALIDITY_TO", Value = ratecard.Rate_Validity_To, DbType = DbType.DateTime });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_NAME", Value = ratecard.POL_Name, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_COUNTRY", Value = ratecard.POL_Country, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_PORT", Value = ratecard.POL_Port, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_NAME", Value = ratecard.POD_Name, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_COUNTRY", Value = ratecard.POD_Country, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_PORT", Value = ratecard.POD_Port, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CREDITOR_MATCHCODE", Value = ratecard.Creditor_Matchcode, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CREDITOR_NAME", Value = ratecard.Creditor_Name, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "PICKUP_ADDRESS_NAME", Value = ratecard.Pickup_Address, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "DELIVERY_ADDRESS_NAME", Value = ratecard.Delivery_Address, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "DANGEROUS_GOODS", Value = ratecard.Dangerous_Goods, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "TEMPERATURE_CONTROLLED", Value = ratecard.Temperature_Controlled, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTAINER_MODE", Value = ratecard.Container_Mode, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTAINER_TYPE", Value = ratecard.Container_Type, DbType = DbType.String });

                        var result = command.ExecuteScalar();
                        if (result != DBNull.Value)
                        {
                            ratecardId = result.ToString();
                            ratecardIds.Add(ratecardId);
                            //return result.ToString();
                        }
                        else
                        {
                            throw new Exception("Failed to create ratecard!");
                        }


                    }

                    //3. Creates individual charges (references transactionId and ratecardId)
                    using (IDbCommand command = conn.CreateCommand())
                    {
                        //command.CommandText = @"
                        //    INSERT INTO DEV_RL_DB.HWL_4PL.RATECARD_CHARGES
                        //        VALUES (
                        //            :id,
                        //            :transaction_id,
                        //            :ratecard_id,
                        //            :created_at,
                        //            :charge_description,
                        //            :calculation_base,
                        //            :minimum,
                        //            :os_unit_price,
                        //            :os_currency,
                        //            :unit_price,
                        //            :currency,
                        //            :per_percent,
                        //            :charge_code
                        //    );";

                        StringBuilder sb = new StringBuilder("INSERT INTO DEV_RL_DB.HWL_4PL.RATECARD_CHARGES VALUES");

                        //foreach (Charge charge in ratecard.Charges)
                        for (int i = 0; i < ratecard.Charges.Count; i++)
                        {
                            Charge charge = ratecard.Charges[i];

                            sb.Append(@$"(
                                :id{i},
                                :transaction_id{i},
                                :ratecard_id{i},
                                :created_at{i},
                                :charge_description{i},
                                :calculation_base{i},
                                :minimum{i},
                                :os_unit_price{i},
                                :os_currency{i},
                                :unit_price{i},
                                :currency{i},
                                :per_percent{i},
                                :charge_code{i}
                            )");

                            if (i < ratecard.Charges.Count - 1)
                            {
                                sb.Append(",");
                            }

                            //command.CommandText = @$"CALL DEV_RL_DB.HWL_4PL.CREATE_CHARGE(
                            //    :transaction_id,
                            //    :ratecard_id,
                            //    :charge_description,
                            //    :calculation_base,
                            //    :minimum,
                            //    :os_unit_price,
                            //    :os_currency,
                            //    :unit_price,
                            //    :currency,
                            //    :per_percent,
                            //    :charge_code
                            //)";
                            //command.Parameters.Clear();
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"id{i}", Value = charge.Id, DbType = DbType.Guid });
                                                                                               
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"transaction_id{i}", Value = transactionId, DbType = DbType.Guid });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"ratecard_id{i}", Value = ratecardId, DbType = DbType.Guid });
                                                                                               
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"created_at{i}", Value = DateTime.Now, DbType = DbType.DateTime });
                                                                                               
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"charge_description{i}", Value = charge.Charge_Description, DbType = DbType.String });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"calculation_base{i}", Value = charge.Calculation_Base, DbType = DbType.String });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"minimum{i}", Value = charge.Min, DbType = DbType.Double });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"os_unit_price{i}", Value = charge.OS_Unit_Price, DbType = DbType.Double });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"os_currency{i}", Value = charge.OS_Currency, DbType = DbType.String });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"unit_price{i}", Value = charge.Unit_Price, DbType = DbType.Double });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"currency{i}", Value = charge.Currency, DbType = DbType.String });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"per_percent{i}", Value = charge.Per_Percent, DbType = DbType.Double });
                            command.Parameters.Add(new SnowflakeDbParameter { ParameterName = $"charge_code{i}", Value = charge.Charge_Code, DbType = DbType.String });


                        }
                        command.CommandText = sb.ToString();
                        var result = command.ExecuteScalar();
                        if (result != DBNull.Value)
                        {
                            //chargeIds.Add(result.ToString());
                        }
                        else
                        {
                            throw new Exception("Failed to create charge!");
                        }
                    }
                    Console.WriteLine(ratecards.Count);
                }

                //return transactionId;
                return ratecardIds;

            }
        }

        /**
         * 2. Creates ratecard (references transactionId)
         * 
         */
        public async Task<string> CreateRatecard(RateCard ratecard, string transactionId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @"CALL DEV_RL_DB.HWL_4PL.CREATE_RATECARD(
                        :transaction_id,
	                    :LANE_ID,
	                    :CONTROLLING_CUSTOMER_MATCHCODE,
	                    :CONTROLLING_CUSTOMER_NAME,
	                    :TRANSPORT_MODE,
	                    :FUNC,
	                    :RATE_VALIDITY_FROM,
	                    :RATE_VALIDITY_TO,
	                    :POL_NAME,
	                    :POL_COUNTRY,
	                    :POL_PORT,
	                    :POD_NAME,
	                    :POD_COUNTRY,
	                    :POD_PORT,
	                    :CREDITOR_MATCHCODE,
	                    :CREDITOR_NAME,
	                    :PICKUP_ADDRESS_NAME,
	                    :DELIVERY_ADDRESS_NAME,
	                    :DANGEROUS_GOODS,
	                    :TEMPERATURE_CONTROLLED,
	                    :CONTAINER_MODE,
	                    :CONTAINER_TYPE
                    )";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.Guid });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "LANE_ID", Value = ratecard.Lane_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTROLLING_CUSTOMER_MATCHCODE", Value = ratecard.Controlling_Customer_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTROLLING_CUSTOMER_NAME", Value = ratecard.Controlling_Customer_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "TRANSPORT_MODE", Value = ratecard.Transport_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "FUNC", Value = ratecard.Function, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "RATE_VALIDITY_FROM", Value = ratecard.Rate_Validity_From, DbType = DbType.DateTime });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "RATE_VALIDITY_TO", Value = ratecard.Rate_Validity_To, DbType = DbType.DateTime });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_NAME", Value = ratecard.POL_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_COUNTRY", Value = ratecard.POL_Country, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_PORT", Value = ratecard.POL_Port, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_NAME", Value = ratecard.POD_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_COUNTRY", Value = ratecard.POD_Country, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_PORT", Value = ratecard.POD_Port, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CREDITOR_MATCHCODE", Value = ratecard.Creditor_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CREDITOR_NAME", Value = ratecard.Creditor_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "PICKUP_ADDRESS_NAME", Value = ratecard.Pickup_Address, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "DELIVERY_ADDRESS_NAME", Value = ratecard.Delivery_Address, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "DANGEROUS_GOODS", Value = ratecard.Dangerous_Goods, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "TEMPERATURE_CONTROLLED", Value = ratecard.Temperature_Controlled, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTAINER_MODE", Value = ratecard.Container_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CONTAINER_TYPE", Value = ratecard.Container_Type, DbType = DbType.String });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return result.ToString();
                    }
                    else
                    {
                        throw new Exception("Failed to create ratecard!");
                    }


                }
            }
        }

        /**
         * 3. Creates individual charges (references transactionId and ratecardId)
         * 
         */
        public List<string> CreateCharges(List<Charge> charges, string transactionId, string ratecardId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                List<string> chargeIds = new List<string>();
                conn.Open();
                foreach (Charge charge in charges)
                {

                    using (IDbCommand command = conn.CreateCommand())
                    {

                        command.CommandText = @$"CALL DEV_RL_DB.HWL_4PL.CREATE_CHARGE(
                            :transaction_id,
                            :ratecard_id,
                            :charge_description,
                            :calculation_base,
                            :minimum,
                            :os_unit_price,
                            :os_currency,
                            :unit_price,
                            :currency,
                            :per_percent,
                            :charge_code
                        )";

                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.Guid });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_description", Value = charge.Charge_Description, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "calculation_base", Value = charge.Calculation_Base, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "minimum", Value = charge.Min, DbType = DbType.Double });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "os_unit_price", Value = charge.OS_Unit_Price, DbType = DbType.Double });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "os_currency", Value = charge.OS_Currency, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "unit_price", Value = charge.Unit_Price, DbType = DbType.Double });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "currency", Value = charge.Currency, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "per_percent", Value = charge.Per_Percent, DbType = DbType.Double });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_code", Value = charge.Charge_Code, DbType = DbType.String });

                        var result = command.ExecuteScalar();
                        if (result != DBNull.Value)
                        {
                            chargeIds.Add(result.ToString());
                        }
                        else
                        {
                            throw new Exception("Failed to create charge!");
                        }

                    }

                }

                return chargeIds;

            }
        }

        /**
         * Retrieves ratecard from transaction ID.
         * 
         */
        public List<RateCard> GetRatecardsFromTransactionId(string transactionId, long offset, long limit)
        {
            List<RateCard> ratecards = new List<RateCard> ();

            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                //1. Find total number of rows
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT COUNT(*) FROM DEV_RL_DB.HWL_4PL.RATECARDS;";

                    IDataReader reader = command.ExecuteReader();
                    //Read result
                    while (reader.Read())
                    {
                        //Check if there are any more rows to be read.
                        if (offset >= reader.GetInt64(0))
                        {
                            return ratecards;
                        }
                    }
                }


                //2. Find ratecards
                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARDS
                        WHERE transaction_id ILIKE :transactionId
                        LIMIT :limit
                        OFFSET :offset
                    ;";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transactionId", Value = transactionId, DbType = DbType.Guid });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "limit", Value = limit, DbType = DbType.Int64 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "offset", Value = offset, DbType = DbType.Int64 });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        RateCard rc = new RateCard();
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        rc.Id = Guid.Parse(reader.GetString(reader.GetOrdinal("ID")));
                        rc.Lane_ID = reader.GetString(reader.GetOrdinal("LANE_ID"));
                        rc.Controlling_Customer_Matchcode = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_MATCHCODE"));
                        rc.Controlling_Customer_Name = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_NAME"));
                        rc.Transport_Mode = reader.GetString(reader.GetOrdinal("TRANSPORT_MODE"));
                        rc.Function = reader.GetString(reader.GetOrdinal("FUNC"));
                        rc.Rate_Validity_From = reader.GetDateTime(reader.GetOrdinal("RATE_VALIDITY_FROM"));
                        rc.Rate_Validity_To = reader.GetDateTime(reader.GetOrdinal("RATE_VALIDITY_TO"));
                        rc.POL_Name = reader.GetString(reader.GetOrdinal("POL_NAME"));
                        rc.POL_Country = reader.GetString(reader.GetOrdinal("POL_COUNTRY"));
                        rc.POL_Port = reader.GetString(reader.GetOrdinal("POL_PORT"));
                        rc.POD_Name = reader.GetString(reader.GetOrdinal("POD_NAME"));
                        rc.POD_Country = reader.GetString(reader.GetOrdinal("POD_COUNTRY"));
                        rc.POD_Port = reader.GetString(reader.GetOrdinal("POD_PORT"));
                        rc.Creditor_Matchcode = reader.GetString(reader.GetOrdinal("CREDITOR_MATCHCODE"));
                        rc.Creditor_Name = reader.GetString(reader.GetOrdinal("CREDITOR_NAME"));
                        rc.Pickup_Address = reader.GetString(reader.GetOrdinal("PICKUP_ADDRESS_NAME"));
                        rc.Delivery_Address = reader.GetString(reader.GetOrdinal("DELIVERY_ADDRESS_NAME"));
                        rc.Dangerous_Goods = reader.GetString(reader.GetOrdinal("DANGEROUS_GOODS"));
                        rc.Temperature_Controlled = reader.GetString(reader.GetOrdinal("TEMPERATURE_CONTROLLED"));
                        rc.Container_Mode = reader.GetString(reader.GetOrdinal("CONTAINER_MODE"));
                        rc.Container_Type = reader.GetString(reader.GetOrdinal("CONTAINER_TYPE"));

                        ratecards.Add(rc);

                    }

                }

            }
            return ratecards;

        }

        /**
         * Retrieves charges from ratecard ID
         * 
         */
        public List<Charge> GetChargesFromRatecardId(string ratecardId, long offset=0, long limit=1000)
        {
            List<Charge> charges = new List<Charge>();

            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                //1. Find total number of rows
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT COUNT(*) FROM DEV_RL_DB.HWL_4PL.RATECARD_CHARGES;";

                    IDataReader reader = command.ExecuteReader();
                    //Read result
                    while (reader.Read())
                    {
                        //Check if there are any more rows to be read.
                        if (offset >= reader.GetInt64(0))
                        {
                            return charges;
                        }
                    }
                }

                //2. Find charges
                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARD_CHARGES
                        WHERE ratecard_id ILIKE :ratecardId
                        LIMIT :limit
                        OFFSET :offset
                    ;";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecardId", Value = ratecardId, DbType = DbType.Guid });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "limit", Value = limit, DbType = DbType.Int64 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "offset", Value = offset, DbType = DbType.Int64 });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        Charge charge = new Charge();
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        charge.Id = Guid.Parse(reader.GetString(reader.GetOrdinal("ID")));
                        charge.Charge_Description = reader.GetString(reader.GetOrdinal("CHARGE_DESCRIPTION"));
                        charge.Calculation_Base = reader.GetString(reader.GetOrdinal("CALCULATION_BASE"));
                        charge.Min = reader.GetDecimal(reader.GetOrdinal("MINIMUM"));
                        charge.OS_Unit_Price = reader.GetDecimal(reader.GetOrdinal("OS_UNIT_PRICE"));
                        charge.OS_Currency = reader.GetString(reader.GetOrdinal("OS_CURRENCY"));
                        charge.Unit_Price = reader.GetDecimal(reader.GetOrdinal("UNIT_PRICE"));
                        charge.Currency = reader.GetString(reader.GetOrdinal("CURRENCY"));
                        charge.Per_Percent = reader.GetDecimal(reader.GetOrdinal("PER_PERCENT"));
                        charge.Charge_Code = reader.GetString(reader.GetOrdinal("CHARGE_CODE"));

                        charges.Add(charge);

                    }

                }

            }
            return charges;

        }

        /**
         * Deletes ratecard.
         * 
         */
        public int DeleteRatecard(string ratecardId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $@"CALL DEV_RL_DB.HWL_4PL.DELETE_RATECARD(
                        :ratecard_id
                    )";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return Int32.Parse(result.ToString());
                    }
                    else
                    {
                        throw new Exception($"Failed to delete ratecard (RatecardID: {ratecardId})");
                    }
                }
            }
        }

        public int DeleteCharge(string chargeId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $@"CALL DEV_RL_DB.HWL_4PL.DELETE_CHARGE(
                        :charge_id
                    )";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_id", Value = chargeId, DbType = DbType.Guid });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return Int32.Parse(result.ToString());
                    }
                    else
                    {
                        throw new Exception($"Failed to delete charge (ChargeID: {chargeId})");
                    }
                }
            }
        }

        /**
        * Add charge IDs to 
        * 
        */
        public void UpdateChargeIds(List<string> chargeIds, string ratecardId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                foreach (string chargeId in chargeIds)
                {

                    using (IDbCommand command = conn.CreateCommand())
                    {

                        command.CommandText = @$"CALL DEV_RL_DB.HWL_4PL.UPDATE_CHARGE_IDS(
                            :ratecard_id,
                            :charge_id
                        )";

                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_id", Value = chargeId, DbType = DbType.Guid });

                        int result = command.ExecuteNonQuery();

                    }

                }
            }
        }

        public void UpdateTransaction(string ratecardId, string transactionId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"CALL DEV_RL_DB.HWL_4PL.UPDATE_TRANSACTION(
                        :transaction_id,
                        :ratecard_id
                    )";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.Guid });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });

                    int result = command.ExecuteNonQuery();

                }

            }
        }

        /**
         * Returns a list of ratecard IDs associated with the given transaction ID.
         * 
         */
        public List<string> GetRatecardIds(string transactionId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                List<string> ratecardIds = new List<string>();

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARD_TRANSACTIONS
                        WHERE ID ILIKE :transaction_id;
                    ";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.Guid });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        ratecardIds.AddRange(parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS"))));

                    }


                }

                return ratecardIds;

            }
        }

        /**
         * Returns by ratecardId
         * 
         */
        public RateCard GetRatecard(string ratecardId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                RateCard rc = null;

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARDS
                        WHERE ID ILIKE :ratecard_id;
                    ";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        rc = new RateCard();
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        rc.Id = Guid.Parse(reader.GetString(reader.GetOrdinal("ID")));
                        rc.Lane_ID = reader.GetString(reader.GetOrdinal("LANE_ID"));
                        rc.Controlling_Customer_Matchcode = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_MATCHCODE"));
                        rc.Controlling_Customer_Name = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_NAME"));
                        rc.Transport_Mode = reader.GetString(reader.GetOrdinal("TRANSPORT_MODE"));
                        rc.Function = reader.GetString(reader.GetOrdinal("FUNC"));
                        rc.Rate_Validity_From = reader.GetDateTime(reader.GetOrdinal("RATE_VALIDITY_FROM"));
                        rc.Rate_Validity_To = reader.GetDateTime(reader.GetOrdinal("RATE_VALIDITY_TO"));
                        rc.POL_Name = reader.GetString(reader.GetOrdinal("POL_NAME"));
                        rc.POL_Country = reader.GetString(reader.GetOrdinal("POL_COUNTRY"));
                        rc.POL_Port = reader.GetString(reader.GetOrdinal("POL_PORT"));
                        rc.POD_Name = reader.GetString(reader.GetOrdinal("POD_NAME"));
                        rc.POD_Country = reader.GetString(reader.GetOrdinal("POD_COUNTRY"));
                        rc.POD_Port = reader.GetString(reader.GetOrdinal("POD_PORT"));
                        rc.Creditor_Matchcode = reader.GetString(reader.GetOrdinal("CREDITOR_MATCHCODE"));
                        rc.Creditor_Name = reader.GetString(reader.GetOrdinal("CREDITOR_NAME"));
                        rc.Pickup_Address = reader.GetString(reader.GetOrdinal("PICKUP_ADDRESS_NAME"));
                        rc.Delivery_Address = reader.GetString(reader.GetOrdinal("DELIVERY_ADDRESS_NAME"));
                        rc.Dangerous_Goods = reader.GetString(reader.GetOrdinal("DANGEROUS_GOODS"));
                        rc.Temperature_Controlled = reader.GetString(reader.GetOrdinal("TEMPERATURE_CONTROLLED"));
                        rc.Container_Mode = reader.GetString(reader.GetOrdinal("CONTAINER_MODE"));
                        rc.Container_Type = reader.GetString(reader.GetOrdinal("CONTAINER_TYPE"));

                    }

                }

                return rc;

            }
        }

        /**
         * Gets the charge.
         * 
         */
        public Charge GetCharge(string chargeId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                Charge charge = null;

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARD_CHARGES
                        WHERE ID ILIKE :charge_id;
                    ";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_id", Value = chargeId, DbType = DbType.Guid });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        charge = new Charge();
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        charge.Id = Guid.Parse(reader.GetString(reader.GetOrdinal("ID")));
                        charge.Charge_Description = reader.GetString(reader.GetOrdinal("CHARGE_DESCRIPTION"));
                        charge.Calculation_Base = reader.GetString(reader.GetOrdinal("CALCULATION_BASE"));
                        charge.Min = reader.GetDecimal(reader.GetOrdinal("MINIMUM"));
                        charge.OS_Unit_Price = reader.GetDecimal(reader.GetOrdinal("OS_UNIT_PRICE"));
                        charge.OS_Currency = reader.GetString(reader.GetOrdinal("OS_CURRENCY"));
                        charge.Unit_Price = reader.GetDecimal(reader.GetOrdinal("UNIT_PRICE"));
                        charge.Currency = reader.GetString(reader.GetOrdinal("CURRENCY"));
                        charge.Per_Percent = reader.GetDecimal(reader.GetOrdinal("PER_PERCENT"));
                        charge.Charge_Code = reader.GetString(reader.GetOrdinal("PER_PERCENT"));

                    }

                }


                return charge;
            }
        }


        public List<string> GetChargeIds(string ratecardId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                List<string> chargeIds = new();

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARD_CHARGES
                        WHERE ratecard_id ILIKE :ratecard_id;
                    ";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        chargeIds.Add(reader.GetString(reader.GetOrdinal("ID")));

                    }

                }

                return chargeIds;
            }
        }

        public List<string> Search(
            long limit = 10,
            long offset = 0,
            RateCard formInput = null
        )
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                List<string> ratecardIds = new();

                //1. Find total number of rows
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT COUNT(*) FROM DEV_RL_DB.HWL_4PL.RATECARDS;";

                    IDataReader reader = command.ExecuteReader();
                    //Read result
                    while (reader.Read())
                    {
                        //Check if there are any more rows to be read.
                        if (offset >= reader.GetInt64(0))
                        {
                            return ratecardIds;
                        }
                    }
                }

                //2. Search
                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARDS
                        WHERE LANE_ID ILIKE :Lane_ID
                            AND CONTROLLING_CUSTOMER_MATCHCODE ILIKE :Controlling_Customer_Matchcode
                            AND CONTROLLING_CUSTOMER_NAME ILIKE :Controlling_Customer_Name
                            AND TRANSPORT_MODE ILIKE :Transport_Mode
                            AND FUNC ILIKE :Function
                            AND RATE_VALIDITY_FROM >= :Rate_Validity_From
                            AND RATE_VALIDITY_TO <= :Rate_Validity_To
                            AND POL_NAME ILIKE :POL_Name
                            AND POL_COUNTRY ILIKE :POL_Country
                            AND POL_PORT ILIKE :POL_Port
                            AND POD_NAME ILIKE :POD_Name
                            AND POD_COUNTRY ILIKE :POD_Country
                            AND POD_PORT ILIKE :POD_Port
                            AND CREDITOR_MATCHCODE ILIKE :Creditor_Matchcode
                            AND CREDITOR_NAME ILIKE :Creditor_Name
                            AND PICKUP_ADDRESS_NAME ILIKE :Pickup_Address
                            AND DELIVERY_ADDRESS_NAME ILIKE :Delivery_Address
                            AND DANGEROUS_GOODS ILIKE :Dangerous_Goods
                            AND TEMPERATURE_CONTROLLED ILIKE :Temperature_Controlled
                            AND CONTAINER_MODE ILIKE :Container_Mode
                            AND CONTAINER_TYPE ILIKE :Container_Type

                        LIMIT :limit
                        OFFSET :offset
                        
                    ;";

                    Console.WriteLine(formInput.Rate_Validity_From);
                    Console.WriteLine(formInput.Rate_Validity_To);


                    //command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Lane_ID", Value = formInput.Lane_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Controlling_Customer_Matchcode", Value = formInput.Controlling_Customer_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Controlling_Customer_Name", Value = formInput.Controlling_Customer_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Transport_Mode", Value = formInput.Transport_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Function", Value = formInput.Function, DbType = DbType.String });
                    //command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Rate_Validity_From", Value = (formInput.Rate_Validity_From.Equals(DateTime.MinValue) ? "%" : formInput.Rate_Validity_From.ToString()), DbType = DbType.String });
                    //command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Rate_Validity_To", Value = (formInput.Rate_Validity_To.Equals(DateTime.MinValue) ? "%" : formInput.Rate_Validity_To.ToString()), DbType = DbType.String });

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Rate_Validity_From", Value = formInput.Rate_Validity_From, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Rate_Validity_To", Value = formInput.Rate_Validity_To, DbType = DbType.Date });

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_Name", Value = formInput.POL_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_Country", Value = formInput.POL_Country, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POL_Port", Value = formInput.POL_Port, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_Name", Value = formInput.POD_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_Country", Value = formInput.POD_Country, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "POD_Port", Value = formInput.POD_Port, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Creditor_Matchcode", Value = formInput.Creditor_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Creditor_Name", Value = formInput.Creditor_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Pickup_Address", Value = formInput.Pickup_Address, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Delivery_Address", Value = formInput.Delivery_Address, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Dangerous_Goods", Value = formInput.Dangerous_Goods, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Temperature_Controlled", Value = formInput.Temperature_Controlled, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Mode", Value = formInput.Container_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Type", Value = formInput.Container_Type, DbType = DbType.String });

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "limit", Value = limit, DbType = DbType.Int64 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "offset", Value = offset, DbType = DbType.Int64 });


                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        ratecardIds.Add(reader.GetString(reader.GetOrdinal("ID")));

                    }

                }

                return ratecardIds;
            }
        }

        public string GetRatecardExcelVersion()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                string ver = "";

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARD_CONFIG
                    ";

                    //command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.Guid });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        ver = reader.GetString(reader.GetOrdinal("EXCEL_VERSION"));

                    }

                }

                return ver;
            }
        }

        private static List<string> parseArrayOfUUIDs(string rawString)
        {
            List<string> rawUuids = rawString.Split("\n").ToList();
            List<string> res = new List<string>();

            for (int i = 0; i < rawUuids.Count; i++)
            {

                if (rawUuids[i].Equals("[") || rawUuids[i].Equals("]"))
                {
                    continue;
                }
                res.Add(rawUuids[i].Substring(3, 36));

            }

            return res;
        }
      
        public string InsertShipment(Shipment shipment)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                string uploadMessage = "Error";

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"CALL CREATE_SHIPMENT (:Job_No, :Master_BL_No, :Container_Mode, :Place_Of_Loading_ID, :Place_Of_Loading_Name, :Place_Of_Discharge_ID, " +
                        ":Place_Of_Discharge_Name, :Vessel_Name, :Voyage_No, :ETD_Date, :ETA_Date, :Carrier_Matchcode, :Carrier_Name, :Carrier_Contract_No, :Carrier_Booking_Reference_No, :Inco_Terms, " +
                        ":Controlling_Customer_Name, :Shipper_Name,  :Consignee_Name, :Total_No_Of_Pieces, :Package_Type,  :Total_No_Of_Volume_Weight_MTQ, :Total_No_Of_Gross_Weight_KGM, :Description, :Shipment_Note)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Job_No", Value = shipment.Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Master_BL_No", Value = shipment.Master_BL_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Mode", Value = shipment.Container_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Loading_ID", Value = shipment.Place_Of_Loading_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Loading_Name", Value = shipment.Place_Of_Loading_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Discharge_ID", Value = shipment.Place_Of_Discharge_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Discharge_Name", Value = shipment.Place_Of_Discharge_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Vessel_Name", Value = shipment.Vessel_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Voyage_No", Value = shipment.Voyage_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETD_Date", Value = shipment.ETD_Date, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETA_Date", Value = shipment.ETA_Date, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Matchcode", Value = shipment.Carrier_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Name", Value = shipment.Carrier_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Contract_No", Value = shipment.Carrier_Contract_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Booking_Reference_No", Value = shipment.Carrier_Booking_Reference_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Inco_Terms", Value = shipment.Inco_Terms, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Controlling_Customer_Name", Value = shipment.Controlling_Customer_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipper_Name", Value = shipment.Shipper_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Consignee_Name", Value = shipment.Consignee_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Total_No_Of_Pieces", Value = shipment.Total_No_Of_Pieces, DbType = DbType.Int64 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Package_Type", Value = shipment.Package_Type, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Total_No_Of_Volume_Weight_MTQ", Value = shipment.Total_No_Of_Volume_Weight_MTQ, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Total_No_Of_Gross_Weight_KGM", Value = shipment.Total_No_Of_Gross_Weight_KGM, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Description", Value = shipment.Description, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Note", Value = shipment.Shipment_Note, DbType = DbType.String });

                    uploadMessage = command.ExecuteScalar().ToString();
                }
                return uploadMessage;
            }
        }


        public void UpdateShipment(Shipment shipment)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"CALL UPDATE_SHIPMENT (:Job_No, :Master_BL_No, :Container_Mode, :Place_Of_Loading_ID, :Place_Of_Loading_Name, :Place_Of_Discharge_ID, " +
                        ":Place_Of_Discharge_Name, :Vessel_Name, :Voyage_No, :ETD_Date, :ETA_Date, :Carrier_Matchcode, :Carrier_Name, :Carrier_Contract_No, :Carrier_Booking_Reference_No, :Inco_Terms, " +
                        ":Controlling_Customer_Name, :Shipper_Name,  :Consignee_Name, :Total_No_Of_Pieces, :Package_Type,  :Total_No_Of_Volume_Weight_MTQ, :Total_No_Of_Gross_Weight_KGM, :Description, :Shipment_Note)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Job_No", Value = shipment.Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Master_BL_No", Value = shipment.Master_BL_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Mode", Value = shipment.Container_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Loading_ID", Value = shipment.Place_Of_Loading_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Loading_Name", Value = shipment.Place_Of_Loading_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Discharge_ID", Value = shipment.Place_Of_Discharge_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Discharge_Name", Value = shipment.Place_Of_Discharge_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Vessel_Name", Value = shipment.Vessel_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Voyage_No", Value = shipment.Voyage_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETD_Date", Value = shipment.ETD_Date, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETA_Date", Value = shipment.ETA_Date, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Matchcode", Value = shipment.Carrier_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Name", Value = shipment.Carrier_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Contract_No", Value = shipment.Carrier_Contract_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Booking_Reference_No", Value = shipment.Carrier_Booking_Reference_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Inco_Terms", Value = shipment.Inco_Terms, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Controlling_Customer_Name", Value = shipment.Controlling_Customer_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipper_Name", Value = shipment.Shipper_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Consignee_Name", Value = shipment.Consignee_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Total_No_Of_Pieces", Value = shipment.Total_No_Of_Pieces, DbType = DbType.Int64 });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Package_Type", Value = shipment.Package_Type, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Total_No_Of_Volume_Weight_MTQ", Value = shipment.Total_No_Of_Volume_Weight_MTQ, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Total_No_Of_Gross_Weight_KGM", Value = shipment.Total_No_Of_Gross_Weight_KGM, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Description", Value = shipment.Description, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Note", Value = shipment.Shipment_Note, DbType = DbType.String });

                    command.ExecuteNonQuery();
                }
            }
        }

        public void InsertContainer(Container container)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"CALL CREATE_CONTAINER (:Shipment_Job_No, :Container_No, :Container_Type, :Seal_No_1, :Seal_No_2)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = container.Shipment_Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_No", Value = container.Container_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Type", Value = container.Container_Type, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Seal_No_1", Value = container.Seal_No_1, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Seal_No_2", Value = container.Seal_No_2, DbType = DbType.String });

                    command.ExecuteNonQuery();

                }
            }
        }

        public int DeleteShipment(string Shipment_Job_No)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $@"CALL DEV_RL_DB.HWL_4PL.DELETE_SHIPMENT(
                        :Shipment_Job_No
                    )";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = Shipment_Job_No, DbType = DbType.String });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return Int32.Parse(result.ToString());
                    }
                    else
                    {
                        throw new Exception($"Failed to delete Shipment (Job_No: {Shipment_Job_No})");
                    }
                }
            }
        }

        public int DeleteContainer(string Shipment_Job_No, string Container_No)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $@"CALL DEV_RL_DB.HWL_4PL.DELETE_CONTAINER(:Shipment_Job_No, :Container_No)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = Shipment_Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_No", Value = Container_No, DbType = DbType.String });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return Int32.Parse(result.ToString());
                    }
                    else
                    {
                        throw new Exception($"Failed to delete container (Job_No: {Shipment_Job_No}, Container_No: {Container_No})");
                    }
                }
            }
        }

        public HashSet<string> fetchAllShipments()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                HashSet<string> result = new HashSet<string>();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT JOB_NO FROM DEV_RL_DB.HWL_4PL.SHIPMENT";

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        string Job_No = reader.GetString(reader.GetOrdinal("JOB_NO"));
                        result.Add(Job_No);
                    }
                }
                return result;
            }
        }

        public List<Shipment> fetchShipments(string Job_No, string Master_BL_No, string Place_Of_Loading_Name, string Place_Of_Discharge_Name, string Vessel_Name, string Voyage_No, string Container_No, string Container_Type,
             DateTime ETD_Date_From, DateTime ETD_Date_To, DateTime ETA_Date_From, DateTime ETA_Date_To)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                List<Shipment> result = new List<Shipment>();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT DISTINCT s.* 
                        FROM DEV_RL_DB.HWL_4PL.SHIPMENT s
                        JOIN  DEV_RL_DB.HWL_4PL.SHIPMENT_CONTAINER c ON s.JOB_NO = c.SHIPMENT_JOB_NO
                        WHERE s.JOB_NO ILIKE :Job_No 
                        AND s.MASTER_BL_NO ILIKE :Master_BL_No
                        AND s.PLACE_OF_LOADING_NAME ILIKE :Place_Of_Loading_Name
                        AND s.PLACE_OF_DISCHARGE_NAME ILIKE :Place_Of_Discharge_Name 
                        AND s.VESSEL_NAME ILIKE :Vessel_Name
                        AND s.VOYAGE_NO ILIKE :Voyage_No
                        AND c.CONTAINER_NO ILIKE :Container_No
                        AND c.CONTAINER_TYPE ILIKE :Container_Type
                        AND :ETD_Date_From <= s.ETD_DATE
                        AND s.ETD_Date <= :ETD_Date_To
                        AND :ETA_Date_From <= s.ETA_Date
                        AND s.ETA_Date <= :ETA_Date_To";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Job_No", Value = Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Master_BL_No", Value = Master_BL_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Loading_Name", Value = Place_Of_Loading_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Discharge_Name", Value = Place_Of_Discharge_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Vessel_Name", Value = Vessel_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Voyage_No", Value = Voyage_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_No", Value = Container_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Type", Value = Container_Type, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETD_Date_From", Value = ETD_Date_From, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETD_Date_To", Value = ETD_Date_To, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETA_Date_From", Value = ETA_Date_From, DbType = DbType.Date });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETA_Date_To", Value = ETA_Date_To, DbType = DbType.Date });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        Shipment s = new Shipment();
                        s.Job_No = reader.GetString(reader.GetOrdinal("JOB_NO"));
                        s.Master_BL_No = reader.GetString(reader.GetOrdinal("MASTER_BL_NO"));
                        s.Container_Mode = reader.GetString(reader.GetOrdinal("CONTAINER_MODE"));
                        s.Place_Of_Loading_ID = reader.GetString(reader.GetOrdinal("PLACE_OF_LOADING_ID"));
                        s.Place_Of_Loading_Name = reader.GetString(reader.GetOrdinal("PLACE_OF_LOADING_ID"));
                        s.Place_Of_Discharge_ID = reader.GetString(reader.GetOrdinal("PLACE_OF_DISCHARGE_ID"));
                        s.Place_Of_Discharge_Name = reader.GetString(reader.GetOrdinal("PLACE_OF_DISCHARGE_NAME"));
                        s.Vessel_Name = reader.GetString(reader.GetOrdinal("VESSEL_NAME"));
                        s.Voyage_No = reader.GetString(reader.GetOrdinal("VOYAGE_NO"));
                        s.ETD_Date = reader.GetDateTime(reader.GetOrdinal("ETD_DATE"));
                        s.ETA_Date = reader.GetDateTime(reader.GetOrdinal("ETA_DATE"));
                        s.Carrier_Matchcode = reader.GetString(reader.GetOrdinal("CARRIER_MATCHCODE"));
                        s.Carrier_Name = reader.GetString(reader.GetOrdinal("CARRIER_NAME"));
                        s.Carrier_Contract_No = reader.GetString(reader.GetOrdinal("CARRIER_CONTRACT_NO"));
                        s.Carrier_Booking_Reference_No = reader.GetString(reader.GetOrdinal("CARRIER_BOOKING_REFERENCE_NO"));
                        s.Inco_Terms = reader.GetString(reader.GetOrdinal("INCO_TERMS"));
                        s.Controlling_Customer_Name = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_NAME"));
                        s.Shipper_Name = reader.GetString(reader.GetOrdinal("SHIPPER_NAME"));
                        s.Consignee_Name = reader.GetString(reader.GetOrdinal("CONSIGNEE_NAME"));
                        s.Total_No_Of_Pieces = reader.GetInt16(reader.GetOrdinal("TOTAL_NO_OF_PIECES"));
                        s.Package_Type = reader.GetString(reader.GetOrdinal("PACKAGE_TYPE"));
                        s.Total_No_Of_Volume_Weight_MTQ = reader.GetDouble(reader.GetOrdinal("TOTAL_NO_OF_VOLUME_WEIGHT_MTQ"));
                        s.Total_No_Of_Gross_Weight_KGM = reader.GetDouble(reader.GetOrdinal("TOTAL_NO_OF_GROSS_WEIGHT_KGM"));
                        s.Description = reader.GetString(reader.GetOrdinal("DESCRIPTION"));
                        s.Shipment_Note = reader.GetString(reader.GetOrdinal("SHIPMENT_NOTE"));

                        result.Add(s);
                    }
                }
                return result;
            }
        }

        public Shipment fetchShipment(string Shipment_Job_No)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                Shipment s = new();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.SHIPMENT WHERE JOB_NO = '{Shipment_Job_No}'";
                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        s.Job_No = reader.GetString(reader.GetOrdinal("JOB_NO"));
                        s.Master_BL_No = reader.GetString(reader.GetOrdinal("MASTER_BL_NO"));
                        s.Container_Mode = reader.GetString(reader.GetOrdinal("CONTAINER_MODE"));
                        s.Place_Of_Loading_ID = reader.GetString(reader.GetOrdinal("PLACE_OF_LOADING_ID"));
                        s.Place_Of_Loading_Name = reader.GetString(reader.GetOrdinal("PLACE_OF_LOADING_ID"));
                        s.Place_Of_Discharge_ID = reader.GetString(reader.GetOrdinal("PLACE_OF_DISCHARGE_ID"));
                        s.Place_Of_Discharge_Name = reader.GetString(reader.GetOrdinal("PLACE_OF_DISCHARGE_NAME"));
                        s.Vessel_Name = reader.GetString(reader.GetOrdinal("VESSEL_NAME"));
                        s.Voyage_No = reader.GetString(reader.GetOrdinal("VOYAGE_NO"));
                        s.ETD_Date = reader.GetDateTime(reader.GetOrdinal("ETD_DATE"));
                        s.ETA_Date = reader.GetDateTime(reader.GetOrdinal("ETA_DATE"));
                        s.Carrier_Matchcode = reader.GetString(reader.GetOrdinal("CARRIER_MATCHCODE"));
                        s.Carrier_Name = reader.GetString(reader.GetOrdinal("CARRIER_NAME"));
                        s.Carrier_Contract_No = reader.GetString(reader.GetOrdinal("CARRIER_CONTRACT_NO"));
                        s.Carrier_Booking_Reference_No = reader.GetString(reader.GetOrdinal("CARRIER_BOOKING_REFERENCE_NO"));
                        s.Inco_Terms = reader.GetString(reader.GetOrdinal("INCO_TERMS"));
                        s.Controlling_Customer_Name = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_NAME"));
                        s.Shipper_Name = reader.GetString(reader.GetOrdinal("SHIPPER_NAME"));
                        s.Consignee_Name = reader.GetString(reader.GetOrdinal("CONSIGNEE_NAME"));
                        s.Total_No_Of_Pieces = reader.GetInt16(reader.GetOrdinal("TOTAL_NO_OF_PIECES"));
                        s.Package_Type = reader.GetString(reader.GetOrdinal("PACKAGE_TYPE"));
                        s.Total_No_Of_Volume_Weight_MTQ = reader.GetDouble(reader.GetOrdinal("TOTAL_NO_OF_VOLUME_WEIGHT_MTQ"));
                        s.Total_No_Of_Gross_Weight_KGM = reader.GetDouble(reader.GetOrdinal("TOTAL_NO_OF_GROSS_WEIGHT_KGM"));
                        s.Description = reader.GetString(reader.GetOrdinal("DESCRIPTION"));
                        s.Shipment_Note = reader.GetString(reader.GetOrdinal("SHIPMENT_NOTE"));
                    }
                }
                return s;
            }
        }

        public List<Container> fetchContainers(string Shipment_Job_No)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                List<Container> result = new List<Container>();
                using (IDbCommand command = conn.CreateCommand())
                {
                    //command.CommandText = "CALL GET_CONTAINER (:Shipment_Job_No)";
                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.SHIPMENT_CONTAINER WHERE SHIPMENT_JOB_NO = :Shipment_Job_No";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = Shipment_Job_No, DbType = DbType.String });
                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        Container c = new Container();
                        c.Id = reader.GetString(reader.GetOrdinal("ID"));
                        c.Shipment_Job_No = reader.GetString(reader.GetOrdinal("SHIPMENT_JOB_NO"));
                        c.Container_No = reader.GetString(reader.GetOrdinal("CONTAINER_NO"));
                        c.Container_Type = reader.GetString(reader.GetOrdinal("CONTAINER_TYPE"));
                        c.Seal_No_1 = reader.GetString(reader.GetOrdinal("SEAL_NO_1"));
                        c.Seal_No_2 = reader.GetString(reader.GetOrdinal("SEAL_NO_2"));

                        result.Add(c);
                    }
                }
                return result;
            }
        }


        public List<RateCard> GetShipmentRatecards(Shipment s)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                List<RateCard> ratecards = new List<RateCard>();

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARDS
                        WHERE POL_PORT ILIKE :Place_Of_Loading_ID
                        AND POD_PORT ILIKE :Place_Of_Discharge_ID
                        AND CREDITOR_MATCHCODE ILIKE :Carrier_Matchcode
                        AND CONTAINER_MODE ILIKE :Container_Mode
                        AND :ETD >= RATE_VALIDITY_FROM
                        AND :ETD <= RATE_VALIDITY_TO
                    ";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Loading_ID", Value = s.Place_Of_Loading_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Place_Of_Discharge_ID", Value = s.Place_Of_Discharge_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Carrier_Matchcode", Value = s.Carrier_Matchcode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Mode", Value = s.Container_Mode, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ETD", Value = s.ETD_Date, DbType = DbType.Date });

                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        RateCard rc = new RateCard();

                        rc.Id = Guid.Parse(reader.GetString(reader.GetOrdinal("ID")));
                        rc.Lane_ID = reader.GetString(reader.GetOrdinal("LANE_ID"));
                        rc.Controlling_Customer_Matchcode = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_MATCHCODE"));
                        rc.Controlling_Customer_Name = reader.GetString(reader.GetOrdinal("CONTROLLING_CUSTOMER_NAME"));
                        rc.Transport_Mode = reader.GetString(reader.GetOrdinal("TRANSPORT_MODE"));
                        rc.Function = reader.GetString(reader.GetOrdinal("FUNC"));
                        rc.Rate_Validity_From = reader.GetDateTime(reader.GetOrdinal("RATE_VALIDITY_FROM"));
                        rc.Rate_Validity_To = reader.GetDateTime(reader.GetOrdinal("RATE_VALIDITY_TO"));
                        rc.POL_Name = reader.GetString(reader.GetOrdinal("POL_NAME"));
                        rc.POL_Country = reader.GetString(reader.GetOrdinal("POL_COUNTRY"));
                        rc.POL_Port = reader.GetString(reader.GetOrdinal("POL_PORT"));
                        rc.POD_Name = reader.GetString(reader.GetOrdinal("POD_NAME"));
                        rc.POD_Country = reader.GetString(reader.GetOrdinal("POD_COUNTRY"));
                        rc.POD_Port = reader.GetString(reader.GetOrdinal("POD_PORT"));
                        rc.Creditor_Matchcode = reader.GetString(reader.GetOrdinal("CREDITOR_MATCHCODE"));
                        rc.Creditor_Name = reader.GetString(reader.GetOrdinal("CREDITOR_NAME"));
                        rc.Pickup_Address = reader.GetString(reader.GetOrdinal("PICKUP_ADDRESS_NAME"));
                        rc.Delivery_Address = reader.GetString(reader.GetOrdinal("DELIVERY_ADDRESS_NAME"));
                        rc.Dangerous_Goods = reader.GetString(reader.GetOrdinal("DANGEROUS_GOODS"));
                        rc.Temperature_Controlled = reader.GetString(reader.GetOrdinal("TEMPERATURE_CONTROLLED"));
                        rc.Container_Mode = reader.GetString(reader.GetOrdinal("CONTAINER_MODE"));
                        rc.Container_Type = reader.GetString(reader.GetOrdinal("CONTAINER_TYPE"));

                        ratecards.Add(rc);

                    }
                }

                return ratecards;

            }
        }

        public void InsertShipmentCharge(ShipmentCharge sc)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"CALL CREATE_SHIPMENT_CHARGE (:SHIPMENT_JOB_NO, :CHARGE_CODE, :CHARGE_NAME, :CREDITOR_NAME, :OS_CHARGE_CURRENCY, :CHARGE_CURRENCY, :CHARGE_EX_RATE, 
                        :VAT_CODE, :CHARGE_EST_COST_NET_OS_AMOUNT, :CHARGE_EST_COST_NET_AMOUNT, :LANE_ID, :REMARKS)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "SHIPMENT_JOB_NO", Value = sc.Shipment_Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CHARGE_CODE", Value = sc.Charge_Code, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CHARGE_NAME", Value = sc.Charge_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CREDITOR_NAME", Value = sc.Creditor_Name, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "OS_CHARGE_CURRENCY", Value = sc.OS_Charge_Currency, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CHARGE_CURRENCY", Value = sc.Charge_Currency, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CHARGE_EX_RATE", Value = sc.Charge_Ex_Rate, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "VAT_CODE", Value = sc.VAT_Code, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CHARGE_EST_COST_NET_OS_AMOUNT", Value = sc.Charge_Est_Cost_Net_OS_Amount, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "CHARGE_EST_COST_NET_AMOUNT", Value = sc.Charge_Est_Cost_Net_Amount, DbType = DbType.Double });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "LANE_ID", Value = sc.Lane_ID, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "REMARKS", Value = sc.Remarks, DbType = DbType.String });

                    command.ExecuteNonQuery();

                }
            }

        }

        public List<ShipmentCharge> fetchShipmentCharges(string Shipment_Job_No)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                List<ShipmentCharge> result = new List<ShipmentCharge>();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.SHIPMENT_ACCRUAL_COST WHERE SHIPMENT_JOB_NO = :Shipment_Job_No";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = Shipment_Job_No, DbType = DbType.String });
                    IDataReader reader = command.ExecuteReader();

                    //Read result
                    while (reader.Read())
                    {
                        ShipmentCharge sc = new ShipmentCharge();
                        sc.Shipment_Job_No = reader.GetString(reader.GetOrdinal("SHIPMENT_JOB_NO"));
                        sc.Charge_Code = reader.GetString(reader.GetOrdinal("CHARGE_CODE"));
                        sc.Charge_Name = reader.GetString(reader.GetOrdinal("CHARGE_NAME"));
                        sc.Creditor_Name = reader.GetString(reader.GetOrdinal("CREDITOR_NAME"));
                        sc.OS_Charge_Currency = reader.GetString(reader.GetOrdinal("OS_CHARGE_CURRENCY"));
                        sc.Charge_Currency = reader.GetString(reader.GetOrdinal("CHARGE_CURRENCY"));
                        sc.Charge_Ex_Rate = reader.GetDouble(reader.GetOrdinal("CHARGE_EX_RATE"));
                        sc.VAT_Code = reader.GetString(reader.GetOrdinal("VAT_CODE"));
                        sc.Charge_Est_Cost_Net_OS_Amount = reader.GetDecimal(reader.GetOrdinal("CHARGE_EST_COST_NET_OS_AMOUNT"));
                        sc.Charge_Est_Cost_Net_Amount = reader.GetDecimal(reader.GetOrdinal("CHARGE_EST_COST_NET_AMOUNT"));
                        sc.Lane_ID = reader.GetString(reader.GetOrdinal("LANE_ID"));
                        sc.Remarks = reader.GetString(reader.GetOrdinal("REMARKS"));
                        result.Add(sc);
                    }
                }
                return result;
            }
        }

        public int DeleteShipmentCharge(string Shipment_Job_No, string Charge_Name)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    Console.Write(Charge_Name);
                    command.CommandText = $@"CALL DEV_RL_DB.HWL_4PL.DELETE_SHIPMENT_CHARGE(:Shipment_Job_No, :Charge_Name)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = Shipment_Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Charge_Name", Value = Charge_Name, DbType = DbType.String });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return Int32.Parse(result.ToString());
                    }
                    else
                    {
                        throw new Exception($"Failed to delete shipment charge (Job_No: {Shipment_Job_No}, Container_No: {Charge_Name})");
                    }
                }
            }
        }

    }
    
}