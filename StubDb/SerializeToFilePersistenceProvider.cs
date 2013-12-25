using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace StubDb
{
    public class SerializeToFilePersistenceProvider : IContextStoragePersistenceProvider
    {
        public const string TemFilePath = @"C:\Temp\StubDb";

        #region Nested Classes

        public class DataContainer
        {
            public const string SeparatorSymbol = "ߊ";

            public List<EntityContainer> Entities { get; set; }
            public List<ConnectionContainer> Connections { get; set; }

            public DataContainer()
            {
                Entities = new List<EntityContainer>();
                Connections = new List<ConnectionContainer>();
            }

            public DataContainer(StubContext.ContextStorage storage, List<Type> types): this()
            {
                foreach (var entityType in types)
                {
                    var entityContainer = new EntityContainer();
                    entityContainer.TypeName = entityType.FullName;
                    //entityContainer.Properties = this.
                    //var entities = storage.Entities.GetEntities(entityType).Values;
                    //Entities.Add(new List<object>(entities));
                }
            }
            


        }

        public class EntityContainer
        {
            public string TypeName { get; set; }
            public string Properties { get; set; }
            public List<string> ValuesList { get; set; }
        }

        public class ConnectionContainer
        {
            public string FirstType { get; set; }
            public string SecondType { get; set; }
            public string ConnectionsString { get; set; }
        }

        #endregion

        public void SaveContext(StubContext.ContextStorage storage, Dictionary<string, Type> types)
        {
            var data = new DataContainer(storage, types.Select(x => x.Value).ToList());

            string json = JsonConvert.SerializeObject(data);

            using (var writer = new StreamWriter(TemFilePath, false))
            { 
                writer.WriteLine(json);
            }
        }

        public StubContext.ContextStorage LoadContext(Dictionary<string, Type> types)
        {
            var result = (StubContext.ContextStorage) null;

            using (var reader = new StreamReader(TemFilePath, true))
            {
                string json = reader.ReadToEnd();
                var dataContainer = JsonConvert.DeserializeObject<DataContainer>(json);
                int berak = 0;
            }

            return result;
        }
    }
}