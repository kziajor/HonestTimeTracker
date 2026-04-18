using FluentValidation;
using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Infrastructure.Persistence;
using HonestTimeTracker.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HonestTimeTracker.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped<IProjectRepository, ProjectRepository>();

        services.AddScoped<IValidator<CreateProjectCommand>, CreateProjectCommandValidator>();
        services.AddScoped<IValidator<UpdateProjectCommand>, UpdateProjectCommandValidator>();
        services.AddScoped<IValidator<DeleteProjectCommand>, DeleteProjectCommandValidator>();
        services.AddScoped<IValidator<ToggleProjectClosedCommand>, ToggleProjectClosedCommandValidator>();

        services.AddCommandHandler<CreateProjectCommand, CreateProjectCommandHandler, int>();
        services.AddCommandHandler<UpdateProjectCommand, UpdateProjectCommandHandler, Unit>();
        services.AddCommandHandler<DeleteProjectCommand, DeleteProjectCommandHandler, Unit>();
        services.AddCommandHandler<ToggleProjectClosedCommand, ToggleProjectClosedCommandHandler, Unit>();

        services.AddScoped<IQueryHandler<GetProjectsQuery, List<ProjectDto>>, GetProjectsQueryHandler>();

        return services;
    }

    private static IServiceCollection AddCommandHandler<TCommand, THandler, TResult>(
        this IServiceCollection services)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResult>>(sp =>
            new ValidatingCommandHandler<TCommand, TResult>(
                sp.GetRequiredService<THandler>(),
                sp.GetRequiredService<IValidator<TCommand>>()));
        return services;
    }
}
