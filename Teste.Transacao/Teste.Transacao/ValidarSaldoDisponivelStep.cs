namespace Teste.Transacao;

public class ValidarSaldoDisponivelStep : IValidacaoStep
{
    public Task<Result> ValidarAsync(TransacaoRequest req, CancellationToken ct)
    {
        decimal saldoDisponivel = 2000;

        if (req.Valor > saldoDisponivel)
            return Task.FromResult(new Result(false, "Saldo insuficiente."));

        return Task.FromResult(new Result(true));
    }
}
