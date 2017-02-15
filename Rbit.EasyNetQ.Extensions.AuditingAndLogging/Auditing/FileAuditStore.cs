using System;
using System.IO;
using Newtonsoft.Json;
using Rbit.EasyNetQ.Extensions.AuditingAndLogging.Interfaces;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Auditing
{
    /// <summary>
    /// This store saves audit messages to disk.
    /// </summary>
    public class FileAuditStore : ILogStore<RabbitAuditMessage>
    {
        /// <summary>
        /// The location on disk, relative to the current folder of the executable (or service).
        /// </summary>
        private readonly string _location;

        /// <summary>
        /// Initializes a new instance of the FileAuditStore class, this constructor takes the file location to save audit record files to.
        /// </summary>
        /// <param name="location"></param>
        public FileAuditStore(string location)
        {
            _location = location;
        }

        /// <summary>
        /// Saves the audit record to disk.
        /// </summary>
        /// <param name="message">The audit message to save.</param>
        public string Save(RabbitAuditMessage message)
        {
            try
            {
                // Write to a default json format
                var folder = string.Format(@"{0}\{1}", Environment.CurrentDirectory, _location);

                var filename = string.Format("{0}\\audit.{1}.{2}.json", folder, message.ProducerName.Replace(":", "-"), DateTime.Now.Ticks.ToString());

                if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }

                // Write the string to a file.
                var file = new StreamWriter(filename);

                file.WriteLine(JsonConvert.SerializeObject(message));

                file.Close();

                return filename;
            }
            catch
            {
                // explicitely fail silently 
                return string.Empty;
            }
        }
    }
}