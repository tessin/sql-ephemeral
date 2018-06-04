using System;
using System.Data.SqlClient;

namespace Tessin.Data.Ephemeral.Configuration
{
    public class EphemeralDatabaseContext : IDisposable
    {
        public IEphemeralDatabase Database { get; }

        private readonly Lazy<SqlConnection> _conn;
        public SqlConnection Connection { get { return _conn.Value; } }

        public EphemeralDatabaseContext(IEphemeralDatabase database)
        {
            this.Database = database;

            _conn = new Lazy<SqlConnection>(() =>
            {
                var conn = new SqlConnection(this.Database.ConnectionString);
                conn.Open();
                return conn;
            });
        }

        public void Dispose()
        {
            if (_conn.IsValueCreated)
            {
                _conn.Value.Dispose();
            }
        }
    }
}
