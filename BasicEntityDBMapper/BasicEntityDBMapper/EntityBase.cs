using System;

namespace TLM.BasicEntityDBMapper.EntityBase
{
    public abstract class EntityBase<T>
    {
        public T Id { get; set; }
    }
}
