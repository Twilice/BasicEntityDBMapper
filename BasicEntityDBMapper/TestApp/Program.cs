using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;

using TLM.BasicEntityDBMapper.EntityBase;
using TLM.BasicEntityDBMapper.EntityRelation;

namespace TestApp
{
    public class TestEntityItem : EntityBase<long>
    {
        public TestEntityManager TestEntityManager { get; set; }
        public int SomeValue { get; set; }
        public int someNotMappedValue;
    }

    public class TestEntityItemCustomStuff : EntityBase<int>
    {
        public TestEntityManager TestEntityManager { get; set; }
    }
    public class TestEntityManager : EntityBase<long>
    {
        public List<TestEntityItem> TestEntityItems { get; set; } = new List<TestEntityItem>();
        public List<TestEntityItemCustomStuff> CustomListOfItem2s { get; set; } = new List<TestEntityItemCustomStuff>();
    }

    class Program
    {
        static void Main(string[] args)
        {
            DbDataReader reader = null; // do query

            EntityRelation entityRelation = new EntityRelation(reader);

            entityRelation.CreateRelation<TestEntityManager, TestEntityItem>();
            entityRelation.CreateRelation<int, TestEntityManager, TestEntityItemCustomStuff>(childListPropertyName: nameof(TestEntityManager.CustomListOfItem2s));

            var managers = entityRelation.PopulateEntities<TestEntityManager>();
            entityRelation.PopulateEntities<TestEntityItem>();
            entityRelation.PopulateEntities<TestEntityItemCustomStuff, int>(onPopulated: (reader, entity) => {
                var str = (string) reader["SomeColumnInDBButNotInEntityModel"];
            });

            var items = managers.First().TestEntityItems;
            var items2 = managers.First().CustomListOfItem2s;

        }
    }
}
