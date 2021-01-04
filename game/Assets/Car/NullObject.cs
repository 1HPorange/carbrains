using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Horribly Hacky thiny to allow dictionaries to have null values via Dictionary(NullObject(K), V)
/// via https://stackoverflow.com/questions/4632945/dictionary-with-null-key
/// </summary>
/// <typeparam name="T"></typeparam>
public struct NullObject<T>
{
    private bool isnull;

    private NullObject(T item, bool isnull) : this()
    {
        this.isnull = isnull;
        this.Item = item;
    }

    public NullObject(T item) : this(item, item == null)
    {
    }

    public static NullObject<T> Null()
    {
        return new NullObject<T>();
    }

    public T Item { get; private set; }

    public bool IsNull()
    {
        return this.isnull;
    }

    public static implicit operator T(NullObject<T> nullObject)
    {
        return nullObject.Item;
    }

    public static implicit operator NullObject<T>(T item)
    {
        return new NullObject<T>(item);
    }

    public override string ToString()
    {
        return (Item != null) ? Item.ToString() : "NULL";
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
            return this.IsNull();

        if (!(obj is NullObject<T>))
            return false;

        var no = (NullObject<T>)obj;

        if (this.IsNull())
            return no.IsNull();

        if (no.IsNull())
            return false;

        return this.Item.Equals(no.Item);
    }

    public override int GetHashCode()
    {
        if (this.isnull)
            return 0;

        var result = Item.GetHashCode();

        if (result >= 0)
            result++;

        return result;
    }
}
