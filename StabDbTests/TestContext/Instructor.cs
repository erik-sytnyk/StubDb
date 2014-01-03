using System.Collections.Generic;

namespace StabDbTests.TestContext
{
    public class Instructor
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }

        public List<Course> Courses { get; set; }
    }
}