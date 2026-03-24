using SmtpServer;
using SmtpServer.Storage;

namespace GraphSmtpBridge;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    public Worker(ILogger<Worker> logger, IConfiguration config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = _config.GetValue<int>("SmtpBridge:SmtpPort", 2525);
        _logger.LogInformation("Iniciando Servidor SMTP en el puerto {Port}", port);

        var options = new SmtpServerOptionsBuilder()
            .ServerName("MS365-Graph-Bridge")
            .Port(port)
            .Build();

        // Corregido: En SmtpServer v9, se debe pasar el ServiceProvider
        var smtpServer = new SmtpServer.SmtpServer(options, _serviceProvider);

        try
        {
            await smtpServer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("El servidor SMTP se ha detenido.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error fatal en el servidor SMTP");
        }
    }
}
