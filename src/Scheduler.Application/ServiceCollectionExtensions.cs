using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Scheduler.Application.Commands;
using Scheduler.Application.Handlers;
using Scheduler.Application.Interfaces;
using Scheduler.Application.Queries;
using Scheduler.Application.Services;
using Scheduler.Application.Validators;

namespace Scheduler.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();
        services.AddScoped<AppointmentAvailabilityChecker>();
        services.AddScoped<ICommandHandler<CreateAppointmentCommand>, CreateAppointmentCommandHandler>();
        services.AddScoped<IQueryHandler<CheckAvailabilityQuery, AvailabilityResult>, CheckAvailabilityQueryHandler>();
        services.AddValidatorsFromAssemblyContaining<CreateAppointmentCommandValidator>();

        return services;
    }
}
