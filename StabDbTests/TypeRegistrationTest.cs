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
    public class TypeRegistrationTest
    {
        #region Nested classes

        public class ParentContainerFirst
        {
            public int Id { get; set; }
            public string Test { get; set; }
            public ChildFirst ChildFirsts { get; set; }
            public Byte[] DataBytes { get; set; }
        }

        public class ParentContainerSecond
        {
            public int Id { get; set; }
            public List<bool> Bools { get; set; }
            public List<ChildSecond> ChildSeconds { get; set; }
        }

        public class ChildFirst
        {
            public int Id { get; set; }
        }

        public class ChildSecond
        {
            public int Id { get; set; }
        }

        public class TypeRegistractionContext : StubContext
        {
            public StubSet<ParentContainerFirst> FirstParentItems { get; set; }
            public StubSet<ParentContainerSecond> SecondParentItems { get; set; }
        }

        public class EntityWithNavigationProperty
        {
            public int Id { get; set; }

            public EntityWithoutNavigationProperty ConnectedEnity
            {
                get;
                set;
            }            
        }

        public class EntityWithoutNavigationProperty
        {
            public int Id { get; set; }
        }

        public class ConnectionsRegistrationContext: StubContext
        {
            public StubSet<EntityWithNavigationProperty> EntitiesWithNavigationProperty { get; set; }
            public StubSet<EntityWithoutNavigationProperty> EntitiesWithoutNavigationProperty { get; set; }
        }

        #endregion

        [TestMethod]
        public void should_register_types_correctly()
        {
            var context = new TypeRegistractionContext();

            var typeNames = context.Types.Select(x => x.Value.UniqueName).ToList();

            Assert.IsTrue(typeNames.Contains("ParentContainerFirst"));
            Assert.IsTrue(typeNames.Contains("ParentContainerSecond"));
            Assert.IsTrue(typeNames.Contains("ChildFirst"));
            Assert.IsTrue(typeNames.Contains("ChildSecond"));

            Assert.AreEqual(typeNames.Count, 4);
        }

    }
}
