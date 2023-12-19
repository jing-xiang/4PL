using Snowflake.Data.Client;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Diagnostics.Contracts;
using Microsoft.Identity.Client;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using _4PL.Components.Account.Pages.Manage;

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

        public async void ResetAttempts(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"UPDATE user_information SET failed_attempts = 0 WHERE email = {user.Email}";
                    command.ExecuteScalar();
                }
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
        

        /*
         * Ratecard
         */

        /**
         * 1. Creates an empty RC transaction.
         * 
         */
        public async Task<string> CreateRcTransaction(ApplicationUser user)
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
	                    :CONTAINER_TYPE,
	                    :LOCAL_CURRENCY
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
         * Creates individual charges (references transactionId and ratecardId)
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
                        rc.Local_Currency = reader.GetString(reader.GetOrdinal("LOCAL_CURRENCY"));

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
        public List<Charge> GetChargesFromRatecardId(string ratecardId, long offset, long limit)
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
                        charge.Unit_Price = reader.GetDecimal(reader.GetOrdinal("UNIT_PRICE"));
                        charge.Currency = reader.GetString(reader.GetOrdinal("CURRENCY"));
                        charge.Per_Percent = reader.GetDecimal(reader.GetOrdinal("PER_PERCENT"));
                        charge.Charge_Code = reader.GetString(reader.GetOrdinal("PER_PERCENT"));

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

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "ratecard_id", Value = ratecardId, DbType = DbType.String });

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

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "charge_id", Value = chargeId, DbType = DbType.String });

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

                RateCard rc = new RateCard();

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
                        rc.Local_Currency = reader.GetString(reader.GetOrdinal("LOCAL_CURRENCY"));

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

                Charge charge = new Charge();

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
                        //Split column of array
                        //Array is represented as string literal. Requires manual parsing
                        //ratecards = parseArrayOfUUIDs(reader.GetString(reader.GetOrdinal("RATECARD_IDS")));
                        charge.Id = Guid.Parse(reader.GetString(reader.GetOrdinal("ID")));
                        charge.Charge_Description = reader.GetString(reader.GetOrdinal("CHARGE_DESCRIPTION"));
                        charge.Calculation_Base = reader.GetString(reader.GetOrdinal("CALCULATION_BASE"));
                        charge.Min = reader.GetDecimal(reader.GetOrdinal("MINIMUM"));
                        charge.Unit_Price = reader.GetDecimal(reader.GetOrdinal("UNIT_PRICE"));
                        charge.Currency = reader.GetString(reader.GetOrdinal("CURRENCY"));
                        charge.Per_Percent = reader.GetDecimal(reader.GetOrdinal("PER_PERCENT"));
                        charge.Charge_Code = reader.GetString(reader.GetOrdinal("PER_PERCENT"));

                    }

                }


                return charge;
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
    }

}