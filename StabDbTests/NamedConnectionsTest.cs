using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class NamedConnectionsTest
    {
        #region Nested classes

        public class Contract
        {
            public int Id { get; set; }
            public Person Employer { get; set; }
            public Person Contractor { get; set; }
        }

        public class Person
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class TestContext: StubContext
        {
            public StubSet<Contract> Contracts { get; set; }
            public StubSet<Person> Persons { get; set; }
        }

        #endregion

        [TestMethod]
        public void should_support_multiple_single_connections_of_the_same_type()
        {
            var context = new TestContext();

            var sergey = new Person() {FirstName = "Sergey", LastName = "Zabiyako"};
            context.Persons.Add(sergey);

            var masha = new Person() { FirstName = "Masha", LastName = "Balabola" };
            context.Persons.Add(masha);

            var contract = new Contract() {Contractor = sergey, Employer = masha};
            context.Contracts.Add(contract);

            var contractFromContext = context.Contracts.Query().First();

            Assert.AreEqual(contractFromContext.Contractor.FirstName, "Sergey");
            Assert.AreEqual(contractFromContext.Employer.FirstName, "Masha");
        }
    }
}
