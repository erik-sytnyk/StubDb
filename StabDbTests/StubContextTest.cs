﻿using System;
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
            var types = StubContext.GetEntityTypes(typeof (TestStubContext));
            Assert.AreEqual(types.Count, 4);
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