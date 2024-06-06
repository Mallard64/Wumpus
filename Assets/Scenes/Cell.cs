using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System.Threading;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class Cell : MonoBehaviour
{
    public bool hasPlayer = false;
    public bool hasWumpus = false;
    public bool hasArrow = false;
    public bool hasPit = false;
    public bool hasBat = false;
    public int i;
    public int j;
    public Dictionary<string, Cell> neighbors = new Dictionary<string, Cell>();
    public Dictionary<string, Cell> next = new Dictionary<string, Cell>();
    public int numConnections = 0;
    // Start is called before the first frame update
    void Start()
    {
        GetComponentInChildren<Text>().text = GetCellIndex().ToString();
    }

    public int GetCellIndex()
    {
        return TMP.gridWidth * j + i + 1;
    }

    public bool isNearWumpus()
    {
        return !hasWumpus && (neighbors["up"].hasWumpus || neighbors["down"].hasWumpus || neighbors["upleft"].hasWumpus || neighbors["upright"].hasWumpus || neighbors["downleft"].hasWumpus || neighbors["downright"].hasWumpus);
    }

    public bool isNearPits()
    {
        return (neighbors["up"].hasPit || neighbors["down"].hasPit || neighbors["upleft"].hasPit || neighbors["upright"].hasPit || neighbors["downleft"].hasPit || neighbors["downright"].hasPit);
    }

    public bool isNearBats()
    {
        return (neighbors["up"].hasBat || neighbors["down"].hasBat || neighbors["upleft"].hasBat || neighbors["upright"].hasBat || neighbors["downleft"].hasBat || neighbors["downright"].hasBat);
    }


    // Update is called once per frame
    void Update()
    {
        var renderer = gameObject.GetComponent<SpriteRenderer>();
        if (hasArrow)
        {
            if (hasWumpus)
            {
                renderer.color = Color.black;
            }
            else
            {
                renderer.color = Color.yellow;
            }
        }
        else if (hasPlayer) {
            if (isNearWumpus())
            {
                renderer.color = Color.red;
            }
            else if (hasWumpus)
            {
                renderer.color = Color.black;
            }
            else if (hasPit)
            {
                renderer.color = Color.cyan;
            }
            else if (hasBat)
            {
                renderer.color = Color.magenta;
            }
            else
            {
                renderer.color = Color.green;
            }
        }
        else
        {
            renderer.color = Color.white;
        }
    }
}
