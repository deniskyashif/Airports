﻿namespace Airports.Data.Repositories
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;

    public interface IRepository<T> 
        where T : class
    {
        IQueryable<T> GetAll();

        IQueryable<T> SearchFor(Expression<Func<T, bool>> conditions);

        T GetById(int id);

        void Add(T entity);

        void Update(T entity);

        void Delete(T entity);
    }
}