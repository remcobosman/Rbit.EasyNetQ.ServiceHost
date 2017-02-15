using Quartz;
using Quartz.Collection;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public abstract class ScheduledTaskBase : IScheduledTask
    {
        /// <summary>
        /// The Quartz job.
        /// </summary>
        public IJobDetail JobDetail
        {
            get
            {
                return JobBuilder.Create(this.GetType())
                        .WithIdentity(this.Name, this.GroupName)
                        .WithDescription(this.Description)
                        .Build();
            }
        }

        private string RunAtStartupConfigurationName { get { return string.Format("{0}.RunAtStartup", this.GetType().Name); } }

        private string ScheduleConfigurationName { get { return string.Format("{0}.Cron", this.GetType().Name); } }


        /// <summary>
        /// The Quartz trigger (schedule) for the job.
        /// </summary>
        public ISet<ITrigger> Triggers
        {
            get
            {

                if (ConfigurationExtensions.ApplicationSetting(this.RunAtStartupConfigurationName, "false").ToLower() == "true")
                {
                    return new HashSet<ITrigger>
                    {
                        TriggerBuilder.Create()
                            .WithIdentity(string.Format("Run [{0}] at startup", this.Name), this.GroupName)
                            .WithDescription("Runs the scheduled task when the services starts up.")
                            .StartNow()
                            .Build(),

                        TriggerBuilder.Create()
                            .WithIdentity(this.GroupName, string.Format("{0} schedule.", this.Name))
                            .WithDescription(string.Format("Schedule for {0}.", this.Description))
                            .WithCronSchedule(ConfigurationExtensions.ApplicationSetting(this.ScheduleConfigurationName))
                            .Build()
                    };
                }
                return new HashSet<ITrigger>
                {
                    TriggerBuilder.Create()
                        .WithIdentity(string.Format("{0} schedule.", this.Name), this.GroupName)
                        .WithDescription(string.Format("Schedule for {0}.", this.Description))
                        .WithCronSchedule(ConfigurationExtensions.ApplicationSetting(this.ScheduleConfigurationName))
                        .Build()
                };
            }
        }

        protected abstract string GroupName { get; }

        /// <summary>
        /// A friendly name for the scheduled task, shown in the logging.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// A description for the scheduled task, shown in the logging.
        /// </summary>
        public abstract string Description { get; }

        public virtual ITaskPrerequisitesValidationResults ValidatePrerequisites() { return null; }
    }
}
