using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Ext.Core;
using StubDb.ModelStorage;

namespace StubDb
{
    public class ModelBuilder
    {
        private StubContext Context { get; set; }

        private List<Tuple<Type, Type>> RequiredDependancies { get; set; }
        
        internal List<Type> IgnoredTypes { get; set; }

        public ModelBuilder(StubContext context)
        {
            Context = context;
            RequiredDependancies = new List<Tuple<Type, Type>>();
            IgnoredTypes = new List<Type>();
        }

        public void EntityHasRequiredDependancy<TEntity>(Expression<Func<TEntity, object>> dependantExpression)
        {
            var propertyName = Meta.Name(dependantExpression);

            var dependantType = EntityTypeManager.GetProperties(typeof(TEntity)).Single(x => x.Name == propertyName).PropertyType;

            RequiredDependancies.Add(new Tuple<Type, Type>(typeof(TEntity), dependantType));               
        }

        public void EntityHasRequiredDependancy(Type dependantType, Type requiredType)
        {
            RequiredDependancies.Add(new Tuple<Type, Type>(dependantType, requiredType));
        }

        public void RegisterRequiredDependancy(Type dependantType, Type requiredType)
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

        public void IgnoreType(Type typeToIgnore)
        {
            this.IgnoredTypes.Add(typeToIgnore);
        }

        public void BeforeRegisteringTypes()
        {

        }

        public void AfterRegisteringTypes()
        {
            foreach (var tuple in RequiredDependancies)
            {
                this.RegisterRequiredDependancy(tuple.Item1, tuple.Item2); 
            }
        }
    }
}