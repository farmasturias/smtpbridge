using System.Buffers;
using System.Text;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace GraphSmtpBridge;

public class SmtpMessageHandler : IMessageStore
{
    private readonly GraphClient _graphClient;
    private readonly ILogger<SmtpMessageHandler> _logger;
    private readonly FileLogger _fileLogger;

    public SmtpMessageHandler(GraphClient graphClient, ILogger<SmtpMessageHandler> logger, FileLogger fileLogger)
    {
        _graphClient = graphClient;
        _logger = logger;
        _fileLogger = fileLogger;
    }

    public async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        var clientIp = context.Properties.TryGetValue("ClientEndPoint", out var ep) ? ep.ToString() : "Unknown";
        _logger.LogInformation("Recibiendo correo desde {Ip}", clientIp);

        // Corregido: Extraer direcciones iniciales de las propiedades de la transacción
        var fromBasic = transaction.From != null ? $"{transaction.From.User}@{transaction.From.Host}" : "unknown@sender.com";
        var toBasic = transaction.To
            .Select(r => $"{r.User}@{r.Host}")
            .Where(s => !string.IsNullOrEmpty(s) && s != "@")
            .ToList();

        // Parsea el mensaje usando MimeKit (ideal para archivos adjuntos, imágenes MIME y codificaciones Base64)
        using var stream = new MemoryStream(buffer.ToArray());
        var mimeMessage = await MimeKit.MimeMessage.LoadAsync(stream, cancellationToken);
        
        var subject = mimeMessage.Subject ?? "Sin Asunto";
        
        // Priorizar el cuerpo del mail en HTML, si no existe toma el de texto plano.
        var body = mimeMessage.HtmlBody ?? mimeMessage.TextBody ?? string.Empty;

        // Limpiar saltos de línea y basura extra de correos extraños (opcional pero seguro)
        body = body.Trim();

        var attachments = new List<GraphClient.EmailAttachment>();
        foreach (var attachment in mimeMessage.Attachments.OfType<MimeKit.MimePart>())
        {
            using var ms = new MemoryStream();
            attachment.Content.DecodeTo(ms);
            attachments.Add(new GraphClient.EmailAttachment
            {
                Name = attachment.FileName ?? "adjunto.dat",
                ContentType = attachment.ContentType.MimeType,
                ContentBytes = ms.ToArray()
            });
        }
        
        _logger.LogInformation("Relay de correo: De {From} Para {Count} destinatarios. Asunto: {Subject}. Adjuntos: {AttCount}", fromBasic, toBasic.Count, subject, attachments.Count);
        _fileLogger.Log($"----------------------------------------");
        _fileLogger.Log($"SMTP IN: De '{fromBasic}' Para '{string.Join(", ", toBasic)}' Asunto '{subject}' Adjuntos: {attachments.Count}");

        var success = await _graphClient.SendMailAsync(fromBasic, toBasic, subject, body, attachments);

        // Usando la respuesta predefinida correcta para fallos de transacción
        return success ? SmtpResponse.Ok : SmtpResponse.TransactionFailed;
    }
}
