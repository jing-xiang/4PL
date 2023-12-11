using Snowflake.Data.Client;
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