using System;
using System.Collections.Generic;
using System.Linq;
using Tools.Ajudantes;

namespace Tools.Tipos {
    public static class Dispatcher
    {
        public static DynDispatcher<TIn, TOut> MakeFromFunc<TIn, TOut>(Func<TIn, Possivel<TOut>> func) 
                => new DynDispatcher<TIn, TOut>(func);
        public static DictDispatcher<IDictionary<TIn, TOut>, TIn, TOut> MakeFromDict< TIn, TOut>(IDictionary<TIn, TOut> dispatcherDict)
            => new DictDispatcher<IDictionary<TIn, TOut>, TIn, TOut> { DispatchDict = dispatcherDict };

        public static IDispatcher<TIn0, TOut> ComposeInput<TIn0, TIn1, TOut>(this IDispatcher<TIn1, TOut> _this, Func<TIn0, TIn1> inputCompose)
            => new DynDispatcher<TIn0, TOut>(pX => _this.TryDispatch(inputCompose(pX)));
        public static IDispatcher<TIn, TOut1> ComposeOutput<TIn, TOut0, TOut1>(this IDispatcher<TIn, TOut0> _this, Func<TOut0, TOut1> outputComposer)
            => new DynDispatcher<TIn, TOut1>(pX => _this.TryDispatch(pX).Map(outputComposer));

        public static IDispatcher<(TIn0, TIn1), TOut> Collapse<TIn0, TIn1, TOut>(this IDispatcher<TIn0, Func<TIn1, TOut>> _this)
            => MakeFromFunc(
                ((TIn0 in_0, TIn1 in_1) res) => _this.TryDispatch(res.in_0).Map(func => func(res.in_1))
            );

        public static IDispatcher<TIn0, TOut> Concat<TIn0, TIn1, TOut>
            (this IDispatcher<TIn1, TOut> _this, Func<TIn0, TIn1> ctxChanger, params IDispatcher<TIn0, TOut>[] others)
            => _this.ComposeInput(ctxChanger).Concat(others);
        public static IDispatcher<TIn, TOut> Concat<TIn, TOut>(this IDispatcher<TIn, TOut> _this, params IDispatcher<TIn, TOut> [] dispatchers)
            => _this + dispatchers.Aggregate((acc, next) => acc + next);

    }


    public abstract class IDispatcher<TIn, TOut>
    {
        public abstract Possivel<TOut> TryDispatch(TIn input);
        public static IDispatcher<TIn, TOut> operator +(IDispatcher<TIn, TOut> lhs, IDispatcher<TIn, TOut> rhs)
        {
            var dispatcherList = new List<IDispatcher<TIn, TOut>>();
            if (lhs is DispatcherSum<TIn, TOut>)
                dispatcherList.AddRange((lhs as DispatcherSum<TIn, TOut>).Dispatchers);
            else
                dispatcherList.Add(lhs);

            if (rhs is DispatcherSum<TIn, TOut>)
                dispatcherList.AddRange((rhs as DispatcherSum<TIn, TOut>).Dispatchers);
            else
                dispatcherList.Add(rhs);

                    
            return new DispatcherSum<TIn, TOut> {
                Dispatchers = dispatcherList
            };
        }
    }

    public class DispatcherSum<TIn, TOut> : IDispatcher<TIn, TOut>
    {
        public List<IDispatcher<TIn, TOut>> Dispatchers { get; set; }

        public override Possivel<TOut> TryDispatch(TIn input)
        {
            Possivel<TOut> res = Possivel.Nada<TOut>();
            foreach (var despachante in this.Dispatchers)
            {
                res = res.Coalesce(despachante.TryDispatch(input));
                if (res.HaAlgo) break;
            }
            return res;
        }
    }

    public class DictDispatcher<TDict, TIn, TOut> : IDispatcher<TIn, TOut>
        where TDict: IDictionary<TIn, TOut>
    {
        public virtual TDict DispatchDict { get; set; }

        public override Possivel<TOut> TryDispatch(TIn input)
            => this.DispatchDict.TryGet(input);
    }

    public class DynDispatcher<TIn, TOut>: IDispatcher<TIn, TOut>
    {
        public virtual Func<TIn, Possivel<TOut>> DispatchFunc { get; set; }
        public DynDispatcher(Func<TIn, Possivel<TOut>> dispatchFunc)
        {
            this.DispatchFunc = dispatchFunc;
        }

        public override Possivel<TOut> TryDispatch(TIn input)
            => this.DispatchFunc(input);
    }
}
