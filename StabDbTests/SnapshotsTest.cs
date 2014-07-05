using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StubDb;

namespace StabDbTests
{
    [TestClass]
    public class SnapshotsTest
    {
        #region Nested classes

        public class Student
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string Surname { get; set; }

            public Student(string firstName, string surname)
            {
                FirstName = firstName;
                Surname = surname;
            }
        }

        public class Instructor
        {
            public int Id { get; set; }
            public string FirstName { get; set; }
            public string Surname { get; set; }

            public Instructor(string firstName, string surname)
            {
                Surname = surname;
                FirstName = firstName;
            }
        }

        public class TestStubContext : StubContext
        {
            public StubSet<Student> Students { get; set; }
            public StubSet<Instructor> Instructors { get; set; }

            public TestStubContext()
                : base()
            {
            }
        }

        #endregion

        [TestMethod]
        public void ShouldRestoreFromSnapshot()
        {
            var context = new TestStubContext();

            context.Students.Add(new Student("John", "Snow"));
            context.Students.Add(new Student("Darko", "Amyntas"));

            var edwin = new Instructor("Edwin", "Miles");
            context.Instructors.Add(edwin);

            context.CreateSnapshot();

            edwin.FirstName = "Boo";
            edwin.Surname = "Foo";

            context.Update(edwin);

            context.RestoreFromSnapshot();
            
            var edwinAfter = context.Instructors.Query().First();

            Assert.AreEqual("Edwin", edwinAfter.FirstName);

            var students = context.Students.Query();

            Assert.AreEqual(2, students.Count());
        }
    }
}
