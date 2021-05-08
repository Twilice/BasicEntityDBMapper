using System;
using System.Collections.Generic;
using System.Data;

using TLM.BasicEntityDBMapper.EntityMapper;
using TLM.BasicEntityDBMapper.EntityBase;
using System.Data.Common;

namespace TLM.BasicEntityDBMapper.EntityRelation
{
    public class EntityRelation : EntityRelation<long> {
        public EntityRelation(DbDataReader reader) : base(reader) { }
    }
    public class EntityRelation<TDefaultKey>
    {
        public DbDataReader Reader { get; protected set; }
        public List<Relation> Relations { get; protected set; } = new List<Relation>();
        public EntityRelation(DbDataReader reader)
        {
            Reader = reader;
        }

        /// <summary>
        /// Will try to automatically create an "AddChild" function that links parent and child to eachother.
        /// Default parameters will be automatically set if parameters are null.
        /// </summary>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="foreignKeyName">Default is TParentId</param>
        /// <param name="parentIdName">Default is Id</param>
        /// <param name="childListPropertyName">Default is TChilds. Property in parent class linking to list of multiple childs. If name is not found, it will try to find property matching List&lt;TChild&gt;.</param>
        /// <param name="parentPropertyName">Default is TParent. Property in child class linking to parent property. If name is not found, it will try to find property matching TParent</param>
        public void CreateRelation<TParent, TChild>(string foreignKeyName = null, string parentIdName = null, string childListPropertyName = null, string parentPropertyName = null)
        {
            Relations.Add(Relation<TDefaultKey>.Create<TParent, TChild>(foreignKeyName, parentIdName, childListPropertyName, parentPropertyName));
        }
        /// <summary>
        /// Will try to automatically create an "AddChild" function that links parent and child to eachother.
        /// Default parameters will be automatically set if parameters are null.
        /// </summary>
        /// <typeparam name="TForeignKeyType"></typeparam>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="foreignKeyName">Default is TParentId</param>
        /// <param name="parentIdName">Default is Id</param>
        /// <param name="childListPropertyName">Default is TChilds. Property in parent class linking to list of multiple childs. If name is not found, it will try to find property matching List&lt;TChild&gt;.</param>
        /// <param name="parentPropertyName">Default is TParent. Property in child class linking to parent property. If name is not found, it will try to find property matching TParent</param>
        public void CreateRelation<TForeignKeyType, TParent, TChild>(string foreignKeyName = null, string parentIdName = null, string childListPropertyName = null, string parentPropertyName = null)
        {
            Relations.Add(Relation<TForeignKeyType>.Create<TParent, TChild>(foreignKeyName, parentIdName, childListPropertyName, parentPropertyName));
        }
        /// <summary>
        /// Default parameters will be automatically set if parameters are null.
        /// </summary>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="addChildDelegate">Function to link parent and child to eachother.</param>
        /// <param name="foreignKeyName">Default is TParentId</param>
        /// <param name="parentIdName">Default is Id</param>
        public void CreateRelation<TParent, TChild>(Action<TParent, TChild> addChildDelegate, string foreignKeyName = null, string parentIdName = null)
        {
            Relations.Add(Relation<TDefaultKey>.Create(addChildDelegate, foreignKeyName, parentIdName));
        }
        /// <summary>
        /// Default parameters will be automatically set if parameters are null.
        /// </summary>
        /// <typeparam name="TForeignKeyType"></typeparam>
        /// <typeparam name="TParent"></typeparam>
        /// <typeparam name="TChild"></typeparam>
        /// <param name="addChildDelegate">Function to link parent and child to eachother.</param>
        /// <param name="foreignKeyName">Default is TParentId</param>
        /// <param name="parentIdName">Default is Id</param>
        public void CreateRelation<TForeignKeyType, TParent, TChild>(Action<TParent, TChild> addChildDelegate, string foreignKeyName = null, string parentIdName = null)
        {
            Relations.Add(Relation<TForeignKeyType>.Create(addChildDelegate, foreignKeyName, parentIdName));
        }

        /// <summary>
        /// Populates an Entity and setups relation according to data in EntityRelation.
        /// </summary>
        /// <typeparam name="TEntity">Type to populate with reader.</typeparam>
        /// <param name="EntityRelation">Relations to create to other entities.</param>
        /// <param name="entityMapper">Will create a GenericEntityMapper as default.</param>
        /// <param name="onPopulated">If defined, is run on a single entity after it's created.</param>
        /// <returns>List of populated Entities</returns>
        public List<TEntity> PopulateEntities<TEntity>(EntityMapper<TEntity> entityMapper = null, Action<DbDataReader, TEntity> onPopulated = null) where TEntity : EntityBase<TDefaultKey>, new()
        {
            return PopulateEntities<TEntity, TDefaultKey>(entityMapper, onPopulated);
        }
        /// <summary>
        /// Populates an Entity and setups relation according to data in EntityRelation.
        /// </summary>
        /// <typeparam name="TEntity">Type to populate with reader.</typeparam>
        /// <typeparam name="TIdType">Tells the mapper that type other than long will be used.</typeparam>
        /// <param name="EntityRelation">Relations to create to other entities.</param>
        /// <param name="entityMapper">Will create a GenericEntityMapper as default.</param>
        /// <param name="onPopulated">If defined, is run on a single entity after it's created.</param>
        /// <returns>List of populated Entities</returns>
        public List<TEntity> PopulateEntities<TEntity, TIdType>(EntityMapper<TEntity> entityMapper = null, Action<DbDataReader, TEntity> onPopulated = null) where TEntity : EntityBase<TIdType>, new()
        {
            List<Relation> parentTo = new List<Relation>();
            List<Relation> childTo = new List<Relation>();
            foreach (var relationData in Relations)
            {
                if (relationData.parentType == typeof(TEntity))
                    parentTo.Add(relationData);
                else if (relationData.childType == typeof(TEntity))
                    childTo.Add(relationData);
            }

            if (entityMapper == null)
                entityMapper = GenericEntityMapper<TEntity>.GetEntityMapper();

            return _populateEntities<TEntity, TIdType>(parentTo, childTo, entityMapper, onPopulated);
        }

        private List<TEntity> _populateEntities<TEntity, TIdType>(List<Relation> parentTo, List<Relation> childTo, EntityMapper<TEntity> entityMapper, Action<DbDataReader, TEntity> onPopulated) where TEntity : EntityBase<TIdType>, new()
        {
            List<TEntity> resultList = new List<TEntity>();
            if (Reader.HasRows)
            {
                entityMapper.InitializeMapper(Reader);

                foreach (var child in childTo)
                {
                    try
                    {
                        child.ordinalForeignKey = Reader.GetOrdinal(child.foreignKeyName);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new IndexOutOfRangeException($"{e.Message} - {typeof(TEntity).Name} expected column {child.foreignKeyName} to map with {child.parentType.Name} which was not provided.", e);
                    }
                }

                foreach (var parent in parentTo)
                {
                    try
                    {
                        parent.ordinalParentId = Reader.GetOrdinal(parent.parentIdName);
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        throw new IndexOutOfRangeException($"{e.Message} - {typeof(TEntity).Name} expected column {parent.parentIdName} to map with {parent.childType.Name} which was not provided.", e);
                    }
                }

                while (Reader.Read())
                {
                    TEntity entity = new TEntity();
                    entityMapper.PopulateEntity(Reader, ref entity);
                    resultList.Add(entity);
                    if (onPopulated != null)
                        onPopulated(Reader, entity);

                    foreach (var parentRelation in parentTo)
                    {
                        if (parentRelation.isPrimaryKey)
                        {
                            parentRelation.PopulateParentRelationPrimaryKey<TEntity, TIdType>(entity);
                        }
                        else
                        {
                            if (!Reader.IsDBNull(parentRelation.ordinalParentId))
                            {
                                Relation.CachedReflectionInferredDelegates<TEntity>.GenericPopulateParentRelation(Reader, entity, parentRelation.foreignKeyType);
                            }
                        }
                    }
                    foreach (var childRelation in childTo)
                    {
                        if (!Reader.IsDBNull(childRelation.ordinalForeignKey))
                        {
                            if (childRelation.int64ForeignKeyType)
                            {
                                childRelation.PopulateChildRelationInt64(Reader, entity);
                            }
                            else
                            {
                                Relation.CachedReflectionInferredDelegates<TEntity>.GenericPopulateChildRelation(Reader, entity, childRelation.foreignKeyType);
                            }
                        }
                    }
                }
            }
            Reader.NextResult();
            return resultList;
        }
    }

    public abstract class Relation
    {
        public Type parentType;
        public Type childType;
        public Type foreignKeyType;
        public string foreignKeyName;
        public string parentIdName = "Id";
        public bool int64ForeignKeyType;
        public bool isPrimaryKey;
        public Action<object, object> addChildDelegate;
        public int ordinalForeignKey = -1;
        public int ordinalParentId = -1;

        public static Dictionary<T, ParentChildsRelation> GetRelations<T>(Relation relationData)
        {
            return ((Relation<T>)relationData).relations;
        }

        protected static Action<object, object> ConvertDelegateParameter<TParent, TChild>(Action<TParent, TChild> addChildDelegate)
        {
            if (addChildDelegate == null) return null;
            return (e1, e2) => addChildDelegate((TParent)e1, (TChild)e2);
        }

        protected static Action<object, object> CreateGenericAddChildDelegate<TParent, TChild>(string customChildListPropertyName = null, string customParentPropertyName = null)
        {
            Type parentType = typeof(TParent);
            Type childType = typeof(TChild);
            string childListPropertyName = customChildListPropertyName ?? childType.Name + "s";
            string parentPropertyName = customParentPropertyName ?? parentType.Name;

            var propertyInfo = parentType.GetProperty(childListPropertyName);
            if (propertyInfo == null)
            {
                foreach (var property in parentType.GetProperties())
                {
                    if (property.PropertyType == typeof(IList<TChild>) || property.PropertyType == typeof(List<TChild>))
                    {
                        if (propertyInfo != null)
                            throw new InvalidOperationException($"Found multiple properties with signature {typeof(IList<TChild>).Name} found on {typeof(TParent).Name}. Can not automatically distinguish between them.\n Please specify propertyname.");
                        propertyInfo = property;
                    }
                }
                if (propertyInfo == null)
                    throw new InvalidOperationException($"No property named {childListPropertyName} or property signature {typeof(IList<TChild>).Name} of found on {typeof(TParent).Name}.");
            }
            var getAccessor = propertyInfo.GetGetMethod();
            Delegate getDelegate = Delegate.CreateDelegate(typeof(Func<TParent, IList<TChild>>), getAccessor, false) ?? Delegate.CreateDelegate(typeof(Func<TParent, List<TChild>>), getAccessor);
            Func<TParent, IList<TChild>> getChildList = (Func<TParent, IList<TChild>>)getDelegate;
            var setAccessor = propertyInfo.GetSetMethod();
            Delegate setDelegate = Delegate.CreateDelegate(typeof(Action<TParent, IList<TChild>>), setAccessor, false);
            Action<TParent, IList<TChild>> setChildList;

            if (setDelegate != null)
            {
                setChildList = (Action<TParent, IList<TChild>>)setDelegate;
            }
            else
            {
                setDelegate = Delegate.CreateDelegate(typeof(Action<TParent, List<TChild>>), setAccessor);
                setChildList = (parent, list) => ((Action<TParent, List<TChild>>)setDelegate)(parent, (List<TChild>)list); // obs :: Why is Action so stupid so it can't be cast between List and IList? Fully possible with Func.
            }

            propertyInfo = childType.GetProperty(parentPropertyName);
            if (propertyInfo == null)
            {
                foreach (var property in childType.GetProperties())
                {
                    if (property.PropertyType == parentType)
                    {
                        if (propertyInfo != null)
                            throw new InvalidOperationException($"Found multiple properties with signature {parentType.Name} found on {childType.Name}. Can not automatically distinguish between them.\n Please specify propertyname.");
                        propertyInfo = property;
                    }
                }
                if (propertyInfo == null)
                    throw new InvalidOperationException($"No property named {parentPropertyName} or property signature {parentType.Name} found on {childType.Name}.");
            }
            setAccessor = propertyInfo.GetSetMethod();
            Action<TChild, TParent> setParent = (Action<TChild, TParent>)setAccessor.CreateDelegate(typeof(Action<TChild, TParent>));

            return (parent, child) =>
            {
                TParent tParent = (TParent)parent;
                TChild tChild = (TChild)child;
                var list = getChildList(tParent);
                if (list == null)
                {
                    list = new List<TChild>();
                    setChildList(tParent, list);
                }
                list.Add(tChild);
                setParent(tChild, tParent);
            };
        }

        internal void PopulateParentRelationPrimaryKey<TEntity, TIdType>(TEntity entity) where TEntity : EntityBase<TIdType>, new()
        {
            ParentChildsRelation relationInstance;

            var relationDict = GetRelations<TIdType>(this);
            bool relationFound = relationDict.TryGetValue(entity.Id, out relationInstance);

            if (relationFound == false)
            {
                relationDict.Add(entity.Id, new ParentChildsRelation(parent: entity));
            }
            else
            {
                foreach (var child in relationInstance.childList)
                    addChildDelegate(entity, child);
            }
        }

        internal void PopulateChildRelationInt64<TEntity>(DbDataReader reader, TEntity entity) where TEntity : class
        {
            try
            {
                long parentKey = reader.GetInt64(ordinalForeignKey);
                ParentChildsRelation relationInstance;
                bool relationFound = GetRelations<long>(this).TryGetValue(parentKey, out relationInstance);

                if (relationFound == false)
                {
                    GetRelations<long>(this).Add(parentKey, new ParentChildsRelation(child: entity));
                }
                else if (relationInstance.parent != null) // parent has been populated, run the linking delegate
                {
                    addChildDelegate(relationInstance.parent, entity);
                }
                else
                {
                    relationInstance.childList.Add(entity);
                }
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException($"{e.Message} - {typeof(TEntity).Name} expected {foreignKeyName} to be of type  {typeof(long).Name} to map with {parentType.Name} but sqlType was {reader.GetDataTypeName(ordinalParentId)}.", e);
            }
        }

        internal void PopulateCustomParentRelation<TEntity, TKeyType>(DbDataReader reader, TEntity entity) where TEntity : class
        {
            try
            {
                TKeyType entityId = (TKeyType)reader.GetValue(ordinalParentId);
                ParentChildsRelation relationInstance;
                var relationDict = GetRelations<TKeyType>(this);

                bool relationFound = relationDict.TryGetValue(entityId, out relationInstance);

                if (relationFound == false)
                {
                    relationDict.Add(entityId, new ParentChildsRelation(parent: entity));
                }
                else // childs has been populated, run the linking delegate.
                {
                    foreach (var child in relationInstance.childList)
                        addChildDelegate(entity, child);
                }
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException($"{e.Message} - {typeof(TEntity).Name} expected {parentIdName} to be of type {typeof(TKeyType).Name} to map with {childType.Name} but sqlType was {reader.GetDataTypeName(ordinalParentId)}.", e);
            }
        }

        internal void PopulateCustomChildRelation<TEntity, TKeyType>(DbDataReader reader, TEntity entity) where TEntity : class
        {
            try
            {
                TKeyType parentKey = (TKeyType)reader.GetValue(ordinalForeignKey);
                ParentChildsRelation relationInstance;
                var relationDict = GetRelations<TKeyType>(this);
                bool relationFound = relationDict.TryGetValue(parentKey, out relationInstance);

                if (relationFound == false)
                {
                    relationDict.Add(parentKey, new ParentChildsRelation(child: entity));
                }
                else if (relationInstance.parent != null) // parent has been populated, run the linking delegate
                {
                    addChildDelegate(relationInstance.parent, entity);
                }
                else
                {
                    relationInstance.childList.Add(entity);
                }
            }
            catch (InvalidCastException e)
            {
                throw new InvalidCastException($"{e.Message} - {typeof(TEntity).Name} expected {foreignKeyName} to be of type  {typeof(TKeyType).Name} to map with {parentType.Name} but sqlType was {reader.GetDataTypeName(ordinalParentId)}.", e);
            }
        }

       
        internal static class CachedReflectionInferredDelegates<TEntity>
        {
            private static Action<DbDataReader, TEntity> cachedPopulateParentRelation = null;
            private static Action<DbDataReader, TEntity> cachedPopulateChildRelation = null;
            public static void GenericPopulateParentRelation(DbDataReader reader, TEntity entity, Type foreignKeyType)
            {
                if (cachedPopulateParentRelation == null)
                {
                    var methodInfo = typeof(Relation).GetMethod(nameof(Relation.PopulateCustomParentRelation), System.Reflection.BindingFlags.NonPublic);
                    var inferredMethodInfo = methodInfo.MakeGenericMethod(new Type[] { typeof(TEntity), foreignKeyType });
                    cachedPopulateParentRelation = (Action<DbDataReader, TEntity>)inferredMethodInfo.CreateDelegate(typeof(Action<DbDataReader, TEntity>));
                }
                cachedPopulateParentRelation(reader, entity);
            }


            public static void GenericPopulateChildRelation(DbDataReader reader, TEntity entity, Type foreignKeyType)
            {
                if (cachedPopulateChildRelation == null)
                {
                    var methodInfo = typeof(Relation).GetMethod(nameof(Relation.PopulateCustomChildRelation), System.Reflection.BindingFlags.NonPublic);
                    var inferredMethodInfo = methodInfo.MakeGenericMethod(new Type[] { typeof(TEntity), foreignKeyType });
                    cachedPopulateChildRelation = (Action<DbDataReader, TEntity>)inferredMethodInfo.CreateDelegate(typeof(Action<DbDataReader, TEntity>));
                }
                cachedPopulateChildRelation(reader, entity);
            }
        }
    }

    public class Relation<TForeignKeyType> : Relation
    {
        public Dictionary<TForeignKeyType, ParentChildsRelation> relations;
        private Relation(Action<object, object> addChildDelegate, string foreignKeyName, bool int64ForeignKeyType, string customParentIdName, Type parentType, Type childType)
        {
            this.parentType = parentType;
            this.childType = childType;
            this.foreignKeyType = typeof(TForeignKeyType);
            this.addChildDelegate = addChildDelegate;
            this.foreignKeyName = foreignKeyName;
            this.int64ForeignKeyType = int64ForeignKeyType;
            if (customParentIdName != null)
            {
                this.parentIdName = customParentIdName;
                isPrimaryKey = false;
            }
            else isPrimaryKey = true;
        }

        internal static Relation Create<TParent, TChild>(Action<object, object> addChildDelegate, string customForeignKeyName = null, string customParentIdName = null)
        {
            Type parentType = typeof(TParent);
            Type childType = typeof(TChild);
            bool int64ForeignKeyType = typeof(TForeignKeyType) == typeof(long);
            if (customForeignKeyName == null)
            {
                customForeignKeyName = parentType.Name + "Id";
            }

            var newEntityRelation = new Relation<TForeignKeyType>(addChildDelegate, customForeignKeyName, int64ForeignKeyType, customParentIdName, parentType, childType);
            newEntityRelation.relations = new Dictionary<TForeignKeyType, ParentChildsRelation>();
            return newEntityRelation;
        }

        internal static Relation Create<TParent, TChild>(Action<TParent, TChild> addChildDelegate, string customForeignKeyName = null, string customParentIdName = null)
        {
            return Create<TParent, TChild>(ConvertDelegateParameter(addChildDelegate), customForeignKeyName, customParentIdName);
        }

        internal static Relation Create<TParent, TChild>(string customForeignKeyName = null, string customParentIdName = null, string customChildListPropertyName = null, string customParentPropertyName = null)
        {
            var addChildDelegate = CreateGenericAddChildDelegate<TParent, TChild>(customChildListPropertyName, customParentPropertyName);
            return Create<TParent, TChild>(addChildDelegate, customForeignKeyName, customParentIdName);
        }
    }

    // holds data for which parent should be linked to which childs
    public struct ParentChildsRelation
    {
        public object parent;
        public List<object> childList;

        public ParentChildsRelation(object parent = null, object child = null)
        {
            childList = new List<object>();
            if (child != null)
                childList.Add(child);
            this.parent = parent;
        }
    }
}
