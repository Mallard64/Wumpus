using System.Collections.Generic;
using UnityEngine;

public class TMP : MonoBehaviour
{
    public GameObject hexPrefab;
    public GameObject bridgePrefab;

    public static int gridWidth = 6;
    public static int gridHeight = 6;

    public float hexWidth;
    public float hexHeight;

    public GameObject[,] kids = new GameObject[gridWidth, gridHeight];

    public Vector2[,] allPositions = new Vector2[gridWidth, gridHeight];

    public string[,] locToCave = new string[gridWidth, gridHeight];

    public int wumpusnum = 0;

    public int wi;
    public int wj;

    private void Start()
    {
        wumpusnum = new System.Random().Next(31);
        GenerateGrid();
    }

    void GenerateGrid()
    {
        int cnt = 1;
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                float xPos = q * hexWidth * 0.75f - 3.5f;
                float yPos = r * hexHeight + (q % 2 == 0 ? 0 : hexHeight / 2) - 4.0f;

                GameObject hexGO = Instantiate(hexPrefab, new Vector3(xPos, yPos, 0), Quaternion.identity);
                if (cnt == wumpusnum) {
                    hexGO.GetComponent<Cell>().hasWumpus = true;
                    wi = q;
                    wj = r;
                }
                hexGO.transform.parent = this.transform;
                kids[q, r] = hexGO;
                hexGO.GetComponent<Cell>().i = q;
                hexGO.GetComponent<Cell>().j = r;

                allPositions[q,r] = new Vector2(xPos, yPos);
                if (cnt < 10) {
                    locToCave[q,r] = "Cave_0" + cnt;
                }
                else {
                    locToCave[q,r] = "Cave_" + cnt;
                }
                cnt++;
            }
        }
        for (int q = 0; q < gridWidth; q++) {
            for (int r = 0; r < gridHeight; r++) {
                kids[q,r].GetComponent<Cell>().wumi = wi;
                kids[q,r].GetComponent<Cell>().wumj = wj;
            }
        }
    }

    
}
