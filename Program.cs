using GraphSmtpBridge;
using SmtpServer.Storage;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Configurar el servicio para que pueda correr como Servicio de Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "GescofSmtpBridge";
});

// Registrar dependencias
builder.Services.AddSingleton<FileLogger>();
builder.Services.AddHttpClient<GraphClient>();
builder.Services.AddSingleton<IMessageStore, SmtpMessageHandler>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
