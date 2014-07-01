using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class IgnoreTest
    {
        #region Nested classes

        public class Address
        {
            public string Street { get; set; }
            public string Town { get; set; }
            public string Country { get; set; }

            public new object MemberwiseClone()
            {
                return base.MemberwiseClone();
            }
        }

        public class User
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public Address Address { get; set; }
            public TempInfo Temp { get; set; }
        }

        public class TempInfo
        {
            public string Temp1 { get; set; } 
            public string Temp2 { get; set; } 
        }

        public class TestContext : StubContext
        {
            public StubSet<User> Users { get; set; }

            public override void ConfigureModel()
            {
                base.ConfigureModel();
                
                this.ModelBuilder.NotEntityType(typeof(Address), true);
                this.ModelBuilder.NotEntityType(typeof(TempInfo), false);
            }
        }

        #endregion

        public void InitContext(TestContext context)
        {
            var yegor = new User()
            {
                FirstName = "Yegor",
                LastName = "Sytnyk",
                Address = new Address()
                {
                    Street = "Uralskaya 9",
                    Town = "Dnepropetrovsk",
                    Country = "Ukraine"
                },
                Temp = new TempInfo()
                {
                    Temp1 = "XXX",
                    Temp2 = "YYY"
                }
                
            };

            var olga = new User()
            {
                FirstName = "Olgz",
                LastName = "Zabelina",
                Address = new Address()
                {
                    Street = "Sholohova 39",
                    Town = "Dnepropetrovsk",
                    Country = "Ukraine"
                },
                Temp = new TempInfo()
                {
                    Temp1 = "XXX",
                    Temp2 = "YYY"
                }
            };

            context.Add(yegor);
            context.Add(olga);
        }

        [TestMethod]
        public void should_ignore_types()
        {
            var context = new TestContext();
            InitContext(context);
        }

        [TestMethod]
        public void should_clone_ignored_types()
        {
            var context = new TestContext();
            InitContext(context);

            var user = context.Users.Query().First();

            user.Address.Country = "USA";

            var userFromContext = context.Users.Query().First();

            Assert.AreNotEqual("USA", userFromContext.Address.Country);
        }

        [TestMethod]
        public void should_allow_overriding_of_ignored_types()
        {
            var context = new TestContext();
            InitContext(context);

            var user = context.Users.Query().First();
            user.Address = new Address() {Country = "UK", Street = "Baker 36", Town = "London"};

            context.Users.Update(user);

            var userAfterUpdate = context.Users.Query().First();

            Assert.AreEqual(userAfterUpdate.Address.Country, "UK");
        }

        [TestMethod]
        public void should_not_store_ignored_types_if_specified()
        {
            var context = new TestContext();
            InitContext(context);

            var user = context.Users.Query().First();

            Assert.IsNull(user.Temp);
        }
    }
}
