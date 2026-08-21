using FaturamentoService.Data;
using FaturamentoService.Middleware;
using FaturamentoService.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirFrontend", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// --- Configuração do cliente HTTP resiliente para o EstoqueService ---
//
// Política de RETRY: até 3 tentativas adicionais em caso de falha
// transitória (erro de rede, timeout, ou status 5xx do servidor),
// com espera exponencial entre elas (2s, 4s, 8s) - evita martelar
// um serviço que já está com problema.
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError() // trata HttpRequestException e respostas 5xx/408
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: tentativa => TimeSpan.FromSeconds(Math.Pow(2, tentativa)),
            onRetry: (outcome, tempoEspera, tentativa, context) =>
            {
                Console.WriteLine($"[Polly] Tentativa {tentativa} falhou. Nova tentativa em {tempoEspera.TotalSeconds}s.");
            });

// Política de CIRCUIT BREAKER: se 5 chamadas seguidas falharem, o
// circuito "abre" por 30 segundos - novas chamadas falham imediatamente
// (ServicoEstoqueIndisponivelException) sem nem tentar contactar o
// serviço, dando tempo dele se recuperar sem ficar recebendo tráfego.
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, duracao) =>
                Console.WriteLine($"[Polly] Circuito ABERTO por {duracao.TotalSeconds}s - EstoqueService parece fora do ar."),
            onReset: () =>
                Console.WriteLine("[Polly] Circuito FECHADO novamente - EstoqueService respondendo."));

builder.Services.AddHttpClient<IEstoqueClient, EstoqueClient>(client =>
{
    var baseUrl = builder.Configuration["EstoqueService:BaseUrl"]
        ?? throw new InvalidOperationException("EstoqueService:BaseUrl não configurado.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseGlobalExceptionHandling();
app.UseCors("PermitirFrontend");
app.UseAuthorization();
app.MapControllers();

app.Run();
