using System;
using Ext.Core;

namespace StubDb.ModelStorage
{
    public class EntityConnection
    {
        public EntityTypeInfo TypeFirst { get; set; }
        public EntityTypeInfo TypeSecond { get; set; }
        public string ConnectionName { get; set; }
        public int IdFirst { get; set; }
        public int IdSecond { get; set; }

        private static string _keySeparator = "@";

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
            return String.Format("{1}{0}{2}{0}{3}", _keySeparator, TypeFirst.UniqueName, TypeSecond.UniqueName, ConnectionName);
        }

        public static void ParseFromKey(string key, out string typeFirst, out string typeSecond, out string connectionName)
        {
            var parts = key.Split(new string[] {_keySeparator}, StringSplitOptions.None);

            Check.That(parts.Length == 3, "Wrong key format");

            typeFirst = parts[0];
            typeSecond = parts[1];
            connectionName = parts[2];
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

        public override bool Equals(object obj)
        {
            var entityConnection = obj as EntityConnection;
            
            if (entityConnection == null) return false;
            
            return entityConnection.GetUniqueKey() == this.GetUniqueKey(); //TODO performance maybe better one by one comparison
        }

        public override int GetHashCode()
        {
            return this.GetUniqueKey().GetHashCode();
        }
    }
}