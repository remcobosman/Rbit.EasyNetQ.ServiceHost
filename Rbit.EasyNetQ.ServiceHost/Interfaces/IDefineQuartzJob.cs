using Quartz;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    /// <summary>
    /// Interface that defines the quartz job definitions and triggers.
    /// </summary>
    public interface IDefineQuartzJob
    {
        /// <summary>
        /// The Quartz job.
        /// </summary>
        IJobDetail JobDetail { get; set; }

        /// <summary>
        /// The Quartz trigger (schedule) for the job.
        /// </summary>
        ITrigger Trigger { get; set; }
    }
}
