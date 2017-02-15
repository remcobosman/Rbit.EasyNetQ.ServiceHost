using System.Configuration;

namespace Rbit.EasyNetQ.ServiceHost.Support
{
    public static class ConfigurationExtensions
    {
        public static string ApplicationSetting(string key)
        {
            if (ConfigurationManager.AppSettings[key] == null || string.IsNullOrEmpty(ConfigurationManager.AppSettings[key]))
            {
                throw new ConfigurationErrorsException(
                    $"Missing key '{key}' in the application config, please add the key and restart the application.");
            }
            return ConfigurationManager.AppSettings[key];
        }

        public static string ApplicationSetting(string key, string defaultSetting)
        {
            if (ConfigurationManager.AppSettings[key] == null || string.IsNullOrEmpty(ConfigurationManager.AppSettings[key]))
            {
                return defaultSetting;
            }
            return ConfigurationManager.AppSettings[key];
        }

        public static string GetConnectionString(string name)
        {
            if (ConfigurationManager.ConnectionStrings[name] == null)
            {
                throw new ConfigurationErrorsException(
                    $"Missing the sql connection to the publications store database with the name '{name}'.");
            }

            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }

        public static string GetConnectionString(string name, string defaultSetting)
        {
            if (ConfigurationManager.ConnectionStrings[name] == null)
            {
                return defaultSetting;
            }

            return ConfigurationManager.ConnectionStrings[name].ConnectionString;
        }
    }
}