namespace Teste.Transacao;

public class ValidarLimiteDiarioStep : IValidacaoStep
{
    public Task<Result> ValidarAsync(TransacaoRequest req, CancellationToken ct)
    {
        decimal limiteDiario = 1000;

        if (req.Valor > limiteDiario)
            return Task.FromResult(new Result(false, "Limite diário excedido."));

        return Task.FromResult(new Result(true));
    }
}
