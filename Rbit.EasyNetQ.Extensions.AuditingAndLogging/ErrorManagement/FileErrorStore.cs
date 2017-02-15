using System;
using System.IO;
using Newtonsoft.Json;
using Rbit.EasyNetQ.Interfaces;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.ErrorManagement
{
    public class FileErrorStore : ILogStore<RabbitErrorMessage>
    {
        private readonly string _location;

        public FileErrorStore(string location)
        {
            _location = location;
        }

        public string Save(RabbitErrorMessage message)
        {
            try
            {
                // Write to a default json format
                var folder = $@"{Environment.CurrentDirectory}\{_location}";

                var filename = $"{folder}\\error.{message.Type.Replace(":", "-")}.{DateTime.Now.Ticks.ToString()}.json";

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