using System.Text.Json;

namespace oamswlatifose.Server.Licensing
{
    public interface ILicensePersistence
    {
        DeploymentState Load();
        void Save(DeploymentState state);
    }

    public class LicensePersistence : ILicensePersistence
    {
        private readonly string _filePath;
        private readonly ILogger<LicensePersistence> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true };

        public LicensePersistence(IWebHostEnvironment env, IConfiguration config, ILogger<LicensePersistence> logger)
        {
            var relativePath = config["LicenseSettings:StatePath"] ?? "license_state.json";
            _filePath = Path.IsPathRooted(relativePath)
                ? relativePath
                : Path.Combine(env.ContentRootPath, relativePath);
            _logger = logger;
        }

        public DeploymentState Load()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var state = JsonSerializer.Deserialize<DeploymentState>(json);
                    if (state is { FirstRunDate: not default(DateTime) })
                        return state;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not read license state file; creating fresh state");
                }
            }

            var fresh = new DeploymentState { FirstRunDate = DateTime.UtcNow };
            Save(fresh);
            _logger.LogInformation("First deployment detected. 30-day trial started on {Date:yyyy-MM-dd}", fresh.FirstRunDate);
            return fresh;
        }

        public void Save(DeploymentState state)
        {
            var json = JsonSerializer.Serialize(state, _jsonOpts);
            File.WriteAllText(_filePath, json);
        }
    }
}
