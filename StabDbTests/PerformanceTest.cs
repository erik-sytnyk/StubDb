using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StabDbTests.TestContext;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class PerformanceTest: BaseTest
    {
        [TestMethod]
        public void measure_init_time()
        {
            var context = new TestStubContext();
            var initTime = base.MeasureOperationTime(() => this.InitContext(context));
            var queryTime = base.MeasureOperationTime(() => context.Students.Query());
            Console.WriteLine(initTime);
            Console.WriteLine(queryTime);
        }

        public void InitContext(TestStubContext context)
        {
            var counter = 6000;

            var courses = new List<Course>();
            var students = new List<Student>();

            for (int i = 0; i < counter; i++)
            {
                var course = new Course();
                course.Name = "Course_" + i.ToString();
                courses.Add(course);
                context.Courses.Add(course);
            }

            for (int i = 0; i < counter - 100; i++)
            {
                var student = new Student();
                student.Courses.Add(courses[i]);
                student.FirstName = "Name_" + i.ToString();
                student.Surname = "Surname_" + i.ToString();
                context.Students.Add(student);
                students.Add(student);
            }
        }

    }
}
