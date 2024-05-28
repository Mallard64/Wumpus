using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Cell : MonoBehaviour
{
    public bool hasPlayer = false;
    public bool hasWumpus = false;
    public int i;
    public int j;
    public int wumi;
    public int wumj;
    public Material Material1;
    public GameObject go;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Math.Abs(wumi-i) <= 1 && Math.Abs(wumj-j) <= 1 && Math.Abs(wumi-i) + Math.Abs(wumj-j) != 0) {
            go.GetComponent<SpriteRenderer>().material = Material1;
        }
        if (hasPlayer && Math.Abs(wumi-i) <= 1 && Math.Abs(wumj-j) <= 1) {
            Debug.Log("WUMPUS WUMPUS");
        }
    }
}
