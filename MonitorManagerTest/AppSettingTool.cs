using Microsoft.Extensions.Configuration;

namespace MonitorManagerTest
{
    public class AppSettingTool
    {
        private readonly IConfiguration _configuration = null;
        public AppSettingTool(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public string appURLString => _configuration.GetSection("URLStrings").Get<URLConfiguration>().appURL;
        public string backupURLString => _configuration.GetSection("URLStrings").Get<URLConfiguration>().backupURL;

        public class URLConfiguration
        {
            public string backupURL { get; set; }

            public string appURL { get; set; }
        }
    }
}
