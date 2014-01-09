using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StabDbTests.TestContext;
using StubDb;
using Ext.Core;

namespace StabDbTests
{
    [TestClass]
    public class EntityTypeManagerTest
    {
        #region Nested classes

        public class MyEntityClass
        {
            public int Id { get; set; }
        }

        public class MyCollection : List<MyEntityClass>
        {
             
        }

        public class MyOtherCollection: IMyEnumerable
        {
            public IEnumerator<MyEntityClass> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public interface IMyEnumerable : IEnumerable<MyEntityClass>
        {
             
        }

        #endregion

        [TestMethod]
        public void should_get_entity_id()
        {
            var context = new TestStubContext();
            var student = new Student() { Id = 5, Surname = "SomeOne" };
            var id = EntityTypeManager.GetEntityId(student);
            Assert.AreEqual(id, 5);
        }

        [TestMethod]
        public void should_check_if_is_enumerable_t()
        {
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(typeof(IEnumerable<MyEntityClass>)));
            Assert.IsFalse(EntityTypeManager.IsEntityTypedEnumerable(typeof(IEnumerable<int>)));
            Assert.IsFalse(EntityTypeManager.IsEntityTypedEnumerable(new List<string>().GetType()));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable((new MyEntityClass[0]).GetType()));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(typeof(MyCollection)));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(typeof(MyOtherCollection)));
            Assert.IsFalse(EntityTypeManager.IsEntityTypedEnumerable((new ArrayList()).GetType()));
        }
    }
}
