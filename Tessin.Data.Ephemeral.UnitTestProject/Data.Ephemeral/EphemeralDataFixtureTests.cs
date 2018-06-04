using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tessin.Data.Ephemeral.Configuration;

namespace Tessin.Data.Ephemeral
{
    [TestClass]
    public class EphemeralDataFixtureTests
    {
        [TestMethod]
        public void EphemeralDataFixture_TrimTest()
        {
            var t = new EphemeralDataFixtureBuilder();
            var db = t.Configuration.RegisterDatabase("trim test");
            t.SetUp();

            // oops... we forgot to delete the database

            Assert.IsTrue(t.TrimCandidateSet().Contains(db.GetDatabaseName()));

            var t2 = new EphemeralDataFixtureBuilder();
            var db2 = t2.Configuration.RegisterDatabase("trim test 2");
            t2.SetUp();

            Assert.IsFalse(t.TrimCandidateSet().Contains(db.GetDatabaseName()));

            t2.TearDown();

            Assert.IsFalse(t.TrimCandidateSet().Contains(db2.GetDatabaseName()));
        }
    }
}
