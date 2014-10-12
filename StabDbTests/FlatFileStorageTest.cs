using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;
using StubDb.Store;

namespace StabDbTests
{
    [TestClass]
    public class FlatFileStorageTest: BaseTest
    {
        #region Nested classes

        public class Parent
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public List<Child> Children { get; set; }

            public Parent()
            {
                Children = new List<Child>();
            }
        }

        public class Child
        {
            public int Id { get; set; }
            public string Text { get; set; }
            public DateTime Date { get; set; }
        }

        public class TestStubContext : StubContext
        {
            public StubSet<Parent> Parents { get; set; }
            public StubSet<Parent> Children { get; set; }

            public TestStubContext()
                : base()
            {
            }
        }

        #endregion

        [TestMethod]
        public void should_save_and_load_context()
        {
            var context = new TestStubContext {PersistenceProvider = new FlatFilePersistenceProvider()};

            var parentA = new Parent() {Text = "First"};
            parentA.Children.Add(new Child(){Text = "First child", Date = new DateTime(1984, 8, 4)});
            parentA.Children.Add(new Child(){Text = "Second child", Date = new DateTime(1985, 9, 26)});
            context.Parents.Add(parentA);

            var parentB = new Parent() { Text = "Second" };
            parentB.Children.Add(new Child() { Text = "First child", Date = new DateTime(2084, 8, 4) });
            context.Parents.Add(parentB);

            context.SaveData();

            context.LoadData();

            var parents = context.Parents.Query();

            var parentAFromContext = parents.Single(x => x.Text == "First");
            var parentBFromContext = parents.Single(x => x.Text == "Second");

            Assert.AreEqual(parentAFromContext.Children.Count, 2);

            var firstChild = parentAFromContext.Children.Single(x => x.Text == "First child");

            Assert.AreEqual(firstChild.Date, new DateTime(1984, 8, 4));

            Assert.AreEqual(parentBFromContext.Children.Count, 1);
        }

        [Ignore]
        [TestMethod]
        public void should_save_and_load_big_contexts()
        {
            var context = new TestStubContext { PersistenceProvider = new FlatFilePersistenceProvider() };
            context.DoDataConsistencyTest = false;

            var parentsNumber = 50000;

            for (int i = 0; i < parentsNumber; i++)
            {
                var parent = new Parent() { Text = "First" };
                parent.Children.Add(new Child() { Text = "First child", Date = new DateTime(1984, 8, 4) });
                parent.Children.Add(new Child() { Text = "Second child", Date = new DateTime(1985, 9, 26) });
                parent.Children.Add(new Child() { Text = "Third child", Date = new DateTime(1985, 10, 26) });
                parent.Children.Add(new Child() { Text = "Forth child", Date = new DateTime(1986, 10, 26) });
                parent.Children.Add(new Child() { Text = "Fifth child", Date = new DateTime(1986, 10, 20) });
                context.Parents.Add(parent);
            }

            var saveTime = base.MeasureOperationTime(() =>
            {
                context.SaveData();
                context.LoadData();
            });            
           
            Assert.IsTrue(saveTime.TotalSeconds < 5);

            Console.WriteLine(saveTime.TotalSeconds);

            var parents = context.Parents.Query().ToList();

            Assert.AreEqual(parents.Count(), parentsNumber);

        }
    }
}
