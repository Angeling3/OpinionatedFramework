using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IOKode.OpinionatedFramework.Persistence.Queries;
using NHibernate;
using NHibernate.Type;

namespace IOKode.OpinionatedFramework.ContractImplementations.NHibernate;

public class QueryExecutor(ISessionFactory sessionFactory, IQueryExecutorConfiguration configuration) : IQueryExecutor
{
    public async Task<ICollection<TResult>> QueryAsync<TResult>(string query, object? parameters, IDbTransaction? dbTransaction = null, CancellationToken cancellationToken = default)
    {
        using var session = dbTransaction == null
            ? sessionFactory.OpenStatelessSession()
            : sessionFactory.OpenStatelessSession(dbTransaction.Connection as DbConnection);
        
        var sqlQuery = session.CreateSQLQuery(query);
        
        AddScalarsForType<TResult>(sqlQuery);
        sqlQuery.SetResultTransformer(configuration.GetResultTransformer<TResult>());
        
        if (parameters != null)
        {
            AddParameters(sqlQuery, parameters);
        }
        
        var results = await sqlQuery.ListAsync<TResult>(cancellationToken);
        return results;
    }
    
    /// <summary>
    /// Loops through properties of TResult, adding .AddScalar for each
    /// using either a custom IUserType for certain property types
    /// or standard NHibernateUtil fallback for others.
    /// </summary>
    private void AddScalarsForType<TResult>(ISQLQuery query)
    {
        var props = typeof(TResult).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite) // only map settable props
            .ToList();

        foreach (var prop in props)
        {
            // We'll use the property name as the "column alias". 
            // That means your SQL must do `SELECT Column AS property_name ...`
            // for each property you want to map.
            string alias = configuration.TransformAlias(prop.Name);
            
            var nhType = GetNHTypeFor(prop.PropertyType);
            query.AddScalar(alias, nhType);
        }
    }

    /// <summary>
    /// Maps a .NET type to the corresponding NHibernate IType. Uses either custom mappings
    /// defined in ConventionMaps or NHibernate's built-in type guessing mechanism.
    /// </summary>
    /// <param name="propertyType">The .NET type for which the NHibernate IType is being resolved.</param>
    /// <returns>The NHibernate IType that corresponds to the specified .NET type.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the specified .NET type cannot be resolved to a supported NHibernate IType.</exception>
    private static IType GetNHTypeFor(Type propertyType)
    {
        var nhUserType = UserTypeMapper.GetNhUserType(propertyType);
        if (nhUserType != null)
        {
            return NHibernateUtil.Custom(nhUserType);
        }

        // Fallback: Use NHibernateUtil to resolve built-in types or throw an exception for unsupported types.
        return NHibernateUtil.GuessType(propertyType) ?? 
               throw new InvalidOperationException($"Unsupported type: {propertyType.FullName}");
    }

    /// <summary>
    /// Simple reflection-based parameter setter. 
    /// If your "parameters" object has properties that match named parameters in the SQL (e.g. :id), 
    /// you can set them.
    /// </summary>
    private void AddParameters(ISQLQuery query, object parameters)
    {
        var paramProps = parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in paramProps)
        {
            var paramName = configuration.TransformParameterName(prop.Name);
            var value = prop.GetValue(parameters, null);
            query.SetParameter(paramName, value, GetNHTypeFor(prop.PropertyType));
        }
    }
}