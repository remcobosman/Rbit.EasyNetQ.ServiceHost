using Quartz;

namespace Rbit.EasyNetQ.Interfaces
{
    public interface IScheduledTask
    {
        /// <summary>
        /// The Quartz job.
        /// </summary>
        IJobDetail JobDetail { get; }

        /// <summary>
        /// The Quartz trigger (schedule) for the job.
        /// </summary>
        Quartz.Collection.ISet<ITrigger> Triggers { get; }

        /// <summary>
        /// A friendly name for the scheduled task, shown in the logging.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A description for the scheduled task, shown in the logging.
        /// </summary>
        string Description { get; }

        ITaskPrerequisitesValidationResults ValidatePrerequisites();
    }
}