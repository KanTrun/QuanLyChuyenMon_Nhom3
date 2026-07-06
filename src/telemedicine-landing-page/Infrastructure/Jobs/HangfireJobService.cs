using System.Linq.Expressions;
using Hangfire;
using TelemedicineLandingPage.Application.Jobs;

namespace TelemedicineLandingPage.Infrastructure.Jobs;

public sealed class HangfireJobService : IJobService
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireJobService(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public string Enqueue(Expression<Action> methodCall)
        => _jobs.Enqueue(methodCall);

    public string Schedule(Expression<Action> methodCall, TimeSpan delay)
        => _jobs.Schedule(methodCall, delay);

    public string ContinueWith(string parentJobId, Expression<Action> methodCall)
        => _jobs.ContinueJobWith(parentJobId, methodCall);

    public void AddOrUpdateRecurring(string recurringJobId, Expression<Action> methodCall, string cronExpression)
        => RecurringJob.AddOrUpdate(recurringJobId, methodCall, cronExpression);
}
