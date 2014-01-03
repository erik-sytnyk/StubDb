namespace StabDbTests.TestContext
{
    public class Course
    {
        public int Id { get; set; }            
        public string Name { get; set; }
        public Location Location { get; set; }

        public Instructor Instructor { get; set; }
    }
}