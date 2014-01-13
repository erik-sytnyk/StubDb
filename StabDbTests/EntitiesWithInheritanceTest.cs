using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class EntitiesWithInheritanceTest
    {
        #region Nested classes

        public class MyEntityBase
        {
            public MyDependantEntity Dependant { get; set; }
        }

        public class MyEntity: MyEntityBase
        {
            public int Id { get; set; }
        }

        public class MyDependantEntity
        {
            public int Id { get; set; }
            public string Text { get; set; }
        }

        public class TestContext : StubContext
        {
            public StubSet<MyEntity> Entities { get; set; }
        }

        #endregion

        [TestMethod]
        public void should_support_self_referencing_models()
        {
            var context = new TestContext();
            var dependant = new MyDependantEntity() {Text = "MySecret"};
            var entity = new MyEntity() {Dependant = dependant};
            
            context.Entities.Add(entity);

            var entityFromContext = context.Entities.Query().FirstOrDefault();

            Assert.IsNotNull(entityFromContext.Dependant);
            Assert.AreEqual(entityFromContext.Dependant.Text, "MySecret");
        }
    }
}
