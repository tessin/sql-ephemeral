using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tessin.Data.Ephemeral.Configuration
{
    public class EphemeralDatabaseCompletionSource : IEphemeralDatabase
    {
        public readonly TaskCompletionSource<string> ConnectionString = new TaskCompletionSource<string>();

        string IEphemeralDatabase.ConnectionString
        {
            get
            {
                if (!ConnectionString.Task.IsCompleted)
                {
                    throw new InvalidOperationException("Ephemeral database not yet available");
                }
                return ConnectionString.Task.Result;
            }
        }
    }
}
