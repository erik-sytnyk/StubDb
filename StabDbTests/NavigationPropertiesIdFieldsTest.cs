using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;
using System.Linq;

namespace StabDbTests
{
    [TestClass]
    public class NavigationPropertiesIdFieldsTest
    {
        #region Nested classes

        public class Course
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int CourseInstructorId { get; set; }
            public Instructor CourseInstructor { get; set; }
        }

        public class Instructor
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class TestContext : StubContext
        {
            public StubSet<Course> Courses { get; set; }
            public StubSet<Instructor> Instructors { get; set; }
        }

        #endregion

        [TestMethod]
        public void shold_init_navigation_property_id_field()
        {
            var context = new TestContext();

            var yegor = new Instructor() {FirstName = "Yegor", LastName = "Sytnyk"};
            context.Instructors.Add(yegor);

            var olga = new Instructor() {FirstName = "Olga", LastName = "Zabelina"};
            context.Instructors.Add(olga);

            var chemistry = new Course() {Name = "Chemistry", CourseInstructor = olga};
            context.Courses.Add(chemistry);

            var chemistryFromContext = context.Courses.Query().Single(x => x.Name == "Chemistry");

            Assert.AreEqual(chemistryFromContext.CourseInstructorId, olga.Id);
        }

        [TestMethod]
        public void should_use_navigation_id_property_when_saving_entities_if_navigution_property_is_not_set()
        {
            var context = new TestContext();

            var yegor = new Instructor() { FirstName = "Yegor", LastName = "Sytnyk" };
            context.Instructors.Add(yegor);

            var olga = new Instructor() { FirstName = "Olga", LastName = "Zabelina" };
            context.Instructors.Add(olga);

            var chemistry = new Course() { Name = "Chemistry", CourseInstructorId = olga.Id };
            context.Courses.Add(chemistry);

            var chemistryFromContext = context.Courses.Query().Single(x => x.Name == "Chemistry");

            Assert.AreEqual(chemistryFromContext.CourseInstructor.FirstName, olga.FirstName);
            Assert.AreEqual(chemistryFromContext.CourseInstructor.LastName, olga.LastName);
        }
    }
}
