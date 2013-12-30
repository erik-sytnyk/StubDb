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
    public class RequiredDependantsTest
    {
        #region Nested classes

        public class TestContext : StubContext
        {
            public StubSet<Container> Items { get; set; }

            public override void ConfigureModel()
            {
                base.ConfigureModel();

                this.ModelBuilder.EntityHasRequiredDependancy(typeof(SingleDependant), typeof(Container));
            }
        }

        public class Container
        {
            public int Id { get; set; }
            public string Label { get; set; }
            public SingleDependant SingleDependant { get; set; }
            public List<MultiDependant> MultiDependants { get; set; }
        }

        public class SingleDependant
        {
            public int Id { get; set; }
            public string Label { get; set; }
        }

        public class MultiDependant
        {
            public int Id { get; set; }
            public string Label { get; set; }
        }

        #endregion

        [TestMethod]
        public void should_delete_single_required_dependant()
        {
            var context = new TestContext();

            var container = (Container)null;

            container = new Container() {Label = "ContainerFirst"};
            
            context.Items.Add(container);
                
            container = new Container() { Label = "ContainerSecond" };
            container.SingleDependant = new SingleDependant() { Label = "Single_Dependant" };

            context.Items.Add(container);

            Assert.AreEqual(context.Query<SingleDependant>().Count(), 1);

            var containerFromContext = context.Items.Query().Single(x => x.Label == "ContainerSecond");
            context.Remove(containerFromContext);

            Assert.AreEqual(context.Query<SingleDependant>().Count(), 0, "Dependant item was not deleted");
        }

        [TestMethod]
        public void should_delete_multiple_required_dependant()
        {
            var context = new TestContext();

            var container = (Container)null;

            container = new Container() { Label = "ContainerFirst" };

            context.Items.Add(container);

            container = new Container() { Label = "ContainerSecond" };
            container.MultiDependants = new List<MultiDependant>();

            for (int i = 1; i <= 3; i++)
            {
                container.MultiDependants.Add(new MultiDependant(){Label = "Item" + i.ToString()});
            }

            context.Items.Add(container);

            Assert.AreEqual(context.Query<MultiDependant>().Count(), 3);

            var containerFromContext = context.Items.Query().Single(x => x.Label == "ContainerSecond");
            context.Remove(containerFromContext);

            Assert.AreEqual(context.Query<SingleDependant>().Count(), 0, "Dependant item was not deleted");
        }
    }
}
