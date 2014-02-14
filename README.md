StubDb
======

StubDb makes it simple to create stub data access layer for fast prototyping or easy unit testing.

It is using similar principals as Entity Framework Code First, but do not use SQL relational databases for data storage. Instead it is using in memory storage with option to save/load data from file.

As with EF Code First to use it first, we should create class context. Context should inherit from <i>StubContext</i> and expose <i>StubSet</i> properties that represent collections of the specified entities in the context.

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

All entities, which are generic type parameter in StubSet properties of context, will be registered as entities types. Also will be registered entities from their navigation properties.
Navigation properties is term from EF. It is is property of Entity Type, which represents connection of Entity type to some other Entity Type. In case of one-to-one connection, it is property of connected Entity Type. For multiple connections, it is IEnumerable of connected Entity Type. 
StubDb tries to make API as simple as possible, hiding most of implementation details.

Main point of interaction is via StubSet<TEntity> properties of context class.

StubSet<TEntity> implements IStubSet<TEntity>

```C#
public interface IStubSet<TEntity>
{
       void Add(TEntity entity);
       void Update(TEntity entity);
       void Remove(TEntity entity);
       void Remove(int id);
       IQueryable<TEntity> Query(int levelOfDependenciesToLoad);
}
```    

To add data to context we can use Add method in StubSet<TEntity>. It will add entity with all connected entities from navigation properties.

To update entity use Upadte method. It will update entity with its connections, but will not update existing connected entities.

To delete data use Remove method. It will remove entity and will clear its connections to other entities.

To query data use Query method in StubSet<TEntity>. By default, it will get corresponding entities from context with its navigation properties. There is optional levelOfDependenciesToLoad parameter (equals 1 be default), use it if you want to initialize deeper level of dependencies. For example, if you select level 2 it means entities will be loaded with their dependencies, which in turn will be loaded with their dependencies, but not deeper.

There are a few methods in StubContext class. Methods Add, Update, Remove, Query will work the same as corresponding methods in StubSet<TEntity>.

Methods SaveData/LoadData will Save/Load data to the file. This is not well-tested functionality. Contributions are welcome.

SeedData is used to seed data for the context, when it is empty. It gets SeedDataAction as parameter

```C#
       public delegate void SeedDataAction(StubContext context);
```
Simply add seed data using context property of SeedDataAction.
