using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tools.Tipos
{
	public interface ICorrotina<TAtualizacao, TFinal>
	{
		IEnumerable<Possivel<Ou<TAtualizacao, TFinal>>> Avancar();
	}

	public static class Corrotina
	{
		public static ICorrotina<TAtualizacao, TFinal> FacaDeEnumeravel<TAtualizacao, TFinal>(IEnumerable<Possivel<Ou<TAtualizacao, TFinal>>> enumeravel) => new CorrotinaEnumeravel<TAtualizacao, TFinal>(enumeravel);

		public static ICorrotina<TAtualizacao, TFinal> FacaDeEnumeravel<TAtualizacao, TFinal>(IEnumerable<Ou<TAtualizacao, TFinal>> enumeravel) => new CorrotinaEnumeravel<TAtualizacao, TFinal>(enumeravel.Select(pX => Possivel.Algo(pX)));
		
		public static ICorrotina<TAtualizacao, Unit> FacaDeEnumeravel<TAtualizacao>(IEnumerable<TAtualizacao> enumeravel) 
			=> new CorrotinaEnumeravel<TAtualizacao, Unit>(
				enumeravel
				.Select(pX => Possivel.Algo(Ou<TAtualizacao, Unit>.Esquerdo(pX)))
				.Append(Possivel.Algo(Ou<TAtualizacao, Unit>.Direito(Unit.Element)))
			);
		public static ICorrotina<TAtualizacao, Unit> FacaDeEnumeravel<TAtualizacao>(IEnumerable<Possivel<TAtualizacao>> enumeravel) 
			=> new CorrotinaEnumeravel<TAtualizacao, Unit>(
				enumeravel
				.Select(pX => pX.Map(Ou<TAtualizacao, Unit>.Esquerdo))
				.Append(Possivel.Algo(Ou<TAtualizacao, Unit>.Direito(Unit.Element)))
			);



		public static ICorrotina<TAtualizacao, Possivel<Exception>> AsCapturandoErro<TAtualizacao>(this ICorrotina<TAtualizacao, Unit> corrotina)
	}

	public class CorrotinaConcatenacao<TAtualizacaoIn0, TFinalIn0, TAtualizacaoIn1, TFinalIn1, TAtualizacaoOut, TFinalOut>: 
		ICorrotina<TAtualizacaoOut, TFinalOut>
	{
		public ICorrotina<TAtualizacaoIn0, TFinalIn0> Corrotina0 { get; set; } 
		public Func<Possivel<Ou<TAtualizacaoIn0, TFinalIn0>> , Possivel<Ou<TAtualizacaoOut, TFinalOut>>> FuncConversor0 { get; set; }
		public Func<TFinalIn0, ICorrotina<TAtualizacaoIn1, TFinalIn1>> MontadorCorrotina1 { get; set; }
		public Func<Possivel<Ou<TAtualizacaoIn1, TFinalIn1>> , Possivel<Ou<TAtualizacaoOut, TFinalOut>>> FuncConversor1 { get; set; }

        public IEnumerable<Possivel<Ou<TAtualizacaoOut, TFinalOut>>> Avancar()
        {
			Possivel<TFinalIn0> resAnterior = Possivel.Nada;
			foreach(var sinal in Corrotina0.Avancar())
			{
				if (sinal.Resu
			}
        }
    }

	public class CorrotinaDyn<TAtualizacao, TFinal>: ICorrotina<TAtualizacao, TFinal>
	{
		public Func<Possivel<Ou<TAtualizacao, TFinal>>> Func;
		public CorrotinaDyn(Func<Possivel<Ou<TAtualizacao, TFinal>>> func)
		{
			this.Func = func;
		}

        public IEnumerable<Possivel<Ou<TAtualizacao, TFinal>>> Avancar()
        {
			var continuar = true;
			while (continuar)
			{
				var proximo = Func();
				continuar = !proximo.Bind(pX => pX.PossivelValorDireito()).HaAlgo;
				yield return proximo;
			}
        }
    }
	public class CorrotinaEnumeravel<TAtualizacao, TFinal>: ICorrotina<TAtualizacao, TFinal>
	{
		public IEnumerable<Possivel<Ou<TAtualizacao, TFinal>>> Enumerable;
        public CorrotinaEnumeravel(IEnumerable<Possivel<Ou< TAtualizacao, TFinal>>> enumerable)
		{
			this.Enumerable = enumerable;
		}
        IEnumerable<Possivel<Ou<TAtualizacao, TFinal>>> ICorrotina<TAtualizacao, TFinal>.Avancar()
        {
			return this.Enumerable;
        }
    }
	public class CorrotinaTask<TTaskReturn>: ICorrotina<Unit, Ou<TTaskReturn, Exception>>
	{
		public Task<TTaskReturn> Task; 
		public CorrotinaTask(Task<TTaskReturn> task)
		{
			this.Task = task;
		}

        public IEnumerable<Possivel<Ou<Unit, Ou<TTaskReturn, Exception>>>> Avancar()
        {
			this.Task.Start();
			yield return this.FacaAtualizacao();

			while (!this.Task.IsCompleted)
				yield return this.FacaNada();

			yield return this.FacaFinal(
				!this.Task.IsFaulted
					? Ou<TTaskReturn, Exception>.Esquerdo(this.Task.Result)
					: Ou<TTaskReturn, Exception>.Direito(this.Task.Exception)
			);
        }
    }
	public static class ICorrotinaExtensions
	{
		public static IEnumerable<Possivel<TFinal>> AvancarParaFinal<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this) => _this.Avancar().Select(att => att.Bind(ou => ou.PossivelValorDireito()));
		public static Possivel<TFinal> ObtenhaValorFinalDoSinal<TAtualizacao, TFinal>(this Possivel<Ou<TAtualizacao, TFinal>> sinal) => sinal.Bind(pX => pX.PossivelValorDireito());
		public static Possivel<TAtualizacao> ObtenhaAtualizacaoDoSinal<TAtualizacao, TFinal>(this Possivel<Ou<TAtualizacao, TFinal>> sinal) => sinal.Bind(pX => pX.PossivelValorEsquerdo());
		
		public static Possivel<Exception> ObtenhaErroDoSinal<TAtualizacao>(this Possivel<Ou<TAtualizacao, Possivel<Exception>>> sinal) => sinal.Bind(att => att.PossivelValorDireito()).Join();

		public static Possivel<Exception> ObtenhaErroDoSinal<TAtualizacao, TFinal>(this Possivel<Ou<TAtualizacao, Ou<TFinal, Exception>>> sinal) => sinal.Bind(att => att.PossivelValorDireito()).Bind(attFinal => attFinal.PossivelValorDireito());
	}

	internal static class ICorrotinaInternalExtensions
	{

		/// <summary>
		/// Essa função não deve ser chamada por objetos que não são corrotinas.
		/// Função ajudante para criar uma atualizacao com nada, para facilitar a escrita de codigo.
		/// </summary>
		public static Possivel<Ou<TAtualizacao, TFinal>> FacaNada<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this) 
		{
			return Possivel.Nada;
		}
		/// <summary>
		/// Essa função não deve ser chamada por objetos que não são corrotinas.<br></br>
		/// Função ajudante para criar uma atualizacao com valor, para facilitar a escrita de codigo.
		/// </summary>
		public static Possivel<Ou<TAtualizacao, TFinal>> FacaAtualizacao<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this, TAtualizacao atualizacao) => Possivel.Algo(Ou<TAtualizacao, TFinal>.Esquerdo(atualizacao));


		/// <inheritdoc cref="FacaAtualizacao{TAtualizacao, TFinal}(ICorrotina{TAtualizacao, TFinal}, TAtualizacao)"/>
		public static Possivel<Ou<Unit, TFinal>> FacaAtualizacao<TFinal>(this ICorrotina<Unit, TFinal> _this) => _this.FacaAtualizacao(Unit.Element);


		/// <summary>
		/// Essa função não deve ser chamada por objetos que não são corrotinas.
		/// Função ajudante para criar uma atualizacao final, para facilitar a escrita de codigo.
		/// </summary>
		public static Possivel<Ou<TAtualizacao, TFinal>> FacaFinal<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this, TFinal valorFinal) => Possivel.Algo(Ou<TAtualizacao, TFinal>.Direito(valorFinal));

		public static Possivel<Ou<TAtualizacao, Ou<TFinal, Exception>>> FacaPossivelFinalErro<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, Ou<TFinal, Exception>> _this, Possivel<Exception> possivelErro) => possivelErro.Map(_this.FacaFinalErro).Coalesce(_this.FacaNada);

		/// <summary>
		/// Essa função não deve ser chamada por objetos que não são corrotinas.
		/// Função ajudante para criar uma atualizacao final com um erro, para facilitar a escrita de codigo.
		/// </summary>
		public static Possivel<Ou<TAtualizacao, Ou<TFinal, Exception>>> FacaFinalErro<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, Ou<TFinal, Exception>> _this, Exception erro) => _this.FacaFinal(Ou.Direito(erro));

		/// <summary>
		/// Essa função não deve ser chamada por objetos que não são corrotinas.
		/// Função ajudante para criar uma atualizacao final com um erro, para facilitar a escrita de codigo.
		/// </summary>
		public static Possivel<Ou<TAtualizacao, Possivel<Exception>>> FacaFinalErro<TAtualizacao>(this ICorrotina<TAtualizacao, Possivel<Exception>> _this, Exception erro) => _this.FacaFinal(erro);
	}
}