using System;
using System.Linq;
using EasyNetQ;
using Ninject.Extensions.Logging;

namespace Rbit.EasyNetQ.Extensions.AuditingAndLogging.Support
{
    public class Log4NetLogger : IEasyNetQLogger
    {
        private readonly ILogger _logger;

        public Log4NetLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void DebugWrite(string format, params object[] args)
        {
            if (args == null)
            {
                _logger.Debug(format);
            }
            else
            {
                _logger.Debug(format, args);
            }
        }

        public void InfoWrite(string format, params object[] args)
        {
            if (args == null)
            {
                _logger.Info(format);
            }
            else
            {
                _logger.Info(format, args);
            }
        }

        public void ErrorWrite(string format, params object[] args)
        {
            if (args == null || !args.Any())
            {
                _logger.Error(format);
            }
            else
            {
                _logger.Error(format, args);
            }
        }

        public void ErrorWrite(Exception exception)
        {
            _logger.ErrorException(exception.Message, exception);
        }
    }
}
