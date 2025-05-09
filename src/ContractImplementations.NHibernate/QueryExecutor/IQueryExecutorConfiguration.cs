using NHibernate.Transform;

namespace IOKode.OpinionatedFramework.ContractImplementations.NHibernate.QueryExecutor;

public interface IQueryExecutorConfiguration
{
    IResultTransformer GetResultTransformer<TResult>();
    string TransformAlias(string alias);
    string TransformParameterName(string parameterName);
}