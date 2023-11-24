using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class UnitFormation:MonoBehaviour
{
    public int debug;

    public GameObject MarkerPrefab;
    public static GameObject FormationPrefab;

    public List<Vector3> Positions = new List<Vector3>();
    public List<GameObject> Markers = new List<GameObject>();

    public float MinFrontage = 0;
    public float MaxFrontage = 0;
    public int Blanks = 0;
    public int Size = 0;
    public int Rows = 0;
    public int Columns = 0;
    public float Spacing = 0;
    public float UnitSize = 1;
    public Vector3 LeftAnchor;
    public Vector3 LeftAnchorCorrected;
    public Vector3 RightAnchor;
    public Vector3 FacingDirection;
    public Vector3 WeightCenter;
    public bool Computed = false;
    public bool Attached = false;

    public void Start()
    {
        
    }

    public void Initialize(int size, float spacing, Vector3 leftAnchor)
    {
        Hide();
        Computed = false;
        Attached = false;

        Size = size;
        Spacing = spacing;
        LeftAnchor = leftAnchor;

        float sizeUnit = UnitSize + Spacing;
        MaxFrontage = sizeUnit * Size;
        MinFrontage = sizeUnit * 2; //2 columns min


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
        Recompute(rightAnchor, 0);
    }
    public void Recompute(Vector3 rightAnchor, float offset)
    {
        if (offset > 0)
        {
            LeftAnchorCorrected = Vector3.MoveTowards(LeftAnchor, rightAnchor, offset);
        }
        else
        {
            LeftAnchorCorrected = LeftAnchor;
        }

        float sizeUnit = UnitSize + Spacing;

        if (1==1)//Vector3.Distance(LeftAnchorCorrected, rightAnchor)>=sizeUnit)
        {
            Debug.Log(debug+" recomped");
            RightAnchor = rightAnchor;

            WeightCenter = Vector3.Lerp(LeftAnchorCorrected, RightAnchor, 0.5f);

            FacingDirection = Vector2.Perpendicular(new Vector2(RightAnchor.x, RightAnchor.z) - new Vector2(LeftAnchorCorrected.x, LeftAnchorCorrected.z));
            FacingDirection.Normalize();
            FacingDirection.z = FacingDirection.y;
            FacingDirection.y = (LeftAnchorCorrected.y - RightAnchor.y) / 2;
            FacingDirection.x *= UnitSize + Spacing;
            FacingDirection.z *= UnitSize + Spacing;

            float width = Mathf.Abs(Vector3.Distance(LeftAnchorCorrected, RightAnchor));

            try
            {
                Columns = (int)Mathf.Min(Size, (width / sizeUnit) + 1);
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
                        Vector3 newPos = Vector3.MoveTowards(LeftAnchorCorrected, RightAnchor, sizeUnit * xCorrected);

                        //Move in z axis
                        Vector3 direction = Vector2.Perpendicular(new Vector2(LeftAnchorCorrected.x, LeftAnchorCorrected.z) - new Vector2(RightAnchor.x, RightAnchor.z));
                        direction.Normalize();
                        direction.z = direction.y;
                        direction.y = (LeftAnchorCorrected.y - RightAnchor.y) / 2;
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
        Debug.Log(debug + " shown");

        for (int i = 0; i<Positions.Count(); i++)
        {
            GameObject marker = i<Markers.Count() ?Markers.ElementAt(i):NewMarker();
            marker.SetActive(true);
            marker.transform.position = Positions[i];
        }
    }

    public bool IsHidden()
    {
        return (Markers.Count>0)?!Markers.First().activeSelf:true;
    }

    public void Hide()
    {
        Debug.Log(debug + " hidden");
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

    private GameObject NewMarker()
    {
        GameObject marker = GameObject.Instantiate(MarkerPrefab, transform);
        Markers.Add(marker);
        return marker;
    }

    public static List<UnitFormation> GetFormationsToUse(int formationsRequested)
    {
        List<UnitFormation> formations = new List<UnitFormation>();
        foreach (Transform formation in Globals.GetMarkersSpace.transform)
        {
            if (!formation.gameObject.GetComponent<UnitFormation>().Attached && !formations.Contains(formation.gameObject.GetComponent<UnitFormation>()))
            {
                formations.Add(formation.gameObject.GetComponent<UnitFormation>());
                if (formations.Count() == formationsRequested)
                {
                    break;
                }
            }
        }
        
        for(int i = formations.Count(); i<formationsRequested; i++)
        { 
            formations.Add(GameObject.Instantiate((GameObject)Resources.Load("Prefabs/Abstract Objects/FormationPrefab", typeof(GameObject)), Globals.GetMarkersSpace).GetComponent<UnitFormation>());
        }

        return formations;
    }
}
