using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Tools.Ajudantes;

namespace Tools.Tipos
{
    public static class Possivel //Ctors que não precisam de tipo
    {
        public static Possivel<T> AlgoSe<T>(bool Se, T valor) => Se ? Possivel<T>.Algo(valor) : Possivel<T>.Nada();
        public static Possivel<T> AlgoSe<T>(bool Se, Func<T> valor) => Se ? Possivel<T>.Algo(valor()) : Possivel<T>.Nada();
        public static Possivel<T> Algo<T>(T valor) => Possivel<T>.Algo(valor);
        public static PossivelNadaElement Nada = new PossivelNadaElement();
    }

    public struct PossivelNadaElement{}


    [DataContract]
    public struct Possivel<T> : IEquatable<Possivel<T>>
    {
        #region Construtores
        internal Possivel(object valor, bool haAlgo) => (this.valor, this.haAlgo) = (valor, haAlgo);
        public static Possivel<T> Nada() => new Possivel<T>(valor: null, haAlgo: false);
        public static Possivel<T> Algo(T valor) => new Possivel<T>(valor: valor, haAlgo: true);
        #endregion

        #region Valores expostos

        public Boolean HaAlgo => haAlgo;
        public T Valor => haAlgo ? (T)valor : throw new NullReferenceException();
        #endregion

        #region Conversores
        public static implicit operator Possivel<T>(T val) => ((object)val == null) ? Possivel<T>.Nada() : Possivel<T>.Algo(val);

        public static implicit operator Possivel<T>(PossivelNadaElement nadaElement) => Possivel<T>.Nada();

        #endregion

        #region Valores internos
        [DataMember] private object valor { get; set; }
        [DataMember] private bool haAlgo { get; set; }
        #endregion

        #region Operadores Comuns
        public override String ToString()
            => this.HaAlgo
            ? $"Possivel<{typeof(T).Name}>.Algo( {this.Valor} )"
            : $"Possivel<{typeof(T).Name}>.Nada";

        #region Operadores de teste de igualdade
        public static bool operator ==(Possivel<T> lhs, Possivel<T> rhs) => lhs.Equals(rhs);
        public static bool operator !=(Possivel<T> lhs, Possivel<T> rhs) => !lhs.Equals(rhs);
        public bool Equals(Possivel<T> other)
            => ((this.HaAlgo == other.HaAlgo)
                && (!this.HaAlgo
                    || this.Valor.Equals(other.Valor)
                    )
                );
        public bool Equals(T other)
            => this.HaAlgo && this.Valor.Equals(other);

        public override bool Equals(object other)
            => (other is Possivel<T>)
                ? this.Equals((Possivel<T>)other)
                : this.valor.Equals(other);

        #endregion

        #endregion
    }

    public static class PossivelStructExtensions
    {
        public static Possivel<T> Join<T>(this Possivel<T?> _this) where T : struct => _this.HaAlgo && _this.Valor.HasValue ? Possivel<T>.Algo(_this.Valor.Value) : Possivel<T>.Nada();
        [MethodImpl(0x100)] public static T? ValorSeHouver<T>(this Possivel<T> _this) where T : struct => _this.HaAlgo ? (T?)_this.Valor : null;
        [MethodImpl(0x100)] public static Possivel<T> AsPossivel<T>(this T? _this) where T : struct => _this.HasValue ? Possivel<T>.Algo(_this.Value) : Possivel<T>.Nada();
        [MethodImpl(0x100)] public static Possivel<T> AsPossivel<T>(this T _this) where T : struct => Possivel<T>.Algo(_this);
        [MethodImpl(0x100)] public static Possivel<T> Join<T>(this Possivel<T>? _this) => _this.HasValue && _this.Value.HaAlgo ? Possivel<T>.Algo(_this.Value.Valor) : Possivel<T>.Nada();

    }

    public static class PossivelExtensions
    {
        public static Possivel<T> Guard<T>(this Possivel<T> _this, Func<T, bool> guardFunc)

        {
            var res = _this.Map(guardFunc).Coalesce(false);
            return _this.Valide(res);
        }
        [MethodImpl(0x100)] public static Possivel<T> Valide<T>(this Possivel<T> _this, bool guard) => guard ? _this : Possivel<T>.Nada();

        [MethodImpl(0x100)] public static Possivel<T> Join<T>(this Possivel<Possivel<T>> _this) => _this.HaAlgo && _this.Valor.HaAlgo ? Possivel.Algo<T>(_this.Valor.Valor) : Possivel.Nada;
        [MethodImpl(0x100)] public static Possivel<T> Join<T>(this Possivel<T> _this) where T : class => _this.HaAlgo && _this.Valor != null ? Possivel<T>.Algo(_this.Valor) : Possivel.Nada;

        [MethodImpl(0x100)] public static Possivel<T> AsPossivel<T>(this T _this) where T : class => _this != null ? Possivel<T>.Algo(_this) : Possivel<T>.Nada();

        [MethodImpl(0x100)] public static T ValorSeHouver<T>(this Possivel<T> _this) where T : class => _this.HaAlgo ? _this.Valor : null;

        [MethodImpl(0x100)] public static Possivel<T> Coalesce<T>(this Possivel<T> _this, Func<Possivel<T>> otherFunc) => _this.HaAlgo ? _this : otherFunc();

        [MethodImpl(0x100)] public static T Coalesce<T>(this Possivel<T> _this, Func<T> otherFunc) => _this.HaAlgo ? _this.Valor : otherFunc();
        [MethodImpl(0x100)] public static Possivel<T> Coalesce<T>(this Possivel<T> _this, Possivel<T> other) => _this.HaAlgo ? _this : other;
        [MethodImpl(0x100)] public static T Coalesce<T>(this Possivel<T> _this, T other) => _this.HaAlgo ? _this.Valor : other;
        [MethodImpl(0x100)] public static Possivel<U> Bind<T, U>(this Possivel<T> _this, Func<T, Possivel<U>> bunc) => _this.HaAlgo ? _this.Valor.In(bunc) : Possivel<U>.Nada();
        [MethodImpl(0x100)] public static Possivel<U> Map<T, U>(this Possivel<T> _this, Func<T, U> func) => _this.HaAlgo ? Possivel<U>.Algo(func(_this.Valor)) : Possivel<U>.Nada();
        [MethodImpl(0x100)] public static Possivel<T> Map<T>(this Possivel<T> _this, Action<T> func)
        {
            if (_this.HaAlgo)
                func(_this.Valor);
            return _this;
        }

        
        
    }
    public static class IEnumerablePossivelExtensoes
    {
        [MethodImpl(0x100)]
        public static IEnumerable<T> ExcetoVazios<T>(this IEnumerable<Possivel<T>> possiveis)
            => possiveis.Where(pX => pX.HaAlgo).Select(pX => pX.Valor);


        [MethodImpl(0x100)]
        public static IEnumerable<T> ExcetoVazios<T, U>(this IEnumerable<T> possiveis, Func<T, Possivel<U>> getPossivelFunc)
            => possiveis.Where(pX => getPossivelFunc(pX).HaAlgo).Select(pX => pX);

        [MethodImpl(0x100)]
        public static Possivel<TVal> TryGet<TKey, TVal>(this IDictionary<TKey, TVal> _this, TKey chave) => _this.ContainsKey(chave) ? Possivel.Algo(_this[chave]) : Possivel.Nada;

    }
    public static class WeakReferencePossivelExtensoes
    {
        public static Possivel<T> TryGetTarget<T>(this WeakReference<T> _this) where T: class
        {
            if (_this.TryGetTarget(out var alvo))
                return Possivel.Algo(alvo);
            else 
                return Possivel.Nada;
        }
    }

}
