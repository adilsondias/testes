using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Teste.Pagamento;

public class PagamentoService
{
    private readonly ILogger<PagamentoService> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly string _connectionString;

    private readonly MemoryCacheEntryOptions _configuracaoCache = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public PagamentoService(
        ILogger<PagamentoService> logger,
        IConfiguration configuration,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string não encontrada.");
    }

    public async Task<Pagamento> BuscarOuCriarAsync(Guid clienteId, decimal valor)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["ClienteId"] = clienteId,
            ["Valor"] = valor
        });

        _logger.LogInformation("Início da consulta e/ou criação do pagamento.");

        if (clienteId == Guid.Empty)
            throw new ArgumentException("ClienteId inválido.", nameof(clienteId));

        if (valor <= 0)
            throw new ArgumentException("O valor deve ser maior que zero.", nameof(valor));

        var identificadorCache = $"pagamento:{clienteId}:{valor}";

        if (_memoryCache.TryGetValue(identificadorCache, out Pagamento? pagamentoCache) && pagamentoCache is not null)
        {
            _logger.LogInformation("Pagamento encontrado no cache.");
            return pagamentoCache;
        }

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var pagamentoEncontrado = await conn.QueryFirstOrDefaultAsync<Pagamento>("SELECT Id, ClienteId, Valor, Status FROM Pagamentos WHERE ClienteId = @ClienteId AND Valor = @Valor", new { ClienteId = clienteId, Valor = valor });

        if (pagamentoEncontrado != null)
        {
            _logger.LogInformation("Pagamento encontrado no banco.");
            _memoryCache.Set(identificadorCache, pagamentoEncontrado, _configuracaoCache);
            return pagamentoEncontrado;
        }

        var novo = new Pagamento
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Valor = valor,
            Status = "Pendente"
        };

        try
        {
            await conn.ExecuteAsync("INSERT INTO Pagamentos (Id, ClienteId, Valor, Status) VALUES (@Id, @ClienteId, @Valor, @Status)", novo);

            _logger.LogInformation("Pagamento criado com sucesso.");

            _memoryCache.Set(identificadorCache, novo, _configuracaoCache);

            return novo;
        }
        catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
        {
            _logger.LogWarning(ex, "Pagamento já foi criado concorrentemente. Buscando registro existente.");

            var pagamentoExistente = await conn.QueryFirstOrDefaultAsync<Pagamento>("SELECT Id, ClienteId, Valor, Status FROM Pagamentos WHERE ClienteId = @ClienteId AND Valor = @Valor", new { ClienteId = clienteId, Valor = valor });

            if (pagamentoExistente == null)
                throw;

            _memoryCache.Set(identificadorCache, pagamentoExistente, _configuracaoCache);

            return pagamentoExistente;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Não foi possível encontrar ou criar o pagamento.");
            throw;
        }
    }
}