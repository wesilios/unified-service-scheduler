using Scheduler.Application.Queries;

namespace Scheduler.Application.Handlers;

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}