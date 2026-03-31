namespace Teste.Transacao;

public record TransacaoRequest(Guid ContaId, decimal Valor, string Tipo)
{
}
