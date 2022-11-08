using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _db;
        internal DbSet<T> dbSet;
        public Repository(ApplicationDbContext db)  //db is the DbContext object
        {
            _db = db;                               //db is given a local reference _db

            //_db.ShoppingCarts.Include(u => u.Product).Include(u => u.Product);
            this.dbSet = _db.Set<T>();               //Set<T> references DbSet class in _db.
            //dbSet = (DbSet<T>)dbSet.AsNoTracking();
        }

        public void Add(T entity)
        {
            dbSet.Add(entity);         //dbSet instead of _db
        }                              //_db.Add(entity)

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter=null, string? includeProperties = null)
        {
            IQueryable<T> query = dbSet;  //I guess IQueryable can return specific records out of a 
            if (filter != null)
            {
                query = query.Where(filter);  //query only when filter parameter is passed
            }
  
            if(includeProperties != null) //whole set 
            {
                foreach(var includeProp in includeProperties.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return query.ToList();        
        }

        public T GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = true)
        {                                                                     //null is deafult user can inclue
            IQueryable<T> query;                                       //Category, CoverType or both
            if (tracked)
            {
                query = dbSet;
            }
            else
            {
                query = dbSet.AsNoTracking();
            }

            query = query.Where(filter);    //First the queryable object is set up
                                            //query.FirstOrDefault(filter) does not return Iuyeryable
                                            //Then it is given the record to return.
            if (includeProperties != null) //whole set 
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return query.FirstOrDefault();
        }

        public void Remove(T entity)
        {
            dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbSet.RemoveRange(entities);
        }
    }
}
