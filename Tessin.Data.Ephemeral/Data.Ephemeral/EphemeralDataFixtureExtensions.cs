using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Tessin.Data.Ephemeral.Configuration;

namespace Tessin.Data.Ephemeral
{
    public static class EphemeralDataFixtureExtensions
    {
        public static string GetDatabaseName(this IEphemeralDatabase db)
        {
            var cb = new SqlConnectionStringBuilder(db.ConnectionString);
            return cb.InitialCatalog;
        }

        public static IEphemeralDatabase RegisterDatabase(this EphemeralDataFixtureConfiguration configuration, string name, Action<EphemeralDatabaseContext> initializer)
        {
            return configuration.RegisterDatabase(name, new DelegateEphemeralDatabaseInitializer(initializer));
        }

        private static void PrepareCommand(string commandText, Dictionary<string, object> parameters, CommandType commandType, SqlCommand cmd)
        {
            cmd.CommandText = commandText;
            cmd.CommandType = commandType;

            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
            }
        }

        public static int Execute(this EphemeralDatabaseContext context, string commandText, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text)
        {
            var cmd = new SqlCommand { Connection = context.Connection };
            PrepareCommand(commandText, parameters, commandType, cmd);
            return cmd.ExecuteNonQuery();
        }

        public static int Execute(this IEphemeralDatabase db, string commandText, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text)
        {
            using (var conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();

                var cmd = new SqlCommand { Connection = conn };
                PrepareCommand(commandText, parameters, commandType, cmd);
                return cmd.ExecuteNonQuery();
            }
        }

        public static IEnumerable<TRecord> ExecuteQuery<TRecord>(this EphemeralDatabaseContext context, string commandText, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text)
            where TRecord : class, new()
        {
            var cmd = new SqlCommand { Connection = context.Connection };
            PrepareCommand(commandText, parameters, commandType, cmd);
            return ExecuteQuery<TRecord>(cmd);
        }

        public static IEnumerable<TRecord> ExecuteQuery<TRecord>(this IEphemeralDatabase db, string commandText, Dictionary<string, object> parameters = null, CommandType commandType = CommandType.Text)
            where TRecord : class, new()
        {
            using (var conn = new SqlConnection(db.ConnectionString))
            {
                conn.Open();

                var cmd = new SqlCommand { Connection = conn };
                PrepareCommand(commandText, parameters, commandType, cmd);
                foreach (var record in ExecuteQuery<TRecord>(cmd))
                {
                    yield return record;
                }
            }
        }

        private static IEnumerable<TRecord> ExecuteQuery<TRecord>(SqlCommand cmd)
            where TRecord : class, new()
        {
            using (var reader = cmd.ExecuteReader())
            {
                var schemaTable = reader.GetSchemaTable();

                var names = new string[schemaTable.Rows.Count];
                for (int i = 0; i < schemaTable.Rows.Count; i++)
                {
                    var row = schemaTable.Rows[i];
                    names[i] = (string)row["ColumnName"];
                }

                var xs = new SqlDbType[schemaTable.Rows.Count];
                for (int i = 0; i < schemaTable.Rows.Count; i++)
                {
                    var row = schemaTable.Rows[i];
                    xs[i] = (SqlDbType)((int)row["ProviderType"]);
                }

                while (reader.Read())
                {
                    var obj = new JObject();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = names[i];
                        if (reader.IsDBNull(i))
                        {
                            obj.Add(name, JValue.CreateNull());
                            continue;
                        }

                        switch (xs[i])
                        {
                            case SqlDbType.BigInt:
                                {
                                    var value = reader.GetInt64(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.Int:
                                {
                                    var value = reader.GetInt32(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.SmallInt:
                                {
                                    var value = reader.GetInt16(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.TinyInt:
                                {
                                    var value = reader.GetByte(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.Binary:
                            case SqlDbType.VarBinary:
                            case SqlDbType.Timestamp: // todo: test!
                                {
                                    var value = reader.GetSqlBinary(i);
                                    obj.Add(name, new JValue(value.Value));
                                    break;
                                }
                            case SqlDbType.Bit:
                                {
                                    var value = reader.GetBoolean(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.Char:
                            case SqlDbType.NChar:
                            case SqlDbType.VarChar:
                            case SqlDbType.NVarChar:
                                {
                                    var value = reader.GetString(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.Real:
                            case SqlDbType.Float:
                                {
                                    var value = reader.GetDouble(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.SmallDateTime:
                            case SqlDbType.DateTime:
                            case SqlDbType.DateTime2:
                                {
                                    var value = reader.GetDateTime(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.DateTimeOffset:
                                {
                                    var value = reader.GetDateTimeOffset(i);
                                    obj.Add(name, new JValue(value));
                                    break;
                                }
                            case SqlDbType.UniqueIdentifier:
                                {
                                    var value = reader.GetSqlGuid(i);
                                    obj.Add(name, new JValue(value.Value));
                                    break;
                                }
                        }
                    }
                    yield return obj.ToObject<TRecord>();
                }
            }
        }
    }
}
