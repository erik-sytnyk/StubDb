using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class ObtainingEntityTypesTest
    {
        public class Student
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string Surname { get; set; }
            public Instructor Instructor { get; set; }
            public List<Course> Courses { get; set; }
        }

        public class Instructor
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string Surname { get; set; }

            public List<Course> Courses { get; set; }
        }

        public class Course
        {
            public int Id { get; set; }            
            public string Name { get; set; }
            public Location Location { get; set; }

            public Instructor Instructor { get; set; }
        }

        public class Location
        {
            public string Building { get; set; }
            public string Room { get; set; }
        }

        public class TestStubContext: StubContext
        {
            public StubSet<Student> Students { get; set; }
            public StubSet<Instructor> Instructors { get; set; }
            public StubSet<Course> Courses { get; set; }

            public TestStubContext(): base()
            {
            }
        }

        [TestMethod]
        public void should_obtain_simple_entity_types()
        {
            var types = StubContext.GetEntityTypes(typeof (TestStubContext));
            Assert.AreEqual(types.Count, 4);
        }

        [TestMethod]
        public void should_get_entity_id()
        {
            var context = new TestStubContext();
            var student = new Student() {Id = 5, Surname = "SomeOne"};
            var id = context.GetEntityId(student);
            Assert.AreEqual(id, 5);
        }

        [TestMethod]
        public void should_keep_data_in_sync()
        {
            var context = new TestStubContext();

            var math = new Course() {Name = "Math"};
            context.Courses.Add(math);

            var geography = new Course() {Name = "Geography"};
            context.Courses.Add(geography);

            var literature = new Course() {Name = "Literature"};
            context.Courses.Add(literature);

            var alex = new Instructor() {FirstName = "Alex", Surname = "Bezborodov"};
            
            alex.Courses = new List<Course>();
            alex.Courses.Add(math);
            alex.Courses.Add(geography);

            context.Instructors.Add(alex);
            
            var per = new Instructor() {FirstName = "Per", Surname = "Sudin"};
            context.Instructors.Add(per);
            
            var yegor = new Instructor() {FirstName = "Yegor", Surname = "Sytnyk"};
            context.Instructors.Add(yegor);

            var mathFromContext = context.Courses.Query().FirstOrDefault(x => x.Name == "Math");
            Assert.IsNotNull(mathFromContext.Instructor);

            context.Instructors.Remove(alex);

            mathFromContext = context.Courses.Query().FirstOrDefault(x => x.Name == "Math");
            Assert.IsNull(mathFromContext.Instructor);
        }
    }
}
