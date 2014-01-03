using System.Collections.Generic;
using StubDb;

namespace StabDbTests.TestContext
{
    public class TestStubContext: StubContext
    {
        public StubSet<Student> Students { get; set; }
        public StubSet<Instructor> Instructors { get; set; }
        public StubSet<Course> Courses { get; set; }

        public virtual void Seed()
        {
            var context = this;

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
        }
    }
}