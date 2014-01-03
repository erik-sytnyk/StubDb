using System.Collections.Generic;

namespace StabDbTests.TestContext
{
    public class Student
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public Instructor Instructor { get; set; }
        public List<Course> Courses { get; set; }
    }
}