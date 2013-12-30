using System;

namespace StubDb.ModelStorage
{
    public class EntityConnection
    {
        public string TypeFirst { get; set; }
        public string TypeSecond { get; set; }
        public int IdFirst { get; set; }
        public int IdSecond { get; set; }

        public EntityConnection(string type1, string type2, int id1, int id2)
        {
            if (IsRightOrder(type1, type2))
            {
                TypeFirst = type1;
                TypeSecond = type2;
                IdFirst = id1;
                IdSecond = id2;
            }
            else
            {
                TypeFirst = type2;
                TypeSecond = type1;
                IdFirst = id2;
                IdSecond = id1;
            }
        }

        public override bool Equals(object obj)
        {
            var entityConnnection = obj as EntityConnection;

            if (entityConnnection == null) return false;

            return entityConnnection.IdFirst == this.IdFirst && entityConnnection.IdSecond == this.IdSecond
                   && entityConnnection.TypeFirst == this.TypeFirst && entityConnnection.TypeSecond == this.TypeSecond;
        }

        public override int GetHashCode()
        {
            return TypeFirst.GetHashCode() + TypeSecond.GetHashCode();
        }

        //TODO better name
        public static bool IsRightOrder(string typeNameFirst, string typeNameSecond)
        {
            return String.Compare(typeNameFirst, typeNameSecond, StringComparison.Ordinal) > 0;
        }

        public static bool IsRightOrder(Type typeFirst, Type typeSecond)
        {
            return IsRightOrder(typeFirst.GetId(), typeSecond.GetId());
        }

        public int GetIdByType(Type connectedEntityType)
        {
            if (TypeFirst == connectedEntityType.GetId())
            {
                return IdFirst;
            }
            else if (TypeSecond == connectedEntityType.GetId())
            {
                return IdSecond;
            }

            throw new Exception("Connection does not have this type");
        }
    }
}