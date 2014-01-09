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

        public class MyCollection: List<string>
        {
             
        }

        public class MyOtherCollection: IMyEnumerable
        {
            public IEnumerator<string> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public interface IMyEnumerable: IEnumerable<string>
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
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(typeof(IEnumerable<int>)));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(new List<string>().GetType()));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable((new int[0]).GetType()));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(typeof(MyCollection)));
            Assert.IsTrue(EntityTypeManager.IsEntityTypedEnumerable(typeof(MyOtherCollection)));
            Assert.IsFalse(EntityTypeManager.IsEntityTypedEnumerable((new ArrayList()).GetType()));
        }
    }
}
