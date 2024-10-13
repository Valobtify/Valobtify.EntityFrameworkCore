namespace Valobtify.EntityFrameworkCore;

class PropertyConvertor
{
    public TSingleValueObject Convert<TSingleValueObject, TValue>(TValue value)
        where TSingleValueObject : SingleValueObject<TSingleValueObject, TValue>, ICreatableValueObject<TSingleValueObject, TValue>
        where TValue : notnull
    {
        return TSingleValueObject.Create(value).Content!;
    }
}