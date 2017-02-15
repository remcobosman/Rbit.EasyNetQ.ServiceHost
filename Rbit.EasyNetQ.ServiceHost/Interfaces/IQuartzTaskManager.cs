using Ninject;

namespace Rbit.EasyNetQ.ServiceHost.Interfaces
{
    public interface IQuartzTaskManager
    {
        void InitializeScheduledTasks(IKernel kernel);
    }
}