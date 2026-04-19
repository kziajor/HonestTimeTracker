using FluentValidation;
using HonestTimeTracker.Application;
using HonestTimeTracker.Application.Leaves;
using HonestTimeTracker.Application.Reports;
using HonestTimeTracker.Application.Projects;
using HonestTimeTracker.Application.Records;
using HonestTimeTracker.Application.Settings;
using HonestTimeTracker.Application.Tasks;
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

        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IRecordRepository, RecordRepository>();
        services.AddScoped<ILeaveRepository, LeaveRepository>();

        services.AddScoped<IValidator<CreateProjectCommand>, CreateProjectCommandValidator>();
        services.AddScoped<IValidator<UpdateProjectCommand>, UpdateProjectCommandValidator>();
        services.AddScoped<IValidator<DeleteProjectCommand>, DeleteProjectCommandValidator>();
        services.AddScoped<IValidator<ToggleProjectClosedCommand>, ToggleProjectClosedCommandValidator>();

        services.AddCommandHandler<CreateProjectCommand, CreateProjectCommandHandler, int>();
        services.AddCommandHandler<UpdateProjectCommand, UpdateProjectCommandHandler, Unit>();
        services.AddCommandHandler<DeleteProjectCommand, DeleteProjectCommandHandler, Unit>();
        services.AddCommandHandler<ToggleProjectClosedCommand, ToggleProjectClosedCommandHandler, Unit>();

        services.AddScoped<IQueryHandler<GetProjectsQuery, List<ProjectDto>>, GetProjectsQueryHandler>();

        services.AddScoped<IValidator<CreateTaskCommand>, CreateTaskCommandValidator>();
        services.AddScoped<IValidator<UpdateTaskCommand>, UpdateTaskCommandValidator>();
        services.AddScoped<IValidator<DeleteTaskCommand>, DeleteTaskCommandValidator>();
        services.AddScoped<IValidator<ToggleTaskClosedCommand>, ToggleTaskClosedCommandValidator>();
        services.AddScoped<IValidator<SetTodayListCommand>, SetTodayListCommandValidator>();

        services.AddCommandHandler<CreateTaskCommand, CreateTaskCommandHandler, int>();
        services.AddCommandHandler<UpdateTaskCommand, UpdateTaskCommandHandler, Unit>();
        services.AddCommandHandler<DeleteTaskCommand, DeleteTaskCommandHandler, Unit>();
        services.AddCommandHandler<ToggleTaskClosedCommand, ToggleTaskClosedCommandHandler, Unit>();
        services.AddCommandHandler<SetTodayListCommand, SetTodayListCommandHandler, Unit>();

        services.AddScoped<IQueryHandler<GetTasksQuery, List<TaskDto>>, GetTasksQueryHandler>();
        services.AddScoped<IQueryHandler<GetTodayTasksQuery, List<TaskDto>>, GetTodayTasksQueryHandler>();

        services.AddScoped<IValidator<CreateRecordCommand>, CreateRecordCommandValidator>();
        services.AddScoped<IValidator<UpdateRecordCommand>, UpdateRecordCommandValidator>();
        services.AddScoped<IValidator<DeleteRecordCommand>, DeleteRecordCommandValidator>();
        services.AddScoped<IValidator<StartTimerCommand>, StartTimerCommandValidator>();
        services.AddScoped<IValidator<StopTimerCommand>, StopTimerCommandValidator>();

        services.AddCommandHandler<CreateRecordCommand, CreateRecordCommandHandler, int>();
        services.AddCommandHandler<UpdateRecordCommand, UpdateRecordCommandHandler, Unit>();
        services.AddCommandHandler<DeleteRecordCommand, DeleteRecordCommandHandler, Unit>();
        services.AddCommandHandler<StartTimerCommand, StartTimerCommandHandler, int>();
        services.AddCommandHandler<StopTimerCommand, StopTimerCommandHandler, Unit>();

        services.AddScoped<IQueryHandler<GetRecordsQuery, List<RecordDto>>, GetRecordsQueryHandler>();
        services.AddScoped<IQueryHandler<GetRecordsSummaryQuery, RecordsSummaryDto?>, GetRecordsSummaryQueryHandler>();

        services.AddScoped<IValidator<UpdateSettingsCommand>, UpdateSettingsCommandValidator>();
        services.AddCommandHandler<UpdateSettingsCommand, UpdateSettingsCommandHandler, Unit>();
        services.AddScoped<ICommandHandler<SaveFloatingTimerPositionCommand, Unit>, SaveFloatingTimerPositionCommandHandler>();
        services.AddScoped<IQueryHandler<GetSettingsQuery, SettingsDto>, GetSettingsQueryHandler>();

        services.AddScoped<IValidator<CreateLeaveCommand>, CreateLeaveCommandValidator>();
        services.AddScoped<IValidator<UpdateLeaveCommand>, UpdateLeaveCommandValidator>();
        services.AddScoped<IValidator<DeleteLeaveCommand>, DeleteLeaveCommandValidator>();

        services.AddCommandHandler<CreateLeaveCommand, CreateLeaveCommandHandler, int>();
        services.AddCommandHandler<UpdateLeaveCommand, UpdateLeaveCommandHandler, Unit>();
        services.AddCommandHandler<DeleteLeaveCommand, DeleteLeaveCommandHandler, Unit>();

        services.AddScoped<IQueryHandler<GetLeavesQuery, List<LeaveDto>>, GetLeavesQueryHandler>();

        services.AddScoped<IQueryHandler<GetNormReportQuery, NormReportDto>, GetNormReportQueryHandler>();

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
