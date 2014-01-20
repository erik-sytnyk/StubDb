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
    public class UpdatingMultipleLevelsOfDependanciesTest
    {
        #region Nested classes

        public class MyEntity
        {
            public int Id { get; set; }
            public MyNestedEntity NestedFirstLevelEntity { get; set; }
        }

        public class MyNestedEntity
        {
            public int Id { get; set; }
            public MyNestedInsideNestedEntity NestedSecondLevelEntity { get; set; }
        }

        public class MyNestedInsideNestedEntity
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
        public void should_not_clear_navigation_properties_when_adding_objects_to_context()
        {
            var context = new TestContext();
            var nestedSecondLevelEntity = new MyNestedInsideNestedEntity() { Text = "MySecret" };
            var nestedFirstLevelEntity = new MyNestedEntity() { NestedSecondLevelEntity = nestedSecondLevelEntity };
            var entity = new MyEntity() { NestedFirstLevelEntity = nestedFirstLevelEntity };

            context.Entities.Add(entity);

            Assert.IsNotNull(entity.NestedFirstLevelEntity);
            Assert.IsNotNull(entity.NestedFirstLevelEntity.NestedSecondLevelEntity);
            Assert.AreEqual(entity.NestedFirstLevelEntity.NestedSecondLevelEntity.Text, "MySecret");
        }
    }
}
