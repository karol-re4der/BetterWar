using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class UnitFormation:MonoBehaviour
{
    public GameObject MarkerPrefab;
    public List<Vector3> Positions = new List<Vector3>();
    public List<GameObject> Markers = new List<GameObject>();

    public int Blanks = 0;
    public int Size = 0;
    public int Rows = 0;
    public int Columns = 0;
    public float Spacing = 0;
    public float UnitSize = 1;
    public Vector3 LeftAnchor;
    public Vector3 RightAnchor;
    public Vector3 FacingDirection;
    public Vector3 WeightCenter;
    public bool Computed = false;

    private int _previousColumns = 0;


    public void Start()
    {
        
    }

    public void Initialize(int size, float spacing, Vector3 leftAnchor)
    {
        Hide();
        Computed = false;

        Size = size;
        Spacing = spacing;
        LeftAnchor = leftAnchor;

        Positions.Clear();
        for (int i = Positions.Count(); i < size; i++)
        {
            Positions.Add(Vector3.zero);
        }
    }

    //Use when recomputing around single anchor point
    public void Recompute()
    {
        float sizeUnit = UnitSize + Spacing;
        float width = Mathf.Abs(Vector3.Distance(Positions[0], Positions[Columns-1]));

        Vector3 direction = Vector2.Perpendicular(new Vector2(WeightCenter.x, WeightCenter.z) - new Vector2(LeftAnchor.x, LeftAnchor.z));
        direction.Normalize();
        direction.z = direction.y;
        direction.y = (LeftAnchor.y - RightAnchor.y) / 2;
        direction.x *= width / 2;
        direction.z *= width / 2;

        RightAnchor = LeftAnchor + direction;
        LeftAnchor = LeftAnchor - direction;


        Recompute(RightAnchor);
    }
    public void Recompute(Vector3 rightAnchor)
    {
        float sizeUnit = UnitSize + Spacing;

        if (Vector3.Distance(LeftAnchor, rightAnchor)>=sizeUnit)
        {
            RightAnchor = rightAnchor;

            WeightCenter = Vector3.Lerp(LeftAnchor, RightAnchor, 0.5f);

            FacingDirection = Vector2.Perpendicular(new Vector2(RightAnchor.x, RightAnchor.z) - new Vector2(LeftAnchor.x, LeftAnchor.z));
            FacingDirection.Normalize();
            FacingDirection.z = FacingDirection.y;
            FacingDirection.y = (LeftAnchor.y - RightAnchor.y) / 2;
            FacingDirection.x *= UnitSize + Spacing;
            FacingDirection.z *= UnitSize + Spacing;

            float width = Mathf.Abs(Vector3.Distance(LeftAnchor, RightAnchor));

            try
            {
                Columns = (int)(width / sizeUnit) + 1;
                Rows = (int)Mathf.Ceil((float)Size / Columns);
                Blanks = (Rows > 1) ? (Columns * Rows - Size) : 0;
            }
            catch (ArithmeticException ex)
            {
                Columns = 0;
                Rows = 0;
                Blanks = 0;
            }


            if (Rows > 0 && Columns > 0)
            {
                Computed = true;
                for (int y = 0; y < Rows; y++)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        int i = y * Columns + x;
                        int xCorrected = (Blanks > 0 && y == Rows - 1) ? (x + Blanks / 2) : x;
                        if (i >= Size)
                        {
                            break;
                        }

                        //Move in x axis
                        Vector3 newPos = Vector3.MoveTowards(LeftAnchor, RightAnchor, sizeUnit * xCorrected);

                        //Move in z axis
                        Vector3 direction = Vector2.Perpendicular(new Vector2(LeftAnchor.x, LeftAnchor.z) - new Vector2(RightAnchor.x, RightAnchor.z));
                        direction.Normalize();
                        direction.z = direction.y;
                        direction.y = (LeftAnchor.y - RightAnchor.y) / 2;
                        direction.x *= UnitSize + Spacing;
                        direction.z *= UnitSize + Spacing;
                        newPos += direction * y;
                        Positions[i] = newPos;
                    }
                }
            }
        }
    }

    public void Visualise()
    {
        for(int i = 0; i<Positions.Count(); i++)
        {
            if (i == Markers.Count())
            {
                GameObject marker = GameObject.Instantiate(MarkerPrefab, Positions[i], Quaternion.identity, transform);
                Markers.Add(marker);
            }
            else
            {
                Markers.ElementAt(i).SetActive(true);
                Markers.ElementAt(i).transform.position = Positions[i];
            }
        }
    }

    public void Hide()
    {
        foreach (GameObject marker in Markers)
        {
            marker.SetActive(false);
        }
    }

    public bool IsShootingPosition(int i)
    {
        if(Blanks>0 && i >= Columns * (Rows - 1))
        {
            return false;
        }
        if (i < Columns || i % Columns == 0 || i % Columns == Columns-1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
