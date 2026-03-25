using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GraphSmtpBridge;

public class GraphClient
{
    public class EmailAttachment
    {
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] ContentBytes { get; set; } = Array.Empty<byte>();
    }

    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<GraphClient> _logger;
    private readonly FileLogger _fileLogger;

    public GraphClient(IConfiguration config, HttpClient httpClient, ILogger<GraphClient> logger, FileLogger fileLogger)
    {
        _config = config;
        _httpClient = httpClient;
        _logger = logger;
        _fileLogger = fileLogger;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var tenantId = _config["SmtpBridge:TenantId"];
        var clientId = _config["SmtpBridge:ClientId"];
        var clientSecret = _config["SmtpBridge:ClientSecret"];

        var url = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", clientId!),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
            new KeyValuePair<string, string>("client_secret", clientSecret!),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var token = doc.RootElement.GetProperty("access_token").GetString();
            _fileLogger.Log("OAuth2 Auth OK: Token obtenido de Microsoft 365.");
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo token de acceso");
            _fileLogger.Log($"OAuth2 Error: No se pudo obtener el token. {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SendMailAsync(string from, List<string> to, string subject, string bodyContent, List<EmailAttachment>? attachments = null, bool isHtml = true)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token)) return false;

        var senderEmail = _config["SmtpBridge:SenderEmail"] ?? from;
        var url = $"https://graph.microsoft.com/v1.0/users/{senderEmail}/sendMail";

        var messageDict = new Dictionary<string, object>
        {
            { "subject", subject },
            { "body", new { contentType = isHtml ? "HTML" : "Text", content = bodyContent } },
            { "toRecipients", to.Select(email => new { emailAddress = new { address = email } }).ToArray() }
        };

        if (attachments != null && attachments.Count > 0)
        {
            messageDict.Add("hasAttachments", true);
            var attList = new List<object>();
            foreach (var att in attachments)
            {
                attList.Add(new Dictionary<string, object>
                {
                    { "@odata.type", "#microsoft.graph.fileAttachment" },
                    { "name", att.Name },
                    { "contentType", att.ContentType },
                    { "contentBytes", Convert.ToBase64String(att.ContentBytes) }
                });
            }
            messageDict.Add("attachments", attList);
        }

        var mailPayload = new Dictionary<string, object>
        {
            { "message", messageDict },
            { "saveToSentItems", "true" }
        };

        var json = JsonSerializer.Serialize(mailPayload);
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Correo enviado correctamente vía Graph API");
                _fileLogger.Log($"SEND OK: Correo enviado a {string.Join(", ", to)} (Asunto: '{subject}').");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Error enviando correo a Graph API: {Status} - {Error}", response.StatusCode, error);
                _fileLogger.Log($"SEND ERROR: Fallo al enviar a MS365 (HTTP {response.StatusCode}): {error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción enviando correo");
            _fileLogger.Log($"SEND EXCEPTION: {ex.Message}");
            return false;
        }
    }
}
