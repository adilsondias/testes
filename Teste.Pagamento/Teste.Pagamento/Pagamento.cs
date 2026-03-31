namespace Teste.Pagamento;

public class Pagamento
{
    public Guid Id { get; set; }
    public Guid ClienteId { get; set; }
    public decimal Valor { get; set; }
    public string Status { get; set; } = string.Empty;
}
