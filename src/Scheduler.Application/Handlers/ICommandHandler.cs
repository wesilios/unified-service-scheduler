using Scheduler.Application.Commands;

namespace Scheduler.Application.Handlers;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task<object> HandleAsync(TCommand command);
}