namespace Tools.Tipos
{
	public interface ICorrotina<TAtualizacao, TFinal>
	{
		Possivel<Ou<TAtualizacao, TFinal>> Avancar();
	}
}