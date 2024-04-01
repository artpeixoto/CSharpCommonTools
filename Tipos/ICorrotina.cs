using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tools.Ajudantes;

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
		public static ICorrotina<TAtualizacao, TFinal> FacaDeElementoFinal<TAtualizacao, TFinal>(TFinal final) => FacaDeEnumeravel(Enumerable.Repeat(Ou<TAtualizacao, TFinal>.Direito(final), 1));

		public static ICorrotina<Unit, Ou<TTaskReturn, Exception>> FacaDeTask<TTaskReturn>(Task<TTaskReturn> task) => new CorrotinaTask<TTaskReturn>(task);
		public static ICorrotina<Unit, Possivel<Exception>> FacaDeTask(Task task) => FacaDeTask(task.ContinueWith(_ => Unit.Element)).MapFinal(pX => pX.PossivelValorDireito());

		public static ICorrotina<T, Exception> FacaDeFuncao<T>(Func<T> func) => new CorrotinaDyn<T, Exception>(
			() => {
                try
                {
                    var res = func();
                    return Possivel.Algo(Ou<T, Exception>.Esquerdo(res));
                }
                catch (Exception ex)
                {
                    return Possivel.Algo(Ou<T, Exception>.Direito(ex));
                }
            });

		public static ICorrotina<TAtualizacaoOut, TFinalOut> Map<TAtualizacaoIn, TFinalIn, TAtualizacaoOut, TFinalOut>(this ICorrotina<TAtualizacaoIn, TFinalIn> _this, Func<Possivel<Ou<TAtualizacaoIn, TFinalIn>>, Possivel<Ou<TAtualizacaoOut, TFinalOut>>> func) => new CorrotinaComposicao<TAtualizacaoIn, TAtualizacaoOut, TFinalIn, TFinalOut>(func, _this);

		public static ICorrotina<TAtualizacaoOut, TFinalOut> Map<TAtualizacaoIn, TFinalIn, TAtualizacaoOut, TFinalOut>(this ICorrotina<TAtualizacaoIn, TFinalIn> _this, Func<Ou<TAtualizacaoIn, TFinalIn>, Ou<TAtualizacaoOut, TFinalOut>> func) => new CorrotinaComposicao<TAtualizacaoIn, TAtualizacaoOut, TFinalIn, TFinalOut>((pX ) => pX.Map(func), _this);

		public static ICorrotina<TAtualizacaoOut, TFinalOut> Map<TAtualizacaoIn, TFinalIn, TAtualizacaoOut, TFinalOut>(this ICorrotina<TAtualizacaoIn, TFinalIn> _this, Func<TAtualizacaoIn, TAtualizacaoOut> funcAtualizacao, Func<TFinalIn, TFinalOut> funcFinal) 
			=> new CorrotinaComposicao<TAtualizacaoIn, TAtualizacaoOut, TFinalIn, TFinalOut>(
				 sinal => sinal.Map(x => x.BiMap(funcAtualizacao, funcFinal)) ,
				 _this
            );

		public static ICorrotina<TAtualizacaoOut, TFinal> MapAtualizacao<TAtualizacaoIn, TAtualizacaoOut, TFinal>(this ICorrotina<TAtualizacaoIn, TFinal> _this, Func<TAtualizacaoIn, TAtualizacaoOut> funcAtualizacao) => _this.Map(funcAtualizacao, pX => pX);
		public static ICorrotina<TAtualizacao, TFinalOut> MapFinal<TAtualizacao,  TFinalIn, TFinalOut>(this ICorrotina<TAtualizacao, TFinalIn> _this, Func<TFinalIn, TFinalOut> funcFinal) => _this.Map( pX => pX, funcFinal) ;

		public static ICorrotina<TAtualizacao, TFinal> AsCorrotinaFalhante<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, Ou<TFinal, Exception>> _this) 
			=> _this.MapFinal(pX => pX.Lado == Lados.Esquerdo ? pX.ValorEsquerdo : throw pX.ValorDireito);

		public static ICorrotina<TAtualizacao, Unit> AsCorrotinaFalhante<TAtualizacao>(this ICorrotina<TAtualizacao, Possivel<Exception>> _this) => _this.MapFinal(pX => !pX.HaAlgo ? Unit.Element : throw pX.Valor);

		public static ICorrotina<TAtualizacao, Ou<TFinal, Exception>> AsCorrotinaNaoFalhante<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this) => new CorrotinaNaoFalhante<TAtualizacao, TFinal>(_this);
		public static ICorrotina<TAtualizacao, Possivel<Exception>> AsCorrotinaNaoFalhante<TAtualizacao>(this ICorrotina<TAtualizacao,Unit> _this) => new CorrotinaNaoFalhante<TAtualizacao, Unit>(_this).MapFinal(pX => pX.PossivelValorDireito());


		public static ICorrotina<TAtualizacao, TFinal1> Concat<TAtualizacao, TFinal0, TFinal1>(this ICorrotina<TAtualizacao, TFinal0> corrotina0, Func<TFinal0, ICorrotina<TAtualizacao, TFinal1>> montadorCorrotina1) => new CorrotinaConcat<TAtualizacao, TFinal0, TFinal1>(corrotina0, montadorCorrotina1);
		public static ICorrotina<TAtualizacao, TFinal1> Concat<TAtualizacao, TFinal0, TFinal1>(this ICorrotina<TAtualizacao, TFinal0> corrotina0, ICorrotina<TAtualizacao, TFinal1> corrotina1) => corrotina0.Concat((_) => corrotina1);

		public static ICorrotina<TAtualizacao, Ou<TFinal1, Exception>> ConcatComErro<TAtualizacao, TFinal0, TFinal1>(this ICorrotina<TAtualizacao, Ou<TFinal0, Exception>> corrotina0, Func<TFinal0, ICorrotina<TAtualizacao, Ou<TFinal1, Exception>>> montadorCorrotina1) => corrotina0.Concat(pX => pX.BiMap(montadorCorrotina1, erro => FacaDeElementoFinal<TAtualizacao, Ou<TFinal1, Exception>>(Ou.Direito(erro))).Join());


		public static ICorrotina<TAtualizacao, Ou<TFinal1, Exception>> ConcatComErro<TAtualizacao, TFinal1>(
			this ICorrotina<TAtualizacao, Possivel<Exception>> corrotina0,
			ICorrotina<TAtualizacao, Ou<TFinal1, Exception>> corrotina1 
			) 
        => corrotina0.Concat(pX => pX.Map(erro => FacaDeElementoFinal<TAtualizacao, Ou<TFinal1, Exception>>(Ou.Direito(erro))).Coalesce(corrotina1));

		public static ICorrotina<TAtualizacao, Ou<TFinal, Exception>> FacaCorrotinaErro<TAtualizacao, TFinal>(Exception erro) => FacaDeElementoFinal<TAtualizacao, Ou<TFinal, Exception>>(Ou.Direito(erro));
		public static ICorrotina<TAtualizacao, Possivel<Exception>> FacaCorrotinaErro<TAtualizacao>(Exception erro) => FacaDeElementoFinal<TAtualizacao, Possivel<Exception>>(Possivel.Algo(erro));
	}
	
	public class CorrotinaComposicao<TAtualizacaoIn, TAtualizacaoOut, TFinalIn, TFinalOut>: ICorrotina<TAtualizacaoOut, TFinalOut>
	{
		public Func<Possivel<Ou<TAtualizacaoIn, TFinalIn>>, Possivel<Ou<TAtualizacaoOut, TFinalOut>>> FuncaoConversao;
		public ICorrotina<TAtualizacaoIn, TFinalIn> CorrotinaInterna;

		public CorrotinaComposicao(Func<Possivel<Ou<TAtualizacaoIn, TFinalIn>>, Possivel<Ou<TAtualizacaoOut, TFinalOut>>> funcConversao, ICorrotina<TAtualizacaoIn, TFinalIn> corrotinaInterna)
		{
			this.CorrotinaInterna = corrotinaInterna;
			this.FuncaoConversao = funcConversao;
		}

		public IEnumerable<Possivel<Ou<TAtualizacaoOut, TFinalOut>>> Avancar()
			=> this.CorrotinaInterna.Avancar().Select(FuncaoConversao);
    }


    public class CorrotinaNaoFalhante<TAtualizacao, TFinal> : ICorrotina<TAtualizacao, Ou<TFinal, Exception>>
    {
		public CorrotinaNaoFalhante(ICorrotina<TAtualizacao, TFinal> corrotina) { this.Corrotina = corrotina; }
		public ICorrotina<TAtualizacao, TFinal> Corrotina;
        public IEnumerable<Possivel<Ou<TAtualizacao, Ou<TFinal, Exception>>>> Avancar()
        {
			var corrotinaInternaEnumtor = this.Corrotina.Avancar().GetEnumerator();
			while(true) {
				bool continuar; Possivel<Possivel<Ou<TAtualizacao, Ou<TFinal, Exception>>>> valorAtual; {
					try
					{
						if (corrotinaInternaEnumtor.MoveNext())
						{
							valorAtual =
								corrotinaInternaEnumtor
								.Current
								.Map(pX => pX.Map(
									pY => pY,
									pY => Ou<TFinal, Exception>.Esquerdo(pY)
								));
							continuar = true;
						}
						else
						{
							valorAtual = Possivel.Nada;
							continuar = false;
						}
					}
					catch (Exception ex)
					{
						valorAtual =
							Ou<TAtualizacao, Ou<TFinal, Exception>>.Direito(
								   Ou.Direito(ex)
							)
							.In(Possivel.Algo)
							.In(Possivel.Algo);
						continuar = false;
					}
				}

				if (valorAtual.HaAlgo)
					yield return valorAtual.Valor;

				if (!continuar)
					break;
			} 
        }
    }


    public class CorrotinaConcat<TAtualizacao, TFinal0, TFinal1 >: 
		ICorrotina<TAtualizacao, TFinal1>
	{
		public CorrotinaConcat(ICorrotina<TAtualizacao, TFinal0> corrotina0, Func<TFinal0, ICorrotina<TAtualizacao, TFinal1>> montadorCorrotina1)
		{
			this.Corrotina0 = corrotina0;
			this.MontadorCorrotina1 = montadorCorrotina1;
		}
		public ICorrotina<TAtualizacao, TFinal0> Corrotina0 { get; set; } 
		public Func<TFinal0, ICorrotina<TAtualizacao, TFinal1>> MontadorCorrotina1 { get; set; }

        public IEnumerable<Possivel<Ou<TAtualizacao, TFinal1>>> Avancar()
        {
			Possivel<TFinal0> resAnterior = Possivel.Nada;
			foreach(var sinal in Corrotina0.Avancar())
			{
				if (!sinal.ObtenhaValorFinalDoSinal().HaAlgo) {
					yield return sinal.Map(x => Ou<TAtualizacao, TFinal1>.Esquerdo(x.ValorEsquerdo));
				}
				else
				{
					resAnterior = sinal.ObtenhaValorFinalDoSinal().Valor;
					break;
				}
			}
			var corrotina1 = MontadorCorrotina1(resAnterior.Valor);
			foreach (var sinal in corrotina1.Avancar())
				yield return sinal; 	
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
			yield return this.FacaSinalAtualizacao();

			while (!this.Task.IsCompleted)
				yield return this.FacaNada();

			yield return this.FacaFinal (
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
		/// 
		/// </summary>
		public static TAtualizacao FacaValorAtualizacao<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this, Func<TAtualizacao> valor) => valor(); 

		/// <summary>
		/// Essa função não deve ser chamada por objetos que não são corrotinas.<br></br>
		/// Função ajudante para criar uma atualizacao com valor, para facilitar a escrita de codigo.
		/// </summary>
		public static Possivel<Ou<TAtualizacao, TFinal>> FacaSinalAtualizacao<TAtualizacao, TFinal>(this ICorrotina<TAtualizacao, TFinal> _this, TAtualizacao atualizacao) => Possivel.Algo(Ou<TAtualizacao, TFinal>.Esquerdo(atualizacao));


		/// <inheritdoc cref="FacaSinalAtualizacao{TAtualizacao, TFinal}(ICorrotina{TAtualizacao, TFinal}, TAtualizacao)"/>
		public static Possivel<Ou<Unit, TFinal>> FacaSinalAtualizacao<TFinal>(this ICorrotina<Unit, TFinal> _this) => _this.FacaSinalAtualizacao(Unit.Element);


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
