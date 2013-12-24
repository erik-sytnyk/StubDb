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
        void Remove(object id);
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
            Context.Add<TEntity>(entity);
        }

        public void Update(TEntity entity)
        {
            Context.Update<TEntity>(entity);
        }

        public void Remove(TEntity entity)
        {
            Context.Remove<TEntity>(entity);
        }

        public void Remove(object id)
        {
            throw new NotImplementedException();
            /*
            var entity = new TEntity();
            //TODO set entity id here
            Context.Remove<TEntity>(entity);
             */
        }

        public IQueryable<TEntity> Query()
        {
            return Context.Query<TEntity>();
        }
    }
}
