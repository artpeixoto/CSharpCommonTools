namespace Tools.Tipos
{
    public class Unit { 
        private Unit(){}

        public static Unit Element = new Unit();
        public override bool Equals(object obj)
            => obj is Unit ? true : false;
        public static bool operator ==(Unit lhs, Unit rhs) => true;
        public static bool operator !=(Unit lhs, Unit rhs) => false;
         
    }
}