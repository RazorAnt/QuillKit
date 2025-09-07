using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using QuillKit.Models;

namespace QuillKit.Services;

public class SiteConfigService
{
    private readonly ILogger<SiteConfigService> _logger;
    private readonly IContentService _contentService;
    private readonly string _configPath = "Config/site-config.yml";
    private SiteConfig _config;
    private readonly object _lock = new();

    public SiteConfigService(ILogger<SiteConfigService> logger, IContentService contentService)
    {
        _logger = logger;
        _contentService = contentService;
        _config = new SiteConfig(); // Default config
        
        // Load config at startup - blocking to ensure it's loaded before first use
        try
        {
            LoadConfigAsync().GetAwaiter().GetResult();
            _logger.LogInformation("📝 Site configuration loaded from {ConfigPath}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ Could not load site configuration, using defaults");
        }
    }

    public SiteConfig Config
    {
        get
        {
            lock (_lock)
            {
                return _config;
            }
        }
    }

    public async Task<SiteConfig> LoadConfigAsync()
    {
        try
        {
            if (!await _contentService.FileExistsAsync(_configPath))
            {
                _logger.LogWarning("Site config file not found at {ConfigPath}, creating default", _configPath);
                await CreateDefaultConfigAsync();
            }

            var yaml = await _contentService.ReadFileAsync(_configPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var config = deserializer.Deserialize<SiteConfig>(yaml);
            
            lock (_lock)
            {
                _config = config ?? new SiteConfig();
            }

            return _config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading site configuration from {ConfigPath}", _configPath);
            throw;
        }
    }

    public async Task SaveConfigAsync(SiteConfig config)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var yaml = serializer.Serialize(config);
            await _contentService.WriteFileAsync(_configPath, yaml);
            
            lock (_lock)
            {
                _config = config;
            }

            _logger.LogInformation("💾 Site configuration saved to {ConfigPath}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving site configuration to {ConfigPath}", _configPath);
            throw;
        }
    }

    private async Task CreateDefaultConfigAsync()
    {
        var defaultConfig = new SiteConfig
        {
            Title = "QuillKit",
            Subtitle = "",
            Description = "",
            Author = new AuthorConfig
            {
                Name = "Author",
                Bio = "Bio"
            }
        };

        await SaveConfigAsync(defaultConfig);
    }

    public string GetObfuscatedEmail()
    {
        if (string.IsNullOrEmpty(_config.Social.Email))
            return string.Empty;

        try
        {
            // Convert email to bytes, then to base64
            var emailBytes = System.Text.Encoding.UTF8.GetBytes(_config.Social.Email);
            var base64Email = Convert.ToBase64String(emailBytes);
            
            // Add the obfuscation character 'Q' at random positions like in the original
            var obfuscated = base64Email.Insert(2, "Q").Insert(8, "Q").Insert(15, "Q").Insert(22, "Q");
            
            return obfuscated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error obfuscating email address");
            return string.Empty;
        }
    }
}
