namespace Teste.Transacao;

public class ValidarFraudeSuspeitStep : IValidacaoStep
{
    public Task<Result> ValidarAsync(TransacaoRequest req, CancellationToken ct)
    {
        decimal limiteAnaliseFraude = 5000;

        if (req.Valor > limiteAnaliseFraude && req.Tipo == "PIX")
            return Task.FromResult(new Result(false, "Transação suspeita de fraude."));

        return Task.FromResult(new Result(true));
    }
}
