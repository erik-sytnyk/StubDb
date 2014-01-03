using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    public delegate object GetProperty(object obj);
    public delegate void SetProperty(object obj, object value);

    [TestClass]
    public class ObjectCloningTest: BaseTest
    {
        #region nested classes

        public class MyObject: ICloneable
        {
            public int Number { get; set; }
            public string Text { get; set; }
            public string AnotherText { get; set; }
            public object Clone()
            {
                return this.MemberwiseClone();
            }
        }

        #endregion

        [TestMethod]
        public void should_create_new_objects()
        {
            var obj = new MyObject() {Number = 4, Text = "Hey"};

            var clone = EntityTypeManager.CloneObject(obj);

            Assert.IsNotNull(clone);
        }

        [TestMethod]
        public void should_create_fast()
        {
            var counter = 1000000;

            var obj = new MyObject() { Number = 4, Text = "Hey", AnotherText = "Hi" };

            var firstOperation = MeasureOperationTime(() =>
                {
                    for (int i = 0; i < counter; i++)
                    {
                        var newObj = obj.Clone();
                    }
                });


            var secondOperation = MeasureOperationTime(() =>
                {
                    for (int i = 0; i < counter; i++)
                    {
                        //var clone = ObjectCreator.CloneObject(obj);
                        var clone = EntityTypeManager.CloneObject(obj);
                    }
                });

            Console.WriteLine(String.Format("First operation: {0}", firstOperation));
            Console.WriteLine(String.Format("Second operation: {0}", secondOperation));
        }
    }
}
