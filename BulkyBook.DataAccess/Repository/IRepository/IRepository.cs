using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        ///T - Category for now
        T GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null);
        //T is the return type   Expression is the expression           null is the default
        //in the Controller.cshtml Edit get action method               user can include Category, CoverType, or both
        //that finds a record                                  
        IEnumerable<T> GetAll(string? includeProperties = null);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);

    }
}
