using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wumpus : MonoBehaviour
{
    public string caveName;
    void Awake()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("wumpus");

        if (objs.Length > 1)
        {
            Destroy(this.gameObject);
        }

        DontDestroyOnLoad(this.gameObject);
    }
    // Start is called before the first frame update
    void Start()
    {
        SpriteRenderer s = GetComponent<SpriteRenderer>();
        s.enabled = false;
        System.Random r = new System.Random();
        int p = (r.Next(29) + 2);
        string ss;
        if (p < 10) {
            ss = "0" + p;
        }
        else {
            ss = p + "";
        }
        caveName = "Cave_" + ss;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
