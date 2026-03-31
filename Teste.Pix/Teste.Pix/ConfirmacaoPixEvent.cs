namespace Teste.Pix;

public record ConfirmacaoPixEvent(Guid IdempotencyKey, Guid ContaId, decimal Valor, string ChavePix)
{
}