namespace Teste.Pix;

public interface ITransacaoRepository
{
    Task<Transacao> GetByIdAsync(Guid id, CancellationToken ct);
    Task<bool> ExistsProcessadaAsync(Guid idempotencyKey, CancellationToken ct);
    Task SalvarAsync(Transacao t, CancellationToken ct);
}
