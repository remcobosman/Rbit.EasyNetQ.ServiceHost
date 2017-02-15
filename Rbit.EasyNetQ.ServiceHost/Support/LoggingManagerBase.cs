using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Ninject.Extensions.Logging;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public abstract class LoggingManagerBase
    {
        internal static void ProcessSavedFiles<T>(ILogger logger, ILogStore<T> store, string folder)
        {
            if (!Directory.Exists(folder)) return;

            logger.Info("Looking for previously saved files to store in the management database.");

            // See if we previously saved fles to disk, so we can now load them up into the database
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                var message = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(File.ReadAllBytes(file)));

                try
                {
                    store.Save(message);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error loading audit record from file [{0}] into the database, leaving file intact.", file);
                }
                finally
                {

                    File.Delete(file);

                    logger.Debug("Saved audit record from file: [{0}] to the database and removed it from disk.", file);
                }
            }
        }
    }
}
