using System;

namespace StubDb.ModelStorage
{
    public class EntityConnection
    {
        public EntityTypeInfo TypeFirst { get; set; }
        public EntityTypeInfo TypeSecond { get; set; }
        public string ConnectionName { get; set; }
        public int IdFirst { get; set; }
        public int IdSecond { get; set; }

        public EntityConnection(EntityTypeInfo type1, EntityTypeInfo type2, string name, int id1, int id2)
        {
            if (IsDefaultTypeStoringOrder(type1, type2))
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
            ConnectionName = name;
        }

        public EntityConnection(EntityTypeInfo type1, EntityTypeInfo type2, string name): this(type1, type2, name, 0, 0)
        {
        }

        public static bool IsDefaultTypeStoringOrder(string typeNameFirst, string typeNameSecond)
        {
            return String.Compare(typeNameFirst, typeNameSecond, StringComparison.Ordinal) > 0;
        }

        public static bool IsDefaultTypeStoringOrder(EntityTypeInfo typeFirst, EntityTypeInfo typeSecond)
        {
            return IsDefaultTypeStoringOrder(typeFirst.GetId(), typeSecond.GetId());
        }

        public string GetUniqueKey()
        {
            return String.Format("{0}{1}{2}", TypeFirst.UniqueName, TypeSecond.UniqueName, ConnectionName);
        }

        public int GetIdByType(EntityTypeInfo connectedEntityType)
        {
            if (TypeFirst.UniqueName == connectedEntityType.UniqueName)
            {
                return IdFirst;
            }
            else if (TypeSecond.UniqueName == connectedEntityType.UniqueName)
            {
                return IdSecond;
            }

            throw new Exception("Connection does not have this type");
        }
    }
}