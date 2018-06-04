using System;

namespace Tessin.Data.Ephemeral.Configuration
{
    public class EphemeralDatabaseRegistration
    {
        public string Name { get; set; }
        public IEphemeralDatabaseInitializer Initializer { get; }
        public EphemeralDatabaseCompletionSource CompletionSource { get; } = new EphemeralDatabaseCompletionSource();

        public EphemeralDatabaseRegistration(string name, IEphemeralDatabaseInitializer initializer)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("a name is required");
            }
            Name = name;
            Initializer = initializer;
        }
    }
}
