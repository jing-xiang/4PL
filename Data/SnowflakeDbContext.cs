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
                }
                else
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
                        }
                        else
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
                    }
                    else
                    {
                        command.CommandText = $"UPDATE user_information SET is_locked = true WHERE email = {user.Email}";
                    }
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

        public void CallStoredProcedureForShipment(Shipment shipment)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL CREATE_SHIPMENT (:Job_No, :Master_BL_No, :Container_Mode, :Place_Of_Loading_ID, :Place_Of_Loading_Name, :Place_Of_Discharge_ID, " +
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

        public void CallStoredProcedureForContainer(Container container)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();

                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL CREATE_CONTAINER (:Shipment_Job_No, :Container_No, :Container_Type, :Seal_No_1, :Seal_No_2)";

                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Shipment_Job_No", Value = container.Shipment_Job_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_No", Value = container.Container_No, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Container_Type", Value = container.Container_Type, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Seal_No_1", Value = container.Seal_No_1, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "Seal_No_2", Value = container.Seal_No_2, DbType = DbType.String });

                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Shipment> fetchShipment()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                List<Shipment> result = new List<Shipment>();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.SHIPMENT";
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

        public List<Container> fetchContainer()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                List<Container> result = new List<Container>();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = @$"SELECT * FROM DEV_RL_DB.HWL_4PL.SHIPMENT_CONTAINER";
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

    }
}