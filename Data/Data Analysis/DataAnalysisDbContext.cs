using Snowflake.Data.Client;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace _4PL.Data
{
    public class DataAnalysisDbContext
    {
        private readonly string _connectionString;

        public DataAnalysisDbContext()
        {
            var configuration = new ConfigurationBuilder()
                    .AddUserSecrets<DataAnalysisDbContext>()
                    .Build();

            byte[] encConn = Convert.FromBase64String(configuration["EncAlg:Conn"]);
            byte[] encKey = Convert.FromBase64String(configuration["EncAlg:Key"]);
            byte[] encIV = Convert.FromBase64String(configuration["EncAlg:IV"]);
            _connectionString = DecryptConn(encConn, encKey, encIV);
        }

        private string DecryptConn(byte[] encConn, byte[] encKey, byte[] encIV)
        {
            Aes decAlg = Aes.Create();
            decAlg.Key = encKey;
            decAlg.IV = encIV;
            MemoryStream decryptionStreamBacking = new MemoryStream();
            CryptoStream decrypt = new CryptoStream(
            decryptionStreamBacking, decAlg.CreateDecryptor(), CryptoStreamMode.Write);
            decrypt.Write(encConn, 0, encConn.Length);
            decrypt.Flush();
            decrypt.Close();
            return new UTF8Encoding(false).GetString(decryptionStreamBacking.ToArray());
        }

        public List<DataReport> FetchDataReports()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM DATA_REPORTS";
                    IDataReader reader = command.ExecuteReader();
                    List<DataReport> reports = new();
                    while (reader.Read())
                    {
                        DataReport curr = new()
                        {
                            Title = reader.GetString(0),
                            Link = reader.GetString(1),
                            AccessRightRequired = reader.GetString(2),
                        };
                        reports.Add(curr);
                    }
                    return reports;
                }
            }
        }

        public void AddNewReport(DataReport newReport)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                bool isDuplicate = false;
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL ADD_REPORT(:title, :link, :access_right)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "title", Value = newReport.Title, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "link", Value = newReport.Link, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "access_right", Value = newReport.AccessRightRequired, DbType = DbType.String });

                    isDuplicate = Convert.ToBoolean(command.ExecuteScalar());
                }
                if (isDuplicate)
                {
                    throw new DuplicateNameException("Data report title already exists.");
                }
            }
        }

        public void UpdateReport(DataReport updatedReport)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                bool isDuplicate = false;
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = "CALL UPDATE_REPORT(:title, :new_title, :link, :access_right)";
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "title", Value = updatedReport.Title, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "new_title", Value = updatedReport.UpdatedTitle, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "link", Value = updatedReport.Link, DbType = DbType.String });
                    command.Parameters.Add(new SnowflakeDbParameter { ParameterName = "access_right", Value = updatedReport.AccessRightRequired, DbType = DbType.String });
                }
            }
        }

        public void DeleteReport(string reportTitle)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"CALL DELETE_REPORT('{reportTitle}')";
                    command.ExecuteScalar();
                }
            }
        }
    }
}
