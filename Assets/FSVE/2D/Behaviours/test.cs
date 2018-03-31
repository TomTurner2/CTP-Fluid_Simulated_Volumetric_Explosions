using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    private int[] a = new int[2];
	// Use this for initialization
	void Start ()
	{
	    a[0] = 1;
	    a[1] = 2;
		Debug.Log(a[0] + "" + a[1]);
        Swap(ref a);
        Debug.Log(a[0] + "" + a[1]);
	}

    void Swap(ref int[] _a)
    {
        int temp = a[0];
        a[0] = a[1];
        a[1] = temp;
    }
}
