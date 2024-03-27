using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Web;
using Tools.Tipos;

namespace Tools.Ajudantes
{
	public static class Ajudantes
	{

		public static List<int> IndexOfAll(this string texto, char valor)
		{
			var indices = new List<int>();
			for (int i = 0; i < texto.Length; i++)
			{
				var caracter = texto[i];
				if (caracter == valor)
				{
					indices.Add(i);
				}
			}

			return indices;
		}

		/// <summary>
		/// Adicionar um ou mais elemento(s) a um enumerable (não inplace) e o retorna.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static IEnumerable<T> Append<T>(this IEnumerable<T> enumerable, params T[] other)
		{
			foreach (var i in enumerable)
			{
				yield return i;
			}
			foreach (var j in other)
			{
				yield return j;
			}
		}
		public static IEnumerable<T> Append<T>(this IEnumerable<T> enumerable, IEnumerable<T> other)
		{
			foreach (var i in enumerable)
			{
				yield return i;
			}
			foreach (var j in other)
			{
				yield return j;
			}
		}
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> enumerable, params T[] others)
		{
			foreach (var i in others)
				yield return i;
			foreach (var j in enumerable)
				yield return j;
		}

		public static U FoldL<T, U>(
			this IEnumerable<T> enumerable,
			Func<U, T, U> accumulatorFunc,
			U initialAcc)
		{
			var acc = initialAcc;
			foreach (var element in enumerable)
				acc = accumulatorFunc(acc, element);
			return acc;
		}

		public static IEnumerable<U> ScanL<T, U>(this IEnumerable<T> enumerable, Func<U, T, U> accumulatorFunc, U initialAcc)
		{

			var results = new List<U>(){initialAcc};
			foreach (var element in enumerable)
				results.Add(accumulatorFunc(results.Last(), element));

			return results.Skip(1);

		}

		/// <summary>
		/// Semelhante à função IEnumerable.Distinct, com a diferença que você informa uma função
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <param name="enumerable"></param>
		/// <param name="selector"></param>
		/// <returns></returns>
		public static IEnumerable<T> DistinctBy<T, U>(this IEnumerable<T> enumerable, Func<T, U> selector)
		{ return enumerable.GroupBy(selector).Select(pX => pX.First()); }

		public static U UsadoEDepoisDisposto<T, U>(this Func<T> disposableGetFunc, Func<T, U> func) where T : IDisposable
		{
			using (var disposable = disposableGetFunc())
				return func(disposable);
		}

		public static void UsadoEDepoisDisposto<T>(this T _this, Action<T> func) where T : IDisposable
		{
			using (var disposable = _this)
				func(disposable);
		}

		public static U UsadoEDepoisDisposto<T, U>(this T _this, Func<T, U> func) where T : IDisposable
		{
			using (var disposable = _this)
				return func(disposable);
		}


		public static MemoryStream ToMemoryStream(this byte[] _this)
		{
			var res = new MemoryStream(_this);
			res.Seek(0, SeekOrigin.Begin);
			return res;
		}

		public static Byte[] ToByteArray<TStream>(this TStream _this) where TStream : Stream
		{
			var byteArray = new byte[_this.Length - _this.Position];
			_this.Read(byteArray, 0, ( int )(_this.Length - _this.Position));
			return byteArray;
		}

		public static TStream Rewinded<TStream>(this TStream _this) where TStream : Stream
		{
			_this.Seek(0, SeekOrigin.Begin);
			return _this;
		}

		public static IEnumerable<IEnumerable<T>> EmPacotes<T>(this IEnumerable<T> _this, uint tamanhoPacote)
		{
			var resto = _this;

			while (resto.Any())
			{
				yield return resto.Take(( int )tamanhoPacote);
				resto = resto.Skip(( int )tamanhoPacote);
			}
		}
		public static IEnumerable<T> TakeExceptLastLazily<T>(this IEnumerable<T> _this, int numberOfElementsToIgnore)
		{
			Queue<T>       queue    = new Queue<T>( numberOfElementsToIgnore );
			IEnumerator<T> enumtor  = _this.GetEnumerator();

			for (int i = 0; i < numberOfElementsToIgnore && enumtor.MoveNext(); i++)
				queue.Enqueue(enumtor.Current);

			T next;

			while (enumtor.MoveNext())
			{
				next = queue.Dequeue();
				queue.Enqueue(enumtor.Current);

				yield return next;
			}
		}





		public static Boolean Many<T>(this IEnumerable<T> _this)
		{
			return _this.Indexed().Any(pX => pX.Key >= 1);
		}

		public static Boolean Many<T>(this IEnumerable<T> _this, Func<T, Boolean> selectorFunc)
		{
			return _this.Where(selectorFunc).Indexed().Many();
		}

		public static IEnumerable<T> ToEnumerable<T>(this IEnumerator _this)
		{
			while (_this.MoveNext())
				yield return ( T )_this.Current;

		}

		public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> _this)
		{
			while (_this.MoveNext())
				yield return _this.Current;
		}

		public static IEnumerable<String> ReadLines(this StringReader _this)
		{
			var next = _this.ReadLine();
			while (next != null)
			{
				yield return next;
				next = _this.ReadLine();
			}
		}
		public static IEnumerable<String> ReadLines(this StreamReader _this)
		{
			var next = _this.ReadLine();
			while (next != null)
			{
				yield return next;
				next = _this.ReadLine();
			}
		}
		public static MemoryStream ReadToMemory(this Stream stream, int bufferSize )
		{
			List<byte[]> buffers = new List<byte[]>();
			int lastRead = 0, currentRead; //no passado
			byte[] currentBuffer;
			do
			{
				currentBuffer = new byte[ bufferSize ];
				currentRead = stream.Read(currentBuffer, 0, bufferSize);
				if (currentRead == 0) break;
				else { lastRead = currentRead; buffers.Add(currentBuffer); }
			} while (true);

			return buffers.In(bX => bX.TakeExceptLastLazily(1).Cast<IEnumerable<byte>>().Append(bX.Last().Take(lastRead))).SelectMany().ToArray().In(pX => new MemoryStream(pX));
		}
		public static void LoadAll<T>(this IEnumerable<T> _this)
		{
			var enumerator = _this.GetEnumerator();
			while (enumerator.MoveNext()) ;
		}

		[MethodImpl(0x100)] public static IEnumerable<TElem> SelectMany<TEnumerable, TElem>(this IEnumerable<TEnumerable> _this) where TEnumerable : IEnumerable<TElem> { return _this.SelectMany(pX => pX); }


		[MethodImpl(0x100)] public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> _this) { return _this.SelectMany(pX => pX); }

		public static IEnumerable<T> ForEach<T, U>(this IEnumerable<T> _this, Func<T, U> action)
		{
			var enumerator = _this.GetEnumerator();
			while (enumerator.MoveNext())
				action(enumerator.Current);
			return _this;
		}


		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> _this, Action<T> action)
		{
			var enumerator = _this.GetEnumerator();
			while (enumerator.MoveNext())
				action(enumerator.Current);
			return _this;
		}


		public static IDictionary<TKey, TValue2> SelectValues<TKey, TValue1, TValue2>(this IDictionary<TKey, TValue1> _this, Func<TValue1, TValue2> func)
		{
			return _this.ToDictionary(pX => pX.Key, pX => func(pX.Value));
		}



		public static String BackToString(this IEnumerable<char> _this)
		{
			return String.Concat(_this);
		}


		public static IEnumerable<T> Intersperse<T>(this IEnumerable<T> _this, T val)
		{
			var thisEnumtor = _this.GetEnumerator();

			if (thisEnumtor.MoveNext())
				yield return thisEnumtor.Current;

			while (thisEnumtor.MoveNext())
			{
				yield return val;
				yield return thisEnumtor.Current;
			}
		}

		public static T MinBy<T, U>(this IEnumerable<T> _this, Func<T, U> getFocus) where U: IComparable<U>
		{
			var enumtor = _this.Select(x => (elemento: x, valorComparacao: getFocus(x))).GetEnumerator();
			if (enumtor.MoveNext())
			{
				(var elementoMin, var valorComparacaoMinimo) = enumtor.Current;
				while (enumtor.MoveNext())
				{
					(var elementoAtual, var valorComparacaoAtual) = enumtor.Current;
					if (valorComparacaoAtual.CompareTo(valorComparacaoMinimo) < 0)
					{
						elementoMin = elementoAtual;
						valorComparacaoMinimo = valorComparacaoAtual;
					}
				}
				return elementoMin;
			}
			throw new Exception("Enumeração não pode ser vazia.");
		}
		public static IEnumerable<KeyValuePair<int, T>> Indexed<T>(this IEnumerable<T> _this)
		{
			int a = 0;
			foreach (var el in _this)
			{
				yield return new KeyValuePair<int, T>(a, el);
				a++;
			}
		}

		public static IEnumerable<T> Chain<T>(this IEnumerable<T> _this, IEnumerable<IEnumerable<T>> others)
		{
			var res = Enumerable.Empty<IEnumerable<T>>().Append(_this).Append(others).SelectMany(pX => pX);
			return res;
		}

		/// <summary>
		/// Função ajudante que serve apenas para manter a sintaxe fluente. 
		/// <example>
		///	Ao inves de escrever algo como
		///	<code>
		///		var res = func2(b.func1(a, "param").ToString(), 10); 
		/// </code>
		/// que é consideravelmente dificil de ler, poderiamos escrever
		/// <code>
		///		var res = 
		///			a
		///			.UsadoEm(pX => b.func1(pX, "param"))
		///			.ToString()
		///			.UsadoEm(pX => func2(pX, 10));
		/// </code>
		/// em que fica claro a estrutura de processamento dos dados
		/// </example>
		/// </summary>
		/// <returns></returns>
		public static U In<T, U>(this T _this, Func<T, U> func) { return func(_this); }
		public static T In<T>(this T _this, Action<T> func) { func(_this); return _this; }

		public static bool HasContent(this String _this) => !String.IsNullOrWhiteSpace(_this);

		public static IEnumerable<T> Chain<T>(this IEnumerable<T> _this, params IEnumerable<T>[] others)
		{
			return _this.Chain(( IEnumerable<IEnumerable<T>> )others);
		}

		public static IEnumerable<(T Atual, Possivel<T> Proximo)> ComPeek1<T>(this IEnumerable<T> _this)
		{
			var enumtor = _this.GetEnumerator();
			var proximo = enumtor.MoveNext() ? Possivel.Algo(enumtor.Current) : Possivel.Nada<T>();

			while (proximo.HaAlgo)
			{
				var atual = proximo.Valor;
				proximo = enumtor.MoveNext() ? Possivel.Algo(enumtor.Current) : Possivel.Nada<T>();
				yield return (atual, proximo);
			}
		} 
	}
}