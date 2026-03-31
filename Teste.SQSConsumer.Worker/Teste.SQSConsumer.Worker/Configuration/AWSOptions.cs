namespace Teste.SQSConsumer.Worker.Configuration;

public class AWSOptions
{
    public SQSOptions SQS { get; set; } = new();
}

public class SQSOptions
{
    public string QueueUrl { get; set; } = string.Empty;
    public int QuantidadeMensagensPorLote { get; set; }
    public int TempoEsperaMensagens { get; set; }
    public int TimeoutVisibilidadeMensagem { get; set; }
}