
namespace MonitorManagerTest
{
    public class Worker : BackgroundService
    {
        protected readonly AppSettingTool appSettingTool;
        public Worker(IConfiguration configuration)
        {
            appSettingTool = new AppSettingTool(configuration);
        }

        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                //var monitor = "C:\\Program Files\\Google\\Chrome\\Application";

                while (!stoppingToken.IsCancellationRequested)
                {
                    MonitorUpdaterManager.InitializeLogger();
                    MonitorUpdaterManager.UpdateMonitor(appSettingTool.appURLString, appSettingTool.backupURLString, "10.2");
                    await Task.Delay(TimeSpan.FromSeconds(360), stoppingToken);
                }
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
