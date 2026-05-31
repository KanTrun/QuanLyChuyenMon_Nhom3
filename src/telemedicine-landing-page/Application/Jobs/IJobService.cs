using System.Linq.Expressions;

namespace TelemedicineLandingPage.Application.Jobs;

public interface IJobService
{
    string Enqueue(Expression<Action> methodCall);

    string Schedule(Expression<Action> methodCall, TimeSpan delay);

    string ContinueWith(string parentJobId, Expression<Action> methodCall);

    void AddOrUpdateRecurring(string recurringJobId, Expression<Action> methodCall, string cronExpression);
}
