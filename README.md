StubDb
======

StubDb makes it simple to create stub data access layer for fast prototyping or easy unit testing.

It is using similar principals as Entity Framework Code First, but do not use SQL relational data bases for data storage. Instead it is using in memory storage with option to save/load data from file.

As with EF Code First to use it first we should create class context. Context should inherit from StubContext and expose StubSet properties that represent collections of the specified entities in the context.

For example:

```C#
public class MyStubContext: StubContext 
{ 
       public StubSet<Course> Courses { get; set; } 
       public StubSet<Department> Departments { get; set; } 
       public StubSet<Enrollment> Enrollments { get; set; } 
       public StubSet<Student> Students { get; set; } 
}
```