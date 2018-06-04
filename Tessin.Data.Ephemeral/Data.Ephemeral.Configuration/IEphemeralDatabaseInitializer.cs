namespace Tessin.Data.Ephemeral.Configuration
{
    public interface IEphemeralDatabaseInitializer
    {
        void Initialize(EphemeralDatabaseContext context);
    }
}
