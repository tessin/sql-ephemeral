using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Tessin.Data.Ephemeral
{
    [TestClass]
    public class EphemeralDataFixtureInitTest : EphemeralDataFixture
    {
        public IEphemeralDatabase Database { get; private set; }

        [TestInitialize]
        public override void SetUpDataFixture()
        {
            Database = Configuration.RegisterDatabase("init test", context =>
            {
                context.Execute("create table [init_test] ([id] int primary key, [name] sysname)");
            });

            base.SetUpDataFixture();
        }

        class table
        {
            public string name { get; set; }
        }

        [TestMethod]
        public void InitTest()
        {
            var list = Database.ExecuteQuery<table>("select * from sys.tables").ToList();
            Assert.AreEqual(1, list.Count); // should be non-empty database
            Assert.IsTrue(list.All(x => x.name == "init_test")); // should have exactly init_test table
        }
    }
}
