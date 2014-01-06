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

        public void EntityHasRequiredDependancy<TEntity>(Expression<Func<TEntity, object>> dependantExpression)
        {
            var propertyName = Meta.Name(dependantExpression);

            var dependantType = EntityTypeManager.GetProperties(typeof(TEntity)).Single(x => x.Name == propertyName).PropertyType;

            this.EntityHasRequiredDependancy(typeof(TEntity), dependantType);                
        }

        public void EntityHasRequiredDependancy(Type dependantType, Type requiredType)
        {
            var dependantEntityType = Context.GetEntityType(dependantType);
            var requiredEntityType = Context.GetEntityType(requiredType);

            //TODO check that we do not have required curcular referecnes

            var dependancy = new RequiredDependancy()
                {
                    DependantType = dependantEntityType,
                    RequiredType = requiredEntityType
                };

            Context.RequiredDependancies.Add(dependancy);
        }
    }
}