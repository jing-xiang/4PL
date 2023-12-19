using Snowflake.Data.Client;
using Microsoft.EntityFrameworkCore;
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

        public async Task RegisterUser(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                bool isDuplicate = false;

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
                        //new array
                        bool[] accessRights = new bool[16];
                        while (reader.Read())
                        {
                            //fetch access rights
                            var RateCardRead = reader.GetBoolean(2);
                            Console.WriteLine(RateCardRead);
                            Console.WriteLine("access rights fetched");
                            //append to array
                            accessRights.Append(RateCardRead);
                        }
                            
                            return accessRights;
                    }
                }
            }
        }


        /*
         * Ratecard
         */

        /**
         * Creates an empty RC transaction.
         * 
         */
        public string CreateRcTransaction(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL DEV_RL_DB.HWL_4PL.CREATE_RC_TRANSACTION(:email)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "email", Value = "123@example.com", DbType = DbType.String });

                    var result = command.ExecuteScalar();
                    if (result != DBNull.Value)
                    {
                        return result.ToString();
                    }
                    else
                    {
                        throw new Exception("Failed to create rc transaction!");
                    }
                }
            }
        }

        /**
         * Creates individual charges.
         * 
         */
        public List<string> CreateCharges(List<Charge> charges, string transactionId)
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
                            :charge_description,
                            :calculation_base,
                            :minimum,
                            :unit_price,
                            :currency,
                            :per_percent,
                            :charge_code
                        )";

                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_description", Value = charge.Charge_Description, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "calculation_base", Value = charge.Calculation_Base, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "minimum", Value = charge.Min, DbType = DbType.Double });
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
         * Creates ratecard.
         * 
         */
        public string CreateRatecard(RateCard ratecard, string transactionId)
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
	                    :CONTAINER_TYPE,
	                    :LOCAL_CURRENCY
                    )";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.String });
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
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "LOCAL_CURRENCY", Value = ratecard.Local_Currency, DbType = DbType.String });

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

                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.String });
                        command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_id", Value = chargeId, DbType = DbType.String });

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

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.String });

                    int result = command.ExecuteNonQuery();

                }

            }
        }
        public void GetRatecardIds(string transactionId)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {

                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.RATECARD_TRANSACTIONS
                        WHERE ID ILIKE :transaction_id;
                    ";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "transaction_id", Value = transactionId, DbType = DbType.String });

                    IDataReader result = command.ExecuteReader();

                }

            }
        }
    }

}