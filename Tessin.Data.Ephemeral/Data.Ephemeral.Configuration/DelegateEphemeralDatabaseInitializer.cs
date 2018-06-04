using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tessin.Data.Ephemeral.Configuration
{
    public class DelegateEphemeralDatabaseInitializer : IEphemeralDatabaseInitializer
    {
        private readonly Action<EphemeralDatabaseContext> _initializer;

        public DelegateEphemeralDatabaseInitializer(Action<EphemeralDatabaseContext> initializer)
        {
            if (initializer == null) throw new ArgumentNullException();
            _initializer = initializer;
        }

        public void Initialize(EphemeralDatabaseContext context)
        {
            _initializer(context);
        }
    }
}
