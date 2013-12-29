using System;
using System.Linq;
using System.Linq.Expressions;
using Ext.Core;
using StubDb.ModelStorage;

namespace StubDb
{
    public class ModelBuilder
    {
        private StubContext Context { get; set; }

        public ModelBuilder(StubContext context)
        {
            Context = context;
        }

        public void EntityHasRequiredDependant<TEntity>(Expression<Func<TEntity, object>> dependantExpression)
        {
            var propertyName = Meta.Name(dependantExpression);

            var dependantType =
                EntityTypeManager.GetProperties(typeof (TEntity)).Single(x => x.Name == propertyName).PropertyType;

            this.EntityHasRequiredDependant(typeof(TEntity), dependantType);
                
        }

        public void EntityHasRequiredDependant(Type entityType, Type requiredDependantType)
        {
            Context.CheckIsEntityType(entityType);
            Context.CheckIsEntityType(requiredDependantType);

            var dependancy = new RequiredDependancy()
                {
                    EntityType = entityType.FullName,
                    RequiredDependantType = requiredDependantType.FullName
                };

            Context.RequiredDependancies.Add(dependancy);
        }
    }
}