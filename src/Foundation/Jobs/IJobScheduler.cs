using System.Threading;
using System.Threading.Tasks;
using Cronos;
using IOKode.OpinionatedFramework.Facades;

namespace IOKode.OpinionatedFramework.Jobs;

/// <summary>
/// Defines a job scheduler contract that allows scheduling, rescheduling, and unscheduling jobs based on a cron expression.
/// </summary>
[AddToFacade("Job")]
public interface IJobScheduler
{
    /// <summary>
    /// Schedules a job to run at the specified interval.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to be scheduled, which must implement <see cref="Job"/>.</typeparam>
    /// <param name="interval">The cron expression representing the schedule interval for the job.</param>
    /// <param name="creator">The job creator.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation, if needed.</param>
    /// <returns>
    /// A task representing the asynchronous operation of enqueuing the job.
    /// The result contains details of the scheduled job.
    /// </returns>
    Task<ScheduledJob<TJob>> ScheduleAsync<TJob>(CronExpression interval, JobCreator<TJob> creator, CancellationToken cancellationToken = default) where TJob : Job;

    /// <summary>
    /// Reschedules an already scheduled job to run at a new specified interval.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to be rescheduled, which must implement <see cref="Job"/>.</typeparam>
    /// <param name="scheduledJob">The job that has already been scheduled and needs rescheduling.</param>
    /// <param name="interval">The new cron expression representing the updated schedule interval for the job.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation, if needed.</param>
    /// <returns>A task representing the asynchronous operation of rescheduling the job.</returns>
    Task RescheduleAsync<TJob>(ScheduledJob<TJob> scheduledJob, CronExpression interval, CancellationToken cancellationToken = default) where TJob : Job;

    /// <summary>
    /// Unschedules a previously scheduled job, effectively removing it from the schedule.
    /// </summary>
    /// <typeparam name="TJob">The type of the job to be unscheduled, which must implement <see cref="Job"/>.</typeparam>
    /// <param name="scheduledJob">The instance of the previously scheduled job to be unscheduled.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation, if needed.</param>
    /// <returns>A task representing the asynchronous operation of unscheduling the job.</returns>
    Task UnscheduleAsync<TJob>(ScheduledJob<TJob> scheduledJob, CancellationToken cancellationToken = default) where TJob : Job;
}