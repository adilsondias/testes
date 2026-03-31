namespace Teste.Pix;

public class Transacao
{
    public Guid Id { get; set; }
    public Guid IdempotencyKey { get; set; }
    public Guid ContaId { get; set; }
    public decimal Valor { get; set; }
    public string ChavePix { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
}
