using Snowflake.Data.Client;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace _4PL.Data
{
    public class AccessRightsDbContext
    {
        private readonly string _connectionString;

        public AccessRightsDbContext()
        {
            var configuration = new ConfigurationBuilder()
                    .AddUserSecrets<SnowflakeDbContext>()
                    .Build();

            var encryptedConn = configuration["ConnectionStrings:SnowflakeConnection"];
            _connectionString = DecryptConn(encryptedConn);
        }

        private string DecryptConn(string encryptedConn)
        {
            byte[] encryptedBytes = Convert.FromBase64String(encryptedConn);
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, optionalEntropy: null, scope: DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        public async Task<List<string>> FetchAccessRights(string userEmail)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT access_type FROM access_control WHERE email = '{userEmail}'";
                    IDataReader reader = command.ExecuteReader();
                    List<string> accessRights = new();
                    while (reader.Read())
                    {
                        accessRights.Add(reader.GetString(0));
                    }
                    return accessRights;
                }
            }
        }

        public async Task<List<AccessRight>> FetchAccessTypes()
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM ACCESS_MODEL";
                    IDataReader reader = command.ExecuteReader();
                    List<AccessRight> accessModel = new();
                    while (reader.Read())
                    {
                        AccessRight curr = new()
                        {
                            AccessType = reader.GetString(0),
                            Description = reader.GetString(1)
                        };
                        accessModel.Add(curr);
                    }
                    return accessModel;
                }
            }
        }

        public bool CheckIsValidType(string accessType)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL CHECK_DUPLICATE_TYPE('{accessType}')";
                return Convert.ToBoolean(command.ExecuteScalar());
            }
        }

        public void AddNewAccessRight(AccessRight newRight)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL ADD_NEW_ACCESS_RIGHT('{newRight.AccessType}', '{newRight.Description}')";
                command.ExecuteScalar();
            }
        }

        public void UpdateAccessRight(AccessRight updatedRight)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL UPDATE_ACCESS_RIGHT('{updatedRight.AccessType}', '{updatedRight.UpdatedAccessType}', '{updatedRight.UpdatedDescription}')";
                command.ExecuteScalar();
            }
        }

        public void DeleteAccessRight(string accessType)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                IDbCommand command = conn.CreateCommand();
                command.CommandText = $"CALL DELETE_ACCESS_RIGHT('{accessType}')";
                command.ExecuteScalar();
            }
        }

        public void CopyAccessRights(string targetEmail, List<string> rightsToCopy)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"CALL DELETE_ACCESS_RIGHTS('{targetEmail}')";
                    command.ExecuteScalar();

                    if (rightsToCopy.IsNullOrEmpty())
                    {
                        return;
                    }

                    command.CommandText = "INSERT INTO ACCESS_CONTROL (EMAIL, ACCESS_TYPE) VALUES ";
                    List<string> toConcat = new();
                    foreach (string right in rightsToCopy)
                    {
                        toConcat.Add($"('{targetEmail}', '{right}')");
                    }
                    command.CommandText += string.Join(",", toConcat);
                    command.ExecuteScalar();
                }
            }
        }

        public void SaveAccessRights(ApplicationUser user)
        {
            using (SnowflakeDbConnection conn = new SnowflakeDbConnection(_connectionString))
            {
                conn.Open();
                using (IDbCommand command = conn.CreateCommand())
                {
                    command.CommandText = $"CALL DELETE_ACCESS_RIGHTS('{user.Email}')";
                    command.ExecuteScalar();

                    command.CommandText = "INSERT INTO ACCESS_CONTROL (EMAIL, ACCESS_TYPE) VALUES ";
                    List<string> toConcat = new();
                    foreach (AccessRight right in user.AccessRights)
                    {
                        if (right.UpdatedRight)
                        {
                            toConcat.Add($"('{user.Email}', '{right.AccessType}')");
                        }
                    }
                    if (!toConcat.IsNullOrEmpty())
                    {
                        command.CommandText += string.Join(",", toConcat);
                        command.ExecuteScalar();
                    }            
                }
            }
        }
    }
}
