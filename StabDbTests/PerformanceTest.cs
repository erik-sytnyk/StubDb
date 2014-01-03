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
            Console.WriteLine(initTime);
        }

        public void InitContext(TestStubContext context)
        {
            var counter = 100000;

            var courses = new List<Course>();

            for (int i = 0; i < counter; i++)
            {
                var course = new Course();
                course.Name = "Course_" + i.ToString();
                courses.Add(course);
                context.Courses.Add(course);
            }
        }

    }
}
