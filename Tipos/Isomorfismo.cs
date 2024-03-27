using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tools.Ajudantes;

namespace Tools.Tipos
{
    public class Isomorfismo<TEsq, TDir>
    {
        protected List<(TEsq ValorEsquerdo, TDir ValorDireito)?> Dados = new List<(TEsq valorEsquerdo, TDir valorDireito)?>();
        protected Dictionary<TEsq, int>  ValorEsquerdoParaIndice  = new Dictionary<TEsq, int>();
        protected Dictionary<TDir, int>  ValorDireitoParaIndice   = new Dictionary<TDir, int>();
        protected HashSet<int>           IndicesVazios            = new HashSet<int>();
        protected EndpointEsqParaDir     EndpointE2D;
        protected EndpointDirParaEsq     EndpointD2E;
        protected object                 LockMudanca = new object();
        public Isomorfismo()
        {
            this.EndpointE2D = new EndpointEsqParaDir(this);
            this.EndpointD2E = new EndpointDirParaEsq(this);
        }

        public IDictionary<TEsq, TDir> EsqParaDir => this.EndpointE2D;
        public IDictionary<TDir, TEsq> DirParaEsq => this.EndpointD2E;
        public void Clear()
        {
            lock (LockMudanca)
            {
                this.Dados.Clear();
                this.ValorDireitoParaIndice.Clear();
                this.ValorEsquerdoParaIndice.Clear();
                this.IndicesVazios.Clear();
            }
        }

        public void AdicioneItem((TEsq ValorEsquerdo, TDir ValorDireito) item)
        {
            if (!this.ValorEsquerdoParaIndice.ContainsKey(item.ValorEsquerdo) && !this.ValorDireitoParaIndice.ContainsKey(item.ValorDireito))
            {
                this.AdicioneParNaoExistente(item.ValorEsquerdo, item.ValorDireito);
            }
        }

        #region Internos
        protected void AdicioneParNaoExistente(TEsq valorEsquerdo, TDir valorDireito)
        {
            lock (LockMudanca)
            {
                int indiceEscolhido;
                var haIndiceVazio = IndicesVazios.Any();
                if (haIndiceVazio)
                {
                    indiceEscolhido = IndicesVazios.First();
                    IndicesVazios.Remove(indiceEscolhido);
                }
                else
                {
                    Dados.Add(null);
                    indiceEscolhido = Dados.Count - 1;
                }

                Dados[ indiceEscolhido ] = (valorEsquerdo, valorDireito);
                ValorEsquerdoParaIndice[ valorEsquerdo ] = indiceEscolhido;
                ValorDireitoParaIndice[ valorDireito ] = indiceEscolhido;
            }
        }
        
        protected (TEsq ValorEsquerdo, TDir ValorDireito) RemovaParExistente(int indice)
        {
            lock (LockMudanca)
            {
                var (valorEsquerdo, valorDireito) = this.Dados[ indice ].Value;
                this.Dados[ indice ] = null;
                this.ValorEsquerdoParaIndice.Remove(valorEsquerdo);
                this.ValorDireitoParaIndice.Remove(valorDireito);
                this.IndicesVazios.Add(indice);
                return (valorEsquerdo, valorDireito);
            }
        }

        public class EndpointDirParaEsq : IDictionary< TDir, TEsq>
        {
            protected Isomorfismo<TEsq, TDir> Pai;
            protected IDictionary<TDir, int> DicionarioChaves  => this.Pai.ValorDireitoParaIndice;
            protected IDictionary<TEsq, int> DicionarioValores => this.Pai.ValorEsquerdoParaIndice;

            internal EndpointDirParaEsq (Isomorfismo<TEsq, TDir> pai)
            {
                this.Pai = pai;
            }


            public TEsq this[ TDir key ] {
                get =>
                    this.Pai.ValorDireitoParaIndice[ key ].In(i => this.Pai.Dados[ i ]).Value.ValorEsquerdo;

                set {
                    lock (this.Pai.LockMudanca) {
                        var indiceDoValor =
                            this.DicionarioValores.ContainsKey(value)   
                            ? (int?) this.DicionarioValores[value]    
                            : null;

                        var indiceDaChave = 
                            this.Pai.ValorDireitoParaIndice.ContainsKey(key)          
                            ? (int?) this.Pai.ValorDireitoParaIndice[key]           
                            : null;

                        if (indiceDoValor.HasValue) //valor a direita ja está atribuido 
                        {
                            if (indiceDaChave != indiceDoValor)
                                throw new Exception("Valor ja está atribuido a outra chave");
                            else
                                return;
                        }
                        else
                        {
                            if (indiceDaChave.HasValue)
                            {
                                var indice = indiceDaChave.Value;
                                var valorAntigo = this.Pai.Dados[indice].Value.ValorEsquerdo;
                                this.DicionarioValores.Remove(valorAntigo);
                                this.DicionarioValores[ value ] = indice;
                                this.Pai.Dados[ indice ] = (value, key);
                            }
                            else
                            {
                                this.Pai.AdicioneParNaoExistente(value, key);
                            }
                        }
                    }
                }
            }

            public ICollection<TDir> Keys =>
                this.DicionarioChaves.Keys;
            public ICollection<TEsq> Values => 
                (ICollection<TEsq>) 
                this.DicionarioChaves
                .Values
                .Select(chave => this.Pai.Dados[chave].Value.ValorDireito)
                .ToList();

            public int Count => this.DicionarioChaves.Keys.Count;

            public bool IsReadOnly => false;

            public void Add(TDir key, TEsq value)
            {
                if (!this.ContainsKey(key))
                {
                    this[ key ] = value;
                }
            }

            public void Add(KeyValuePair<TDir, TEsq> item)
            {

                this.Add(item.Key, item.Value);
            }

            public void Clear()
            {
                this.Pai.Clear();
            }

            public bool Contains(KeyValuePair<TDir, TEsq> item)
            {
                if (this.ContainsKey(item.Key))
                    return this[ item.Key ].Equals(item.Value);
                else 
                    return true;
            }

            public bool ContainsKey(TDir key)
            {
                return this.Pai.ValorDireitoParaIndice.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<TDir, TEsq>[] array, int arrayIndex)
            {
                foreach(var par in this.Keys.Indexed())
                    array[ par.Key + arrayIndex ] = new KeyValuePair<TDir, TEsq>(par.Value, this[par.Value]);
            }

            public IEnumerator<KeyValuePair<TDir, TEsq>> GetEnumerator()
            {
                return 
                    this.Keys
                    .Select(chave => new KeyValuePair<TDir, TEsq>(chave, this[ chave ]))
                    .GetEnumerator();
            }

            public bool Remove(TDir key)
            {
                lock (this.Pai.LockMudanca) {
                    if (this.ContainsKey(key))
                    {
                        var indice = this.Pai.ValorDireitoParaIndice[key];
                        this.Pai.RemovaParExistente(indice);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public bool Remove(KeyValuePair<TDir, TEsq> item)
            {
                lock (this.Pai.LockMudanca)
                {
                    if (this.Contains(item))
                        return this.Remove(item.Key);
                    else
                        return false;
                }
            }

            public bool TryGetValue(TDir key, out TEsq value)
            {
                if (this.ContainsKey(key))
                {
                    value = this[ key ];
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
                => (IEnumerator)this.GetEnumerator();
        }

        public class EndpointEsqParaDir : IDictionary<TEsq, TDir>
        {
            protected Isomorfismo<TEsq, TDir> Pai;
            protected IDictionary<TEsq, int> DicionarioChaves  => this.Pai.ValorEsquerdoParaIndice;
            protected IDictionary<TDir, int> DicionarioValores => this.Pai.ValorDireitoParaIndice;

            internal EndpointEsqParaDir (Isomorfismo<TEsq, TDir> pai)
            {
                this.Pai = pai;
            }


            public TDir this[ TEsq key ] {
                get =>
                    DicionarioChaves[ key ].In(i => this.Pai.Dados[ i ]).Value.ValorDireito;

                set {
                    lock (this.Pai.LockMudanca) {
                        var indiceDoValor =
                            this.DicionarioValores.ContainsKey(value)   
                            ? (int?) this.DicionarioValores[value]    
                            : null;

                        var indiceDaChave = 
                            this.DicionarioChaves.ContainsKey(key)          
                            ? (int?) this.DicionarioChaves[key]           
                            : null;

                        if (indiceDoValor.HasValue) //valor a direita ja está atribuido 
                        {
                            if (indiceDaChave != indiceDoValor)
                                throw new Exception("Valor ja está atribuido a outra chave");
                            else
                                return;
                        }
                        else
                        {
                            if (indiceDaChave.HasValue)
                            {
                                var indice = indiceDaChave.Value;
                                var valorAntigo = this.Pai.Dados[indice].Value.ValorDireito;
                                this.DicionarioValores.Remove(valorAntigo);
                                this.DicionarioValores[ value ] = indice;
                                this.Pai.Dados[ indice ] = ( key, value);
                            }
                            else
                            {
                                this.Pai.AdicioneParNaoExistente(key, value);
                            }
                        }
                    }
                }
            }

            public ICollection<TEsq> Keys =>
                this.DicionarioChaves.Keys;
            public ICollection<TDir> Values => 
                (ICollection<TDir>) 
                this.DicionarioChaves
                .Values
                .Select(chave => this.Pai.Dados[chave].Value.ValorDireito)
                .ToList();

            public int Count => this.DicionarioChaves.Keys.Count;

            public bool IsReadOnly => false;

            public void Add(TEsq key, TDir value)
            {
                if (!this.ContainsKey(key))
                    this[ key ] = value;
                else
                    throw new Exception("Chave já foi adicionada");

            }

            public void Add(KeyValuePair<TEsq, TDir> item)
            {
                this.Add(item.Key, item.Value);
            }

            public void Clear()
            {
                this.Pai.Clear();
            }

            public bool Contains(KeyValuePair<TEsq, TDir> item)
            {
                if (this.ContainsKey(item.Key))
                    return this[ item.Key ].Equals(item.Value);
                else 
                    return true;
            }

            public bool ContainsKey(TEsq key)
            {
                return this.DicionarioChaves.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<TEsq, TDir>[] array, int arrayIndex)
            {
                foreach(var par in this.Keys.Indexed())
                    array[ par.Key + arrayIndex ] = new KeyValuePair<TEsq, TDir>(par.Value, this[par.Value]);
            }

            public IEnumerator<KeyValuePair<TEsq, TDir>> GetEnumerator()
            {
                return 
                    this.Keys
                    .Select(chave => new KeyValuePair<TEsq, TDir>(chave, this[ chave ]))
                    .GetEnumerator();
            }

            public bool Remove(TEsq key)
            {
                lock (this.Pai.LockMudanca) {
                    if (this.ContainsKey(key))
                    {
                        var indice = this.DicionarioChaves[key];
                        this.Pai.RemovaParExistente(indice);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public bool Remove(KeyValuePair<TEsq, TDir> item)
            {
                lock (this.Pai.LockMudanca)
                {
                    if (this.Contains(item))
                        return this.Remove(item.Key);
                    else
                        return false;
                }
            }

            public bool TryGetValue(TEsq  key, out TDir value)
            {
                if (this.ContainsKey(key))
                {
                    value = this[ key ];
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
                => (IEnumerator)this.GetEnumerator();
        }
        #endregion
    }
}
