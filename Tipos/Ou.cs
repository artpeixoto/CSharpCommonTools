using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using Tools.Ajudantes;

namespace Tools.Tipos
{
    public enum Lados { Esquerdo, Direito }

    public class Ou
    {

        public static Ou<T, U> EsquerdoOuDireito<T, U>(bool decider, Func<T> esquerdoGetter, Func<U> direitoGetter) => decider ? Ou<T, U>.Esquerdo(esquerdoGetter()) : Ou<T, U>.Direito(direitoGetter());
        public static Ou<T, U> Esquerdo<T, U>(T valor) => Ou<T, U>.Esquerdo(valor);
        public static Ou<T, U> Direito<T, U>(U valor) => Ou<T, U>.Direito(valor);

        public static OuEsquerdoValueWrapper<T> Esquerdo<T>(T valor) => new OuEsquerdoValueWrapper<T> { Valor = valor };

        public static OuDireitoValueWrapper<T> Direito<T>(T valor) => new OuDireitoValueWrapper<T> { Valor = valor };
    }
    public struct OuEsquerdoValueWrapper<T>
    {
        public T Valor;
    }
    public struct OuDireitoValueWrapper<T>
    {
        public T Valor;
    }

    public struct Ou<T, U> : IEquatable<Ou<T, U>>
    {
        public static explicit operator Ou<T, U>(T valor) => Ou<T, U>.Esquerdo(valor);

        public static explicit operator Ou<T, U>(U valor) => Ou<T, U>.Direito(valor);

        public static implicit operator Ou<T, U>(OuEsquerdoValueWrapper<T> valor)   => Ou<T, U>.Esquerdo(valor.Valor);
        public static implicit operator Ou<T, U>(OuDireitoValueWrapper<U> valor)    => Ou<T, U>.Direito(valor.Valor);

        public class LadoErradoException : Exception { }

        #region Constructors
        internal Ou(object item, Lados side)
        {
            this.Lado = side;
            this.valor = item;
        }

        public static Ou<T, U> Esquerdo(T leftItem) => new Ou<T, U>(leftItem, Lados.Esquerdo);
        public static Ou<T, U> Direito(U rightItem) => new Ou<T, U>(rightItem, Lados.Direito);

        #endregion
        public static bool operator ==(Ou<T, U> lhs, Ou<T, U> rhs) => lhs.Equals(rhs);
        public static bool operator !=(Ou<T, U> lhs, Ou<T, U> rhs) => !lhs.Equals(rhs);
        public bool Equals(Ou<T, U> other)
            => (this.Lado == other.Lado
                && (this.Lado == Lados.Esquerdo
                    ? this.ValorEsquerdo.Equals(other.ValorEsquerdo)
                    : this.ValorDireito.Equals(other.ValorDireito)
                    )
                );


        public Lados Lado { get; private set; }

        internal object valor;
        public T ValorEsquerdo
        {
            get
            {
                if (Lado != Lados.Esquerdo)
                    throw new LadoErradoException();
                else
                    return (T)valor;
            }
            set
            {
                valor = value;
                Lado = Lados.Esquerdo;
            }
        }
        public U ValorDireito
        {
            get
            {
                if (Lado != Lados.Direito)
                    throw new LadoErradoException();
                else
                    return (U)valor;
            }
            set
            {
                valor = value;
                Lado = Lados.Direito;
            }
        }


    }

    /// <summary>
    /// Servem só para conectar o Ou ao resto do codigo de forma simples (nada de criar varios objetos ou coisas assim so para criar ou manipular um Ou) 
    /// </summary>
    public static class ExtensoesOu
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Possivel<T> PossivelValorEsquerdo<T, U>(this Ou<T, U> _this) => _this.Lado == Lados.Esquerdo ? _this.ValorEsquerdo : Possivel.Nada;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Possivel<U> PossivelValorDireito<T, U>(this Ou<T, U> _this) => _this.Lado == Lados.Direito ? _this.ValorDireito : Possivel.Nada;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ou<U, T> Trocado<T, U>(this Ou<T, U> _this)
            => (_this.Lado == Lados.Esquerdo) ? Ou<U, T>.Direito(_this.ValorEsquerdo) : Ou<U, T>.Esquerdo(_this.ValorDireito);

        /// <summary>
        /// A melhor forma de entender esse grande metodo é olhando para sua assinatura de tipo. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<T, Ou<TRes2, U>> Bind<T, U, TRes1, TRes2>(this Func<T, Ou<TRes1, U>> _this, Func<TRes1, Ou<TRes2, U>> lFunc)
            => (tX) => _this(tX).Bind(lFunc);

        /// <summary>
        /// Você tem um Ou chamado foo. Se ele for esquerdo, voce quer aplicar funcao lbar, e se for direito, voce não quer fazer nada. 
        /// Nesse caso, basta: 
        ///  foo.Bind(lbar, rbar)
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ou<TRes, U> Bind<T, U, TRes>(this Ou<T, U> _this, Func<T, Ou<TRes, U>> lFunc)
            => _this.Bind(lFunc, (pX) => (Ou<TRes, U>.Direito(pX)));

        /// <summary>
        /// Você tem um Ou chamado foo. Se ele for esquerdo, voce quer aplicar funcao lbar, e se for direito, voce quer aplicar a funcao rbar. 
        /// Nesse caso, basta: 
        ///  foo.Bind(lbar, rbar)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ou<TRes, URes> Bind<T, U, TRes, URes>(this Ou<T, U> _this, Func<T, Ou<TRes, URes>> lFunc, Func<U, Ou<TRes, URes>> rFunc) =>
             _this.Lado == Lados.Esquerdo
                 ? lFunc(_this.ValorEsquerdo) : rFunc(_this.ValorDireito);



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Ou<TRes, U> Map<T, U, TRes>(this Ou<T, U> _this, Func<T, TRes> func)
            => _this.BiMap(func, (pX) => (pX));

        [MethodImpl(0x0100)]
        public static Ou<TRes, URes> BiMap<T, U, TRes, URes>(this Ou<T, U> _this, Func<T, TRes> lFunc, Func<U, URes> rFunc)
            => _this.Lado == Lados.Esquerdo ? Ou<TRes, URes>.Esquerdo(lFunc(_this.ValorEsquerdo)) : Ou<TRes, URes>.Direito(rFunc(_this.ValorDireito));

        [MethodImpl(0x0100)]
        public static Ou<T, U> BiMap<T, U>(this Ou<T, U> _this, Action<T> lFunc, Action<U> rFunc)
        {
            if (_this.Lado == Lados.Esquerdo) _this.ValorEsquerdo.In(lFunc);
            else _this.ValorDireito.In(rFunc);
            return _this;
        }

        [MethodImpl(0x0100)]
        public static Ou<TRes, U> BiMap<T, TRes, U>(this Ou<T, U> _this, Func<T, TRes> lFunc, Action<U> rFunc)
        {
            if (_this.Lado == Lados.Esquerdo)
                return _this.ValorEsquerdo.In(lFunc).In(Ou<TRes, U>.Esquerdo);

            else
                return _this.ValorDireito.In(rFunc).In(Ou<TRes, U>.Direito);
        }

        [MethodImpl(0x0100)]
        public static Ou<T, URes> BiMap<T, U, URes>(this Ou<T, U> _this, Action<T> lFunc, Func<U, URes> rFunc)
        {
            if (_this.Lado == Lados.Esquerdo)
                return _this.ValorEsquerdo.In(lFunc).In(Ou<T, URes>.Esquerdo);

            else
                return _this.ValorDireito.In(rFunc).In(Ou<T, URes>.Direito);
        }

        public static T Join<T>(this Ou<T, T> _this) => (T)_this.valor;
    }
    public static class ExtensoesOuEnumeraveis
    {
       public static IEnumerable<TVal> Flatten<TVal, TInnerEnumerable>(this IEnumerable<Ou<TVal, TInnerEnumerable>> _this) 
            where TInnerEnumerable: IEnumerable<TVal>
       {
            foreach (var elem in _this)
            {
                switch (elem.Lado)
                {
                    case Lados.Esquerdo:
                        yield return elem.ValorEsquerdo;
                        break;
                    case Lados.Direito:
                        foreach (var innerElem in elem.ValorDireito)
                            yield return innerElem;
                        break;
                }
            }
       }
        public static IEnumerable<TVal> Flatten<TVal, TInnerEnumerable>(this IEnumerable<Ou<TInnerEnumerable, TVal>> _this)
            where TInnerEnumerable : IEnumerable<TVal> => _this.Select(pX => pX.Trocado()).Flatten();
       
    }

}
