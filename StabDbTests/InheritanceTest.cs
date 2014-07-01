using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class InheritanceTest
    {
        #region Nested classes

        public abstract class Criterion
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class CriterionDetailTypeA
        {
            public int Id { get; set; }
            public string ValueA { get; set; }
        }

        public class CriterionTypeA: Criterion
        {
            public List<CriterionDetailTypeA> CriterionDetails { get; set; }

            public CriterionTypeA()
            {
                CriterionDetails = new List<CriterionDetailTypeA>();
            }
        }

        public class CriterionDetailTypeB
        {
            public int Id { get; set; }
            public string ValueB { get; set; }
        }

        public class CriterionTypeB : Criterion
        {
            public List<CriterionDetailTypeB> CriterionDetails { get; set; }

            public CriterionTypeB()
            {
                CriterionDetails = new List<CriterionDetailTypeB>();
            }
        }

        public class Session
        {
            public int Id { get; set; }
            public List<Criterion> Criteria { get; set; }
            public Criterion FavoriteCriterion { get; set; }

            public Session()
            {
                Criteria = new List<Criterion>();
            }
        }

        public class TestContext: StubContext
        {
            public StubSet<Criterion> Criteria { get; set; }
            public StubSet<CriterionTypeA> CriteriaA { get; set; }
            public StubSet<CriterionTypeB> CriteriaB { get; set; }
            public StubSet<Session> Sessions { get; set; }
        }        

        #endregion

        private void InitTestData(TestContext context)
        {
            var hilA = new CriterionTypeA()
            {
                Name = "HILA",
                CriterionDetails = new List<CriterionDetailTypeA> 
                {
                    new CriterionDetailTypeA() {ValueA = "HILA_1"}, 
                    new CriterionDetailTypeA() {ValueA = "HILA_2"}
                }
            };

            var hilB = new CriterionTypeA()
            {
                Name = "HILB",
                CriterionDetails = new List<CriterionDetailTypeA> 
                {
                    new CriterionDetailTypeA() {ValueA = "HILB_1"}, 
                    new CriterionDetailTypeA() {ValueA = "HILB_2"}
                }
            };

            context.CriteriaA.Add(hilA);
            context.CriteriaA.Add(hilB);

            var hslA = new CriterionTypeB()
            {
                Name = "HSLA",
                CriterionDetails = new List<CriterionDetailTypeB> 
                {
                    new CriterionDetailTypeB() {ValueB = "HSLA_1"}, 
                    new CriterionDetailTypeB() {ValueB = "HSLA_2"}
                }
            };

            var hslB = new CriterionTypeB()
            {
                Name = "HSLB",
                CriterionDetails = new List<CriterionDetailTypeB> 
                {
                    new CriterionDetailTypeB() {ValueB = "HSLB_1"}, 
                    new CriterionDetailTypeB() {ValueB = "HSLB_2"}
                }
            };

            context.CriteriaB.Add(hslA);
            context.CriteriaB.Add(hslB);

            var session = new Session();
            session.FavoriteCriterion = hilB;

            session.Criteria.Add(hilA);
            session.Criteria.Add(hilB);
            session.Criteria.Add(hslB);

            context.Add(session);
        }

        [TestMethod]
        public void should_get_all_instances_of_base_class()
        {
            var context = new TestContext();

            InitTestData(context);

            var criteria = context.Criteria.Query();

            Assert.AreEqual(4, criteria.Count());
        }

        [TestMethod]
        public void should_add_via_base_type_set()
        {
            var context = new TestContext();

            InitTestData(context);

            context.Criteria.Add(new CriterionTypeA());

            var criteria = context.Criteria.Query().ToList();

            Assert.AreEqual(5, criteria.Count);
        }

        [TestMethod]
        public void should_load_multiple_navigation_property_of_base_type()
        {
            var context = new TestContext();

            InitTestData(context);

            var session = context.Sessions.Query().FirstOrDefault();

            Assert.AreEqual(3, session.Criteria.Count);
        }

        [TestMethod]
        public void should_load_single_navigation_property_of_base_type()
        {
            var context = new TestContext();

            InitTestData(context);

            var session = context.Sessions.Query().FirstOrDefault();

            Assert.IsNotNull(session.FavoriteCriterion);
        }

        [TestMethod]
        public void should_use_base_type_to_track_id()
        {
            var context = new TestContext();

            InitTestData(context);

            var criteria = context.Criteria.Query().ToList();

            Assert.AreEqual(criteria.Count, criteria.Select(x => x.Id).Distinct().Count());
        }
    }
}
