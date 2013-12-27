using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace StubDb
{
    //TODO let properties to be of interface type in Context
    public interface IStubSet<TEntity>
    {
        void Add(TEntity entity);
        void Update(TEntity entity);
        void Remove(TEntity entity);
        void Remove(int id);
        IQueryable<TEntity> Query();
    }

    public class StubSet<TEntity>: IStubSet<TEntity>
    {
        private StubContext _context;

        public StubContext Context
        {
            get
            {
                if (_context == null) throw new ApplicationException("Stub context was not initialized");
                return _context;
            }
            set { _context = value; }
        }

        public void Add(TEntity entity)
        {
            Context.Add(entity);
        }

        public void Update(TEntity entity)
        {
            Context.Update(entity);
        }

        public void Remove(TEntity entity)
        {
            Context.Remove(entity);
        }

        public void Remove(int id)
        {
            Context.Remove(typeof(TEntity), id);
        }

        public IQueryable<TEntity> Query()
        {
            return Context.Query<TEntity>();
        }
    }
}
