using Scheduler.Application.Commands;
using Scheduler.Application.Queries;

namespace Scheduler.Application.Interfaces;

public interface IDispatcher
{
    Task<object> SendAsync<TCommand>(TCommand command) where TCommand : ICommand;
    Task<TResult> QueryAsync<TQuery, TResult>(TQuery query) where TQuery : IQuery<TResult>;
}