using NRedisStack.RedisStackCommands;
using NRedisStack;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

//Configuration options
IConfiguration configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .Build();

var _redisServerSection = configuration.GetSection("ServerConnection");
var _url = _redisServerSection.GetValue<string>("Url");
var _port = _redisServerSection.GetValue<int>("Port");
var _isSsl = _redisServerSection.GetValue<bool>("IsSSL");
var _password = _redisServerSection.GetValue<string>("Password");
var _certName = _redisServerSection.GetValue<string>("CertName");

StringBuilder _connectionLogs;
var _connectionTextWriter = new StringWriter(_connectionLogs = new StringBuilder());
var configurationOptions = new ConfigurationOptions
{
    EndPoints = { $"{_url}:{_port}" },
    Ssl = _isSsl,
    IncludeDetailInExceptions = true,
    AbortOnConnectFail = false
};

if (_isSsl)
{
    configurationOptions.CertificateSelection += ConfigurationOptions_CertificateSelection;
}

System.Security.Cryptography.X509Certificates.X509Certificate ConfigurationOptions_CertificateSelection(object sender, string targetHost, System.Security.Cryptography.X509Certificates.X509CertificateCollection localCertificates, System.Security.Cryptography.X509Certificates.X509Certificate? remoteCertificate, string[] acceptableIssuers)
{
    var certPath = _certName;
    var password = _password;

    try
    {
        var cert = new X509Certificate2(certPath, password);
        return cert;
    }
    catch (Exception e)
    {
        return null;
    }
}

ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(configurationOptions, _connectionTextWriter);
IDatabase db = redis.GetDatabase();

app.MapGet("/", () => "Hello World!");

var key = "myKey";
app.MapPost("/todoitems/{id}", async (string id) =>
{
    JsonCommands json = db.JSON();
    
    json.Set(key, "$", new LayerResult { Status = "OK", Value = id });

    Results.Ok();
});

app.MapGet("/todoitems", async () =>
{
    JsonCommands json = db.JSON();
    var results = json.Get<LayerResult>(key);
    Console.WriteLine(results.Status.ToString());
    Console.WriteLine(results.Value.ToString());
    Results.Ok(results.Value);
});

app.Run();

class LayerResult
{
    public string Status { get; set; }
    public string Value { get; set; }
}