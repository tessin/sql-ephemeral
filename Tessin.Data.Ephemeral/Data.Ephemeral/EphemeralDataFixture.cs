using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tessin.Data.Ephemeral.Configuration;

namespace Tessin.Data.Ephemeral
{
    [TestClass]
    public abstract class EphemeralDataFixture
    {
        private readonly EphemeralDataFixtureBuilder _builder = new EphemeralDataFixtureBuilder();

        protected EphemeralDataFixtureConfiguration Configuration { get { return _builder.Configuration; } }

        [TestInitialize]
        public virtual void SetUpDataFixture()
        {
            _builder.SetUp();
        }

        [TestCleanup]
        public void TearDownDataFixture()
        {
            _builder.TearDown();
        }
    }
}
