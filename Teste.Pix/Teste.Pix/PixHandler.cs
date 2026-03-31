namespace Teste.Pix;

public class PixHandler(
    ITransacaoRepository _transacaoRepository
    )
{
    public async Task ProcessarConfirmacaoPixAsync(ConfirmacaoPixEvent evento, CancellationToken ct)
    {
        if (evento == null)
            throw new ArgumentException(nameof(evento));

        bool transacaoProcessada = await _transacaoRepository.ExistsProcessadaAsync(evento.IdempotencyKey, ct);

        if (transacaoProcessada)
            return;

        if (evento.ContaId == Guid.Empty)
            throw new ArgumentException("ContaId inválido.", nameof(evento));

        if (evento.Valor <= 0)
            throw new ArgumentException("O valor do PIX deve ser maior que zero.", nameof(evento));

        if (string.IsNullOrWhiteSpace(evento.ChavePix))
            throw new ArgumentException("ChavePix inválida.", nameof(evento));

        await _transacaoRepository.SalvarAsync(new()
        {
            Id = Guid.NewGuid(),
            IdempotencyKey = evento.IdempotencyKey,
            ContaId = evento.ContaId,
            Valor = evento.Valor,
            ChavePix = evento.ChavePix,
            DataCriacao = DateTime.Now
        }, ct);
    }
}