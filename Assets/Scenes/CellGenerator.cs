using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellGenerator : MonoBehaviour
{

    public GameObject hexPrefab;
    public GameObject bridgePrefab;

    public static int gridWidth = 6;
    public static int gridHeight = 5;

    public float hexWidth;
    public float hexHeight;

    public Cell[,] kids = new Cell[gridWidth, gridHeight];

    public Vector2[,] allPositions = new Vector2[gridWidth, gridHeight];

    public string[,] locToCave = new string[gridWidth, gridHeight];

    public int wumpusnum = 0;

    public Cell wumpus;
    public List<Cell> bats = new List<Cell>();
    public List<Cell> pits = new List<Cell>();

    public int bat1 = 0;
    public int bat2 = 0;
    public int cave1 = 0;
    public int cave2 = 0;

    public Sprite upright;
    public Sprite upleft;
    public Sprite downleft;
    public Sprite downright;

    private System.Random rnd = new System.Random();

    private void Start()
    {
        wumpusnum = new System.Random().Next(30)+1;
        GenerateGrid();
    }

    public void makeDisappear()
    {
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                kids[q, r].gameObject.SetActive(false);
            }
        }
    }

    public void makeAppear()
    {

        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                kids[q, r].gameObject.SetActive(true);
            }
        }
    }
    
    public void moveWumpus()
    {
        wumpus.hasWumpus = false;
        wumpusnum = new System.Random().Next(30) + 1;
        int cnt = 1;
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                if (cnt == wumpusnum)
                {
                    kids[q, r].hasWumpus = true;
                    wumpus = kids[q, r];
                }
            }
        }

    }

    public void moveWumpusAdj()
    {
        wumpus.hasWumpus = false;
        int dir = new System.Random().Next(6);
        if (dir == 0)
        {
            wumpus.next["up"].hasWumpus = true;
            wumpus = wumpus.next["up"];
        }
        else if (dir == 1)
        {
            wumpus.next["upleft"].hasWumpus = true;
            wumpus = wumpus.next["upleft"];
        }
        else if (dir == 2)
        {
            wumpus.next["downleft"].hasWumpus = true;
            wumpus = wumpus.next["downleft"];
        }
        else if (dir == 3)
        {
            wumpus.next["upright"].hasWumpus = true;
            wumpus = wumpus.next["upright"];
        }
        else if (dir == 4)
        {
            wumpus.next["downright"].hasWumpus = true;
            wumpus = wumpus.next["downright"];
        }
        else
        {
            wumpus.next["down"].hasWumpus = true;
            wumpus = wumpus.next["down"];
        }
    }

    void GenerateNeighbors()
    {
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                kids[q, r].numConnections += 2;
                kids[q, r].neighbors["up"] = kids[q, (r - 1 + gridHeight) % gridHeight];
                kids[q, r].neighbors["down"] = kids[q, (r + 1) % gridHeight];
            }
        }
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                if (q % 2 == 0)
                {
                    kids[q, r].next.Add("upright", kids[(q + 1) % gridWidth, (r - 1 + gridHeight) % gridHeight]);
                    kids[q, r].next.Add("downright", kids[(q + 1) % gridWidth, r]);
                    kids[q, r].next.Add("upleft", kids[(q - 1 + gridWidth) % gridWidth, (r - 1 + gridHeight) % gridHeight]);
                    kids[q, r].next.Add("downleft", kids[(q - 1 + gridWidth) % gridWidth, r]);
                }
                else
                {
                    kids[q, r].next.Add("upright", kids[(q + 1) % gridWidth, r]);
                    kids[q, r].next.Add("downright", kids[(q + 1) % gridWidth, (r + 1) % gridHeight]);
                    kids[q, r].next.Add("upleft", kids[(q - 1 + gridWidth) % gridWidth, r]);
                    kids[q, r].next.Add("downleft", kids[(q - 1 + gridWidth) % gridWidth, (r + 1) % gridHeight]);
                }
                kids[q, r].next.Add("up", kids[q, (r - 1 + gridHeight) % gridHeight]);
                kids[q, r].next.Add("down", kids[q, (r + 1) % gridHeight]);
            }
        }
        GenerateRandomConnections();
    }

    void GenerateRandomConnections()
    {
        for (int q = 0; q < gridWidth; q++)
        {
            bool works = true;
            while (works)
            {
                works = false;
                int r = new System.Random().Next(gridHeight);
                while (kids[q, r].numConnections >= 3)
                {
                    r = new System.Random().Next(gridHeight);
                }
                //different cases for odd and even columns
                if (q % 2 == 0)
                {
                    //randomly choose a cell, then connect it to it's top right cell or bottom right cell
                    if (new System.Random().Next(2) == 0 && kids[(q + 1) % gridWidth, (r - 1 + gridHeight) % gridHeight].numConnections < 3)
                    {
                        kids[(q + 1) % gridWidth, (r - 1 + gridHeight) % gridHeight].numConnections++;
                        kids[q, r].numConnections++;
                        kids[q, r].neighbors["upright"] = kids[(q + 1) % gridWidth, (r - 1 + gridHeight) % gridHeight];
                        kids[(q + 1) % gridWidth, (r - 1 + gridHeight) % gridHeight].neighbors["downleft"] = kids[q, r];
                        kids[q, r].gameObject.GetComponent<SpriteRenderer>().sprite = upright;
                        kids[(q + 1) % gridWidth, (r - 1 + gridHeight) % gridHeight].gameObject.GetComponent<SpriteRenderer>().sprite = downleft;
                    }
                    //if the top right doesn't work, choose the bottom right
                    else if (kids[(q + 1) % gridWidth, r].numConnections < 3)
                    {
                        kids[(q + 1) % gridWidth, r].numConnections++;
                        kids[q, r].numConnections++;
                        kids[q, r].neighbors["downright"] = kids[(q + 1) % gridWidth, r];
                        kids[(q + 1) % gridWidth, r].neighbors["upleft"] = kids[q, r];
                        kids[q, r].gameObject.GetComponent<SpriteRenderer>().sprite = downright;
                        kids[(q + 1) % gridWidth, r].gameObject.GetComponent<SpriteRenderer>().sprite = upleft;
                    }
                    //if the given cell can't connect to anything else, repeat the process again
                    else
                    {
                        works = true;
                    }
                }
                else
                {
                    //repeat for odd column numbers
                    if (new System.Random().Next(2) == 0 && kids[(q + 1) % gridWidth, r].numConnections < 3)
                    {
                        kids[(q + 1) % gridWidth, r].numConnections++;
                        kids[q, r].numConnections++;
                        kids[q, r].neighbors["upright"] = kids[(q + 1) % gridWidth, r];
                        kids[(q + 1) % gridWidth, r].neighbors["downleft"] = kids[q, r];
                        kids[q, r].gameObject.GetComponent<SpriteRenderer>().sprite = upright;
                        kids[(q + 1) % gridWidth, r].gameObject.GetComponent<SpriteRenderer>().sprite = downleft;
                    }
                    else if (kids[(q + 1) % gridWidth, (r + 1) % gridHeight].numConnections < 3)
                    {
                        kids[(q + 1) % gridWidth, (r + 1) % gridHeight].numConnections++;
                        kids[q, r].numConnections++;
                        kids[q, r].neighbors["downright"] = kids[(q + 1) % gridWidth, (r + 1) % gridHeight];
                        kids[(q + 1) % gridWidth, (r + 1) % gridHeight].neighbors["upleft"] = kids[q, r];
                        kids[q, r].gameObject.GetComponent<SpriteRenderer>().sprite = downright;
                        kids[(q + 1) % gridWidth, (r + 1) % gridHeight].gameObject.GetComponent<SpriteRenderer>().sprite = upleft;
                    }
                    else
                    {
                        works = true;
                    }
                }
            }
        }
    }
    void GenerateGrid()
    {
        int cnt = 1;
        //generate the grid and set up neighbors of each cell
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                //calculate the x/y of the generated cell
                float xPos = (q+1) * hexWidth * 0.75f - 3.5f;
                float yPos = -1 * r * hexHeight + (q % 2 == 1 ? 0 : hexHeight / 2) - 4.5f;
                //Make the cell and add it to the 2D array
                GameObject hexGO = Instantiate(hexPrefab, new Vector3(xPos, yPos, 0), Quaternion.identity);
                hexGO.transform.parent = this.transform;
                var cell = hexGO.GetComponent<Cell>();
                kids[q, r] = cell;
                cell.i = q;
                cell.j = r;
                //initialize neighbors to itself
                kids[q, r].neighbors.Add("up", kids[q, r]);
                kids[q, r].neighbors.Add("upleft", kids[q, r]);
                kids[q, r].neighbors.Add("upright", kids[q, r]);
                kids[q, r].neighbors.Add("down", kids[q, r]);
                kids[q, r].neighbors.Add("downleft", kids[q, r]);
                kids[q, r].neighbors.Add("downright", kids[q, r]);

                if (cell.GetCellIndex() == wumpusnum)
                {
                    cell.hasWumpus = true;
                    wumpus = cell;
                }

                allPositions[q, r] = new Vector2(xPos, yPos);
                if (cnt < 10) {
                    locToCave[q, r] = "Cave_0" + cnt;
                }
                else {
                    locToCave[q, r] = "Cave_" + cnt;
                }
                cnt++;
            }
        }

        GenerateNeighbors();
        SetHazards();
        kids[0, 0].hasPlayer = true;
    }
    public void SetHazards()
    {
        // clear up previous pits and bats
        int counter = 1;
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                if (cave1 == counter || cave2 == counter)
                {
                    kids[q, r].hasPit = false;
                }
                else if (bat1 == counter || bat2 == counter)
                {
                    kids[q, r].hasBat = false;
                }
                counter++;
            }
        }
        pits.Clear();
        bats.Clear();

        // regenerate pits and bats
        cave1 = rnd.Next(29)+2;
        cave2 = cave1;
        while (cave2 == cave1)
        {
            cave2 = rnd.Next(29)+2;
        }
        bat1 = cave1;
        while (bat1 == cave1 || bat1 == cave2)
        {
            bat1 = rnd.Next(29)+2;
        }
        bat2 = bat1;
        while (bat2 == cave1 || bat2 == cave2 || bat2 == bat1)
        {
            bat2 = rnd.Next(29)+2;
        }
        counter = 1;
        for (int q = 0; q < gridWidth; q++)
        {
            for (int r = 0; r < gridHeight; r++)
            {
                if (cave1 == counter || cave2 == counter)
                {
                    kids[q, r].hasPit = true;
                    pits.Add(kids[q, r]);
                }
                else if (bat1 == counter || bat2 == counter)
                {
                    kids[q, r].hasBat = true;
                    bats.Add(kids[q, r]);
                }
                counter++;
            }
        }
    }
}
