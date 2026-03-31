namespace Teste.Transacao;

public class TransacaoPipeline(
    IEnumerable<IValidacaoStep> _steps
    )
{
    public async Task<Result> ExecuteAsync(TransacaoRequest request, CancellationToken ct = default)
    {
        foreach (var step in _steps)
        {
            ct.ThrowIfCancellationRequested();

            var resultado = await step.ValidarAsync(request, ct);

            if (!resultado.Sucesso)
                return resultado;
        }

        return new Result(true);
    }
}

/// <summary>
/// Definindo a ordem de validação
/// </summary>
public static class TestePipeline
{
    public static async Task<Result> ExecutarAsync()
    {
        var steps = new List<IValidacaoStep>
        {
            new ValidarLimiteDiarioStep(),
            new ValidarFraudeSuspeitStep(),
            new ValidarSaldoDisponivelStep()
        };

        var pipeline = new TransacaoPipeline(steps);
        var request = new TransacaoRequest(Guid.NewGuid(), 1500m, "PIX");
        return await pipeline.ExecuteAsync(request);
    }
}