using UnityEngine;
using System.Collections;
using System;


[Serializable]
public struct intVector3
{
    public int x, y, z;
    public intVector3(int _x, int _y, int _z)
    {
        this.x = _x;
        this.y = _y;
        this.z = _z;
    }


    public static implicit operator intVector3(Vector3 _vector)//allow vector3 assignment to intVector3
    {
        return new intVector3(Mathf.RoundToInt(_vector.x), Mathf.RoundToInt(_vector.y), Mathf.RoundToInt(_vector.z));
    }


    public static implicit operator Vector3(intVector3 _vector)//allow assignment to vector 3
    {
        return new Vector3(_vector.x, _vector.y, _vector.z);
    }


    public static intVector3 operator +(intVector3 _vector_lhs, intVector3 _vector_rhs)
    {
        return new intVector3(_vector_lhs.x + _vector_rhs.x, _vector_lhs.y + _vector_rhs.y, _vector_lhs.z + _vector_rhs.z);
    }


    public override bool Equals(object _object)//override equals operator
    {
        if (!(_object is intVector3))//if not a world position
            return false;//ignore

        intVector3 position = (intVector3)_object;//cast to world position
        return position.x == x && position.y == y && position.z == z;
    }


    public override int GetHashCode()
    {
        unchecked
        {
            var hash_code = x;
            hash_code = (hash_code * 397) ^ y;
            hash_code = (hash_code * 397) ^ z;
            return hash_code;
        }
    }


    public static readonly intVector3 Zero = new intVector3(0, 0, 0);
    public static readonly intVector3 One = new intVector3(1, 1, 1);
    public static readonly intVector3 Up = new intVector3(0, 1, 0);
    public static readonly intVector3 Right = new intVector3(1, 0, 0);
}