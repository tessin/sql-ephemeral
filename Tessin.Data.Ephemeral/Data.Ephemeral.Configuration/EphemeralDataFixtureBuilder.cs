using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Tessin.Data.Ephemeral.Internal;

namespace Tessin.Data.Ephemeral.Configuration
{
    public class EphemeralDataFixtureBuilder
    {
        private static readonly byte[] _salt = new byte[] { 5, 233, 60, 229, 40, 150, 232, 137, 2, 198, 228, 149, 95, 243, 69, 170, 103, 142, 99, 135, 103, 133, 181, 119, 98, 196, 93, 124, 55, 36, 178, 30 };
        private const string _tag = "ephemeral";

        public EphemeralDataFixtureConfiguration Configuration { get; } = new EphemeralDataFixtureConfiguration();

        public void SetUp()
        {
            Configuration.Freeze(); // configuration cannot be modified after this

            Trim(TrimCandidateSet());

            foreach (var db in Configuration.Databases)
            {
                CreateDatabase(db);
            }
        }

        public void TearDown()
        {
            foreach (var db in Configuration.Databases.Reverse())
            {
                var databaseName = db.CompletionSource.GetDatabaseName();
                DropDatabase(databaseName);
            }
        }

        public void CreateDatabase(EphemeralDatabaseRegistration db)
        {
            using (var conn = new SqlConnection(Configuration.Server))
            {
                conn.Open();

                var privateDataDir = GetPrivateDataDir();

                var catalogName = GetDatabaseCatalogName(db.Name);
                var fileName = GetDatabaseFileName(catalogName);
                var dataFileName = Path.ChangeExtension(Path.Combine(privateDataDir, fileName), "mdf");
                var logFileName = Path.ChangeExtension(Path.Combine(privateDataDir, fileName), "log");

                var cmd = new SqlCommand { Connection = conn };
                cmd.CommandText = $@"create database [{SqlHelper.Escape(catalogName, ']')}]
on (
    name = [{SqlHelper.Escape(fileName, ']')}_data],
    filename = '{SqlHelper.Escape(dataFileName, '\'')}',
    size = 50,
    maxsize = 1000,
    filegrowth = 25
)
log on (
    name = [{SqlHelper.Escape(fileName, ']')}_log],
    filename = '{SqlHelper.Escape(logFileName, '\'')}',
    size = 25,
    maxsize = 500,
    filegrowth = 25
)
;
";
                cmd.ExecuteNonQuery();

                conn.ChangeDatabase(catalogName);

                // tag database as "ephemeral", 
                // if an ephemeral database is left in a open state
                // we will atempt to delete it, assuming that we can decrypt 
                // the "ephemeral" property, if we can't, well the database 
                // must have been created some other way and we won't touch it.
                var cmd2 = new SqlCommand { Connection = conn };
                cmd2.CommandType = System.Data.CommandType.StoredProcedure;
                cmd2.CommandText = "sp_addextendedproperty";
                cmd2.Parameters.AddWithValue("@name", _tag);
                cmd2.Parameters.AddWithValue("@value", Encrypt(privateDataDir));
                cmd2.ExecuteNonQuery();

                conn.Close(); // disconnect while initializer is running

                var cb = new SqlConnectionStringBuilder(Configuration.Server);
                cb.InitialCatalog = catalogName;
                var connStr = cb.ToString();
                db.CompletionSource.ConnectionString.SetResult(connStr);

                var initializer = db.Initializer;
                if (initializer != null)
                {
                    using (var context = new EphemeralDatabaseContext(db.CompletionSource))
                    {
                        initializer.Initialize(context);
                    }
                }
            }
        }

        public bool DropDatabase(string databaseName)
        {
            // by design, this method can only be used to drop an ephemeral database

            using (var conn = new SqlConnection(Configuration.Server))
            {
                conn.Open();

                conn.ChangeDatabase(databaseName);

                var cmd2 = new SqlCommand { Connection = conn };
                cmd2.CommandText = "select * from fn_listextendedproperty(@name, default, default, default, default, default, default)";
                cmd2.Parameters.AddWithValue("name", _tag);

                string value = null;
                using (var reader = cmd2.ExecuteReader())
                {
                    var valueOrdinal = reader.GetOrdinal("value");
                    if (reader.Read())
                    {
                        value = reader.GetString(valueOrdinal);
                    }
                }

                conn.Close();

                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }

                string privateDataDir;
                try
                {
                    privateDataDir = Decrypt(value);
                }
                catch (Exception)
                {
                    privateDataDir = null;
                }

                if (string.IsNullOrEmpty(privateDataDir))
                {
                    return false;
                }

                conn.Open();

                var cmd3 = new SqlCommand { Connection = conn };

                cmd3.CommandText = $@"
alter database [{SqlHelper.Escape(databaseName, ']')}]
set single_user with rollback immediate
;

drop database [{SqlHelper.Escape(databaseName, ']')}]
;
";
                cmd3.ExecuteNonQuery();

                conn.Close();

                if (Directory.Exists(privateDataDir))
                {
                    Directory.Delete(privateDataDir, true);
                }

                return true;
            }
        }

        public List<string> TrimCandidateSet()
        {
            var list = new List<string>();

            using (var conn = new SqlConnection(Configuration.Server))
            {
                conn.Open();

                var cmd = new SqlCommand { Connection = conn };
                cmd.CommandText = "select * from sys.databases where owner_sid <> 0x01 and name like @databaseNamePrefix";
                cmd.Parameters.AddWithValue("databaseNamePrefix", _tag + "_%");

                using (var reader = cmd.ExecuteReader())
                {
                    var nameOrdinal = reader.GetOrdinal("name");
                    while (reader.Read())
                    {
                        list.Add(reader.GetString(nameOrdinal));
                    }
                }
            }

            return list;
        }

        public void Trim(List<string> candidateSet)
        {
            // sometimes databases don't get deleted properly
            // this happens when tests are aborted abnormally, 
            // like stopping a debugger session. this code does
            // cleanup for those situations
            foreach (var databaseName in candidateSet)
            {
                DropDatabase(databaseName);
            }
        }

        public string GetDatabaseCatalogName(string name)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_tag))
            {
                sb.Append(_tag);
            }
            if (sb.Length > 0)
            {
                sb.Append('_');
            }
            sb.Append(name);
            if (sb.Length > 0)
            {
                sb.Append('_');
            }
            sb.Append($"{DateTime.UtcNow:o}_{Guid.NewGuid()}");
            return sb.ToString();
        }

        public string GetDatabaseFileName(string databaseName)
        {
            var invalidFileNameChars = Path.GetInvalidFileNameChars();

            var sb = new StringBuilder();

            for (int i = 0; i < databaseName.Length; i++)
            {
                var ch = databaseName[i];
                if (invalidFileNameChars.Contains(ch))
                {
                    sb.Append('_'); // mask
                }
                else
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }

        public string GetPrivateDataDir()
        {
            // Must be "ProgramData" becuase access permissions and SQL Server.
            var programData = Environment.GetEnvironmentVariable("ProgramData");
            var path = Path.Combine(programData, "EphemeralDataFixture", Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
            return path;
        }

        public string Encrypt(string value)
        {
            return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), _salt, DataProtectionScope.LocalMachine));
        }

        public string Decrypt(string value)
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(value), _salt, DataProtectionScope.LocalMachine));
        }
    }
}
