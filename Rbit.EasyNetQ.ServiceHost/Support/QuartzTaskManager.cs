using System;
using System.Linq;
using Ninject;
using Ninject.Extensions.Logging;
using Quartz;
using Rbit.EasyNetQ.Interfaces;
using Rbit.EasyNetQ.ServiceHost.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public class QuartzTaskManager : IQuartzTaskManager
    {
        private readonly ILogger _logger;

        public QuartzTaskManager(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Adds the scheduled jobs defined in the specific handler to quartz and starts the scheduler.
        /// </summary>
        public void InitializeScheduledTasks(IKernel kernel)
        {
            // Get the defined tasks from the configurer
            var tasks = kernel.GetAll<IScheduledTask>().ToList();
            if (tasks.Any())
            {
                _logger.Info("No scheduled tasks found and loaded.");
                return;
            }

            // Setup all the scheduled tasks and start the scheduler
            var scheduler = kernel.Get<ISchedulerFactory>().GetScheduler();

            // Iterate through them all and add the tasks to the scheduler
            foreach (var task in tasks)
            {
                try
                {
                    _logger.Info("Adding task [{0}]", task.Name);
                    var triggers = task.Triggers;
                    foreach (var trigger in triggers)
                    {
                        _logger.Info("Adding trigger/schedule [{0}]", trigger.Description);
                    }

                    scheduler.ScheduleJob(task.JobDetail, triggers, true);

                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error adding task [{0}] as [{1}] occurred.", task.Name, ex.Message);
                }
            }

            scheduler.Start();

            _logger.Info("Found and loaded [{0}] scheduled tasks.", tasks.Count());
        }
    }
}
