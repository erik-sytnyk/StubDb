using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class StubContextTest
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
            public int Id { get; set; }
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

        public class TestStubContextWithRequiredDependancies: TestStubContext
        {
            public override void ConfigureModel()
            {
                base.ConfigureModel();
                
                this.ModelBuilder.EntityHasRequiredDependancy<Course>(x => x.Location);
            }
        }

        [TestMethod]
        public void should_obtain_simple_entity_types()
        {
            var context = new TestStubContext();
            Assert.AreEqual(context.Types.Count, 4);
        }

        [TestMethod]
        public void should_keep_data_in_sync()
        {            
            var context = this.InitializeTestContext(new TestStubContext());

            var mathFromContext = context.Courses.Query().FirstOrDefault(x => x.Name == "Math");
            Assert.IsNotNull(mathFromContext.Instructor);

            var alexInstructor = context.Instructors.Query().SingleOrDefault(x => x.FirstName == "Alex" && x.Surname == "Bezborodov");
            
            context.Instructors.Remove(alexInstructor);

            var alexAfterRemove = context.Instructors.Query().SingleOrDefault(x => x.FirstName == "Alex" && x.Surname == "Bezborodov");
            Assert.IsNull(alexAfterRemove);

            mathFromContext = context.Courses.Query().FirstOrDefault(x => x.Name == "Math");
            Assert.IsNull(mathFromContext.Instructor);            
        }

        [TestMethod]
        public void should_save_and_load_context()
        {
            var context = InitializeTestContext(new TestStubContext());

            context.SaveData();

            context.LoadData();

            Assert.AreEqual(context.Instructors.Query().Count(), 3);
        }

        [TestMethod]
        public void should_save_dependant_types()
        {
            var context = this.InitializeTestContext(new TestStubContext());

            var literature = context.Courses.Query().SingleOrDefault(x => x.Name == "Literature");

            Assert.IsNotNull(literature.Location);
        }

        [TestMethod]
        public void should_update_dependencies()
        {
            var context = this.InitializeTestContext(new TestStubContext());

            var alex = context.Instructors.Query().Single(x => x.Surname == "Bezborodov");
            
            Assert.AreEqual(alex.Courses.Count, 2);

            alex.Courses.RemoveAll(x => x.Name == "Math");

            context.Instructors.Update(alex);

            alex = context.Instructors.Query().Single(x => x.Surname == "Bezborodov");

            Assert.AreEqual(alex.Courses.Count, 1);
        }

        [TestMethod]
        public void should_clear_navigation_properties_if_not_loading_them()
        {
            var context = this.InitializeTestContext(new TestStubContext());

            var math = context.Courses.Query().Single(x => x.Name == "Math");

            math.Location = new Location() {};

            var alex = context.Instructors.Query().Single(x => x.Surname == "Bezborodov");

            var mathFromAlex = alex.Courses.Single(x => x.Name == "Math");

            Assert.IsNull(mathFromAlex.Location);
        }

        [TestMethod]
        public void should_load_more_than_one_level_of_pependencies()
        {
            var context = this.InitializeTestContext(new TestStubContext());

            var math = (Course) null;

            math = context.Courses.Query(1).Single(x => x.Name == "Math");
            Assert.IsNull(math.Instructor.Courses);

            math = context.Courses.Query(2).Single(x => x.Name == "Math");
            Assert.AreNotEqual(math.Instructor.Courses.Count, 0);
        }

        [TestMethod]
        public void should_update_entity()
        {
            var context = this.InitializeTestContext(new TestStubContext());

            var math = context.Courses.Query(1).Single(x => x.Name == "Math");

            math.Name = "Math and Algebra";

            context.Update(math);

            var mathFromContext = context.Courses.Query(1).Single(x => x.Name == "Math and Algebra");

            Assert.AreSame(mathFromContext.Name, "Math and Algebra");
        }

        [TestMethod]
        public void should_add_and_get_by_new_id()
        {
            var context = new TestStubContext();

            var student = new Student() {FirstName = "Yegor", Surname = "Sytnyk"};
            context.Students.Add(student);

            var studentId = student.Id;

            student = context.Students.Query().Single(x => x.Id == studentId);

            Assert.IsNotNull(student);
            Assert.AreEqual(student.FirstName, "Yegor");
        }

        [TestMethod]
        public void should_not_clear_navigation_of_project_which_is_added_to_context()
        {
            var context = new TestStubContext();

            var instructor = new Instructor();
            instructor.FirstName = "Alex";
            instructor.Surname = "Sharamok";

            var course = new Course();
            course.Name = "Math";
            course.Instructor = instructor;

            context.Courses.Add(course);

            Assert.IsNotNull(course.Instructor);            
        }

        [TestMethod]
        public void should_remove_connections_during_update_if_they_set_to_null()
        {
            var context = this.InitializeTestContext(new TestStubContext());

            var alex = context.Instructors.Query().Single(x => x.FirstName == "Alex");

            alex.Courses = null;

            context.Instructors.Update(alex);

            var alexAfterUpdate = context.Instructors.Query().Single(x => x.FirstName == "Alex");

            var literature = context.Courses.Query().Single(x => x.Name == "Literature");

            Assert.IsNotNull(literature.Location);

            literature.Location = null;

            context.Courses.Update(literature);

            var literatureAfterUpdate = context.Courses.Query().Single(x => x.Name == "Literature");

            Assert.IsNull(literatureAfterUpdate.Location);
        }

        private TestStubContext InitializeTestContext(TestStubContext context)
        {
            var math = new Course() { Name = "Math" };
            context.Courses.Add(math);

            var geography = new Course() { Name = "Geography" };
            context.Courses.Add(geography);

            var literature = new Course() { Name = "Literature" };
            literature.Location = new Location() { Building = "Unit 43", Room = "13-3" };

            context.Courses.Add(literature);

            var alex = new Instructor() { FirstName = "Alex", Surname = "Bezborodov" };

            alex.Courses = new List<Course>();
            alex.Courses.Add(math);
            alex.Courses.Add(geography);

            context.Instructors.Add(alex);

            var per = new Instructor() { FirstName = "Per", Surname = "Sudin" };
            context.Instructors.Add(per);

            var yegor = new Instructor() { FirstName = "Yegor", Surname = "Sytnyk" };
            context.Instructors.Add(yegor);

            return context;
        }
    }
}
