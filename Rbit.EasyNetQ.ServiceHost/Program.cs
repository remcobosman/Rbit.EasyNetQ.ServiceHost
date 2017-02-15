using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Ninject;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.ServiceHost.Interfaces;
using Rbit.EasyNetQ.ServiceHost.Support;
using Topshelf;

namespace Rbit.EasyNetQ.ServiceHost
{
    class Program
    {
        /// <summary>
        /// The logger instance.
        /// </summary>
        private static ILogger _logger;

        static void Main(string[] args)
        {
            // Check for a configuration file, because we run in the ServieHost appdomain we require a ServcieHost.config
            if (!File.Exists(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile))
            {
                throw new ConfigurationErrorsException($"The configuration file is missing, make sure you have a {new FileInfo(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile).Name} in the folder you are running the ServiceHost application.");
            }

            // Define the kernel for injection
            IKernel container = new StandardKernel();

            // Configure log4net
            log4net.Config.XmlConfigurator.Configure();
            _logger = container.Get<ILoggerFactory>().GetCurrentClassLogger();
            _logger.Info("RabbitMQ Host service starting up, IOC framework loaded and logging configured");

            // Start with getting the service name, without we should not run at all!
            var serviceName = ConfigurationExtensions.ApplicationSetting("Rabbit.ServiceName");

            // Set all the services used by the servicehost, we do that befoire the call to the configurer so you can override them
            container.Bind<IQuartzTaskManager>().To<QuartzTaskManager>();
            container.Bind<IErrorHandlingManager>().To<ErrorHandlingManager>();
            container.Bind<IAuditHandlingManager>().To<AuditHandlingManager>();
            container.Bind<IEasyNetQScannerManager>().To<EasyNetQScannerManager>();

            // Get the endpoint configurer
            var configurer = GetConfigurer();

            // Configure the endpoint and get the container (== IKernel from ninject)
            configurer.Configure(container);
            container.Bind<IConfigureThisBusWihtNinject>().ToMethod(x => configurer);

            // Also bind the service so we can release it from the kernel and dispose it.
            container.Bind<IBusServiceHost>().To<ServiceHost>();

            try
            {
                // Get the scheduler to add tasks to
                var scheduler = container.Get<Quartz.IScheduler>();

                CreateAndRunServiceHost(serviceName, container, scheduler);
            }
            catch (ActivationException ex)
            {
                throw new Exception(
                    "Error activating ninject modules, please install the following Nuget packages 'Quartz' and 'Ninject.Extensions.Quartz'.",
                    ex);
            }
        }

        /// <summary>
        /// Creates and runs the service host for the easynetq handler.
        /// </summary>
        /// <param name="serviceName">The name of the handler service.</param>
        /// <param name="container">The injection container used.</param>
        /// <param name="scheduler">The scheduler instance used.</param>
        private static void CreateAndRunServiceHost(string serviceName, IKernel container, Quartz.IScheduler scheduler)
        {
            HostFactory.Run(hostConfigurator =>
            {
                hostConfigurator.Service<IBusServiceHost>(s =>
                {
                    s.ConstructUsing(name => container.Get<IBusServiceHost>());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc =>
                    {
                        try
                        {
                            // Release and Dispose ninject
                            container.Release(tc);
                            container.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error("Error: {0}", ex.Message);
                        }
                        finally
                        {
                            // Shutdown the scheduler
                            scheduler.Shutdown();
                        }
                    });
                });

                // These are default settings, refere to: 
                // http://topshelf.readthedocs.org/en/latest/overview/commandline.html
                // for hpow to specify service options
                hostConfigurator.SetDisplayName(serviceName);
                hostConfigurator.SetDescription($"Handles messages defined for {serviceName} in RabbitMQ.");
                hostConfigurator.SetServiceName(serviceName.Replace(" ", ""));

                // The above options are overridden like:
                // EasyNetQ.GenericHost.exe install -servicename:myservice -description:"My Service description" -displayname:"MY Display Name"

                // The service runs as local system, you might want to modify that after the service is installed or
                // install with the --interactive option (make a .cdm file :-))
            });
        }

        /// <summary>
        /// Loads the configurator class for the rabbit handler.
        /// </summary>
        /// <returns>The configuration class for the rabbit queue service.</returns>
        private static IConfigureThisBusWihtNinject GetConfigurer()
        {
            // Get the path of the executable, mind you when this runs as a service the currentdirectory would be c:\windows\system32, so
            // take the path of the entry assembly.
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            if (string.IsNullOrEmpty(path)) { throw new Exception($"Unable to determin execution path, no configuration class could be loaded for the handler in assembly {Assembly.GetEntryAssembly().FullName}."); }

            var assemblies = Directory.GetFiles(path, "*.dll").Select(Assembly.LoadFrom).ToList();

            if (!assemblies.Any())
            {
                throw new Exception("No handler assembly found for the RabbitMQ service, please make sure you have an handler class that inherits from 'IConfigureThisBusWihtNinject'.");
            }

            // Now we have an assembly, check for IConfigureThisBusWihtNinject
            var configurers = assemblies.SelectMany(x => x.GetTypes()).Where(st => typeof(IConfigureThisBusWihtNinject).IsAssignableFrom(st) && st.GetInterfaces().Contains(typeof(IConfigureThisBusWihtNinject))).ToList();

            if (!configurers.Any())
            {
                throw new Exception(
                    $"No configuration class found in assembly {assemblies.First().FullName}, please make sure you have an handler class.");
            }

            if (configurers.Count > 1)
            {
                throw new Exception(
                    $"More than one configuration class found in {assemblies.First().FullName}, please make sure you have only one assembly defining the handler.");
            }

            return (IConfigureThisBusWihtNinject)Activator.CreateInstance(configurers.First());
        }
    }
}
