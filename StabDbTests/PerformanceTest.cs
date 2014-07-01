using System;
using System.Collections;
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
            var initTime = base.MeasureOperationTime(() => context.SeedData(InitContext));
            var queryTime = base.MeasureOperationTime(() => context.Students.Query());
            
            Console.WriteLine(initTime);
            //Assert.IsTrue(initTime.TotalMilliseconds < 160);

            Console.WriteLine(queryTime);
            //Assert.IsTrue(queryTime.TotalMilliseconds < 70);
        }

        public void InitContext(StubContext inputContext)
        {
            var counter = 6000;

            var context = inputContext as TestStubContext;

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

        [TestMethod]
        public void CreateGenericListTest()
        {
            var time = base.MeasureOperationTime(() =>
            {
                var genericListType = typeof(List<>);
                var concreteType = genericListType.MakeGenericType(typeof(string));
                for (int i = 0; i < 1000000; i++)
                {
                    var newList = EntityTypeManager.CreateNew(concreteType);
                }
            });

            Assert.IsNotNull(time);
        }

    }
}
