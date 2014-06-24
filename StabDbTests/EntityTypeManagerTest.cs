﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StabDbTests.TestContext;
using StubDb;

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
            var student = new Student() { Id = 5, Surname = "SomeOne" };
            
            var idProp = EntityTypeManager.GetEntityIdProperty(student.GetType());

            Assert.IsNotNull(idProp);

            var id = idProp.GetValue(student);
            
            Assert.AreEqual(id, 5);
        }

        [TestMethod]
        public void should_check_if_is_entity_enumerable_t()
        {
            Assert.IsTrue(EntityTypeManager.IsEnumerableEntityType(typeof(IEnumerable<MyEntityClass>)));
            Assert.IsFalse(EntityTypeManager.IsEnumerableEntityType(typeof(IEnumerable<int>)));
            Assert.IsFalse(EntityTypeManager.IsEnumerableEntityType(new List<string>().GetType()));
            Assert.IsTrue(EntityTypeManager.IsEnumerableEntityType((new MyEntityClass[0]).GetType()));
            Assert.IsTrue(EntityTypeManager.IsEnumerableEntityType(typeof(MyCollection)));
            Assert.IsTrue(EntityTypeManager.IsEnumerableEntityType(typeof(MyOtherCollection)));
            Assert.IsFalse(EntityTypeManager.IsEnumerableEntityType((new ArrayList()).GetType()));
        }
    }
}
