using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Teste.SQSConsumer.Worker.Configuration;

namespace Teste.SQSConsumer.Worker;

public class SqsWorker(
    ILogger<SqsWorker> logger,
    IAmazonSQS sqs,
    IOptions<WorkerOptions> workerOptions,
    IOptions<AWSOptions> awsOptions
    ) : BackgroundService
{
    private readonly WorkerOptions _parametrosWorker = workerOptions.Value;
    private readonly AWSOptions _parametrosAWS = awsOptions.Value;
    private readonly ResiliencePipeline _pipelineRetry = new ResiliencePipelineBuilder().AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(2),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder().Handle<Exception>()
    }).Build();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Início - QueueUrl: {QueueUrl}", _parametrosAWS.SQS.QueueUrl);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var resposta = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest()
                {
                    QueueUrl = _parametrosAWS.SQS.QueueUrl,
                    MaxNumberOfMessages = _parametrosAWS.SQS.QuantidadeMensagensPorLote,
                    WaitTimeSeconds = _parametrosAWS.SQS.TempoEsperaMensagens,
                    VisibilityTimeout = _parametrosAWS.SQS.TimeoutVisibilidadeMensagem
                }, cancellationToken);

                if (resposta.Messages.Count == 0)
                    continue;

                logger.LogInformation("{MessageCount} mensagens recebidas.", resposta.Messages.Count);

                await ProcessarEmLoteAsync(resposta.Messages, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogInformation("Finalizado por cancelamento.");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao consumir mensagens da fila {QueueUrl}.", _parametrosAWS.SQS.QueueUrl);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ProcessarEmLoteAsync(IEnumerable<Message> mensagensSQS, CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(_parametrosWorker.LimiteParalelismo);

        var tasks = mensagensSQS.Select(async mensagem =>
        {
            await semaphore.WaitAsync(cancellationToken);

            try
            {
                await ProcessarMensagemAsync(mensagem, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task ProcessarMensagemAsync(Message messagemSQS, CancellationToken cancellationToken)
    {
        var messageId = messagemSQS.MessageId;
        using var scope = logger.BeginScope(new
        {
            MessageId = messageId,
            ReceiveCount = ObterQuantidadeRecebimento(messagemSQS),
            _parametrosAWS.SQS.QueueUrl
        });

        logger.LogInformation("Iniciando processamento da mensagem.");

        try
        {
            await _pipelineRetry.ExecuteAsync(async token =>
            {
                await ProcessarComControleVisibilidadeAsync(messagemSQS, token);
            }, cancellationToken);

            await sqs.DeleteMessageAsync(new DeleteMessageRequest()
            {
                QueueUrl = _parametrosAWS.SQS.QueueUrl,
                ReceiptHandle = messagemSQS.ReceiptHandle
            }, cancellationToken);

            logger.LogInformation("Mensagem {MessageId} processada com sucesso.", messageId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Processamento cancelado para mensagem {MessageId}. A mensagem não será removida da fila.", messageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no processamento da mensagem {MessageId}.", messageId);
        }
    }

    private async Task ProcessarComControleVisibilidadeAsync(Message mensagemSQS, CancellationToken cancellationToken)
    {
        try
        {
            await ProcessAsync(mensagemSQS.Body, cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Falha ao processar a mensagem {MessageId}.", mensagemSQS.MessageId);

            await sqs.ChangeMessageVisibilityAsync(new ChangeMessageVisibilityRequest()
            {
                QueueUrl = _parametrosAWS.SQS.QueueUrl,
                ReceiptHandle = mensagemSQS.ReceiptHandle,
                VisibilityTimeout = 60
            }, cancellationToken);

            throw;
        }
    }

    private static int ObterQuantidadeRecebimento(Message mensagemSQS)
    {
        if (mensagemSQS.Attributes.TryGetValue("ApproximateReceiveCount", out var valorAtributo) && int.TryParse(valorAtributo, out var quantidade))
            return quantidade;

        return 1;
    }

    private async Task ProcessAsync(string mensagem, CancellationToken cancellationToken)
    {
        // Desserializar payload
        // Validar dados
        // Executar regras de negócio
        await Task.Delay(500, cancellationToken);
    }
}
