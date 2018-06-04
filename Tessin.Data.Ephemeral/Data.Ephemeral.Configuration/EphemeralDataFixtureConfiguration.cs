using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace Tessin.Data.Ephemeral.Configuration
{
    public class EphemeralDataFixtureConfiguration
    {
        private string _server = string.Empty;
        public string Server { get { return _server; } set { if (_databases.IsReadOnly) { throw new InvalidOperationException("cannot change server connection after configuration has been frozen"); } _server = value; } }

        public EphemeralDataFixtureConfiguration UseSqlServerExpress()
        {
            var cb = new SqlConnectionStringBuilder(_server)
            {
                DataSource = @".\SQLEXPRESS",
                IntegratedSecurity = true,
            };
            Server = cb.ToString();
            return this;
        }

        public EphemeralDataFixtureConfiguration UseSqlServerLocalDB()
        {
            // see https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-2016-express-localdb
            var cb = new SqlConnectionStringBuilder(_server)
            {
                DataSource = @"(LocalDB)\MSSQLLocalDB", // "MSSQLLocalDB" == automatic instance (public)
                IntegratedSecurity = true,
            };
            Server = cb.ToString();
            return this;
        }

        private IList<EphemeralDatabaseRegistration> _databases = new List<EphemeralDatabaseRegistration>();
        public IEnumerable<EphemeralDatabaseRegistration> Databases { get { return _databases; } }

        public EphemeralDataFixtureConfiguration()
        {
            // express allows us to actually inspect the state from managment studio
            // LocalDB is trickier but will work under circumstances that Express wont
#if DEBUG
            this.UseSqlServerExpress();
#else
            this.UseSqlServerLocalDB();
#endif
        }

        public IEphemeralDatabase RegisterDatabase(string name, IEphemeralDatabaseInitializer initializer = null)
        {
            if (_databases.IsReadOnly)
            {
                throw new InvalidOperationException("cannot add database after configuration has been frozen");
            }
            var db = new EphemeralDatabaseRegistration(name, initializer);
            _databases.Add(db);
            return db.CompletionSource;
        }

        public void Freeze()
        {
            if (!_databases.IsReadOnly)
            {
                _databases = _databases.ToList().AsReadOnly();
            }
        }
    }
}
