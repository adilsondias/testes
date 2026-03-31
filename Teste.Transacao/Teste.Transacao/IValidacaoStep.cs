namespace Teste.Transacao;

public interface IValidacaoStep
{
    Task<Result> ValidarAsync(TransacaoRequest req, CancellationToken ct);
}
