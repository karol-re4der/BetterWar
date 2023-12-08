using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class UnitFormation:MonoBehaviour
{
    public GameObject leftAnchorMarker;
    public GameObject rightAnchorMarker;
    public GameObject pivotMarker;

    public GameObject DebugMarkerPrefab;

    public int Blanks = 0;
    public int Size = 0;
    public int Rows = 0;
    public int Columns = 0;

    public int MinColumns = 1;
    public int MinRows = 1;

    public Vector3 LeftAnchor;
    public Vector3 RightAnchor;
    public Vector3 FacingDirection;

    public List<Vector3> Positions = new List<Vector3>();
    public UnitController[,] UnitsAttached;
    public List<GameObject> _markersInUse = new List<GameObject>();

    public bool ShowDebugMarkers = false;

    public void Initialize()
    {
        leftAnchorMarker = GameObject.Instantiate(DebugMarkerPrefab, Globals.GetMarkersSpace);
        rightAnchorMarker = GameObject.Instantiate(DebugMarkerPrefab, Globals.GetMarkersSpace);
        pivotMarker = GameObject.Instantiate(DebugMarkerPrefab, Globals.GetMarkersSpace);

        Color randomColor = UnityEngine.Random.ColorHSV();
        leftAnchorMarker.GetComponent<MeshRenderer>().materials[0].color = randomColor;
        rightAnchorMarker.GetComponent<MeshRenderer>().materials[0].color = randomColor;
        pivotMarker.GetComponent<MeshRenderer>().materials[0].color = randomColor;

        leftAnchorMarker.transform.localScale *= 1;
        rightAnchorMarker.transform.localScale *= 1;
        pivotMarker.transform.localScale *= 0.5f;
    }

    void Start()
    {
        
    }

    void Update()
    {
        leftAnchorMarker.SetActive(IsHidden() ? false : ShowDebugMarkers);
        rightAnchorMarker.SetActive(IsHidden() ? false : ShowDebugMarkers);
        pivotMarker.SetActive(IsHidden() ? false : ShowDebugMarkers);
    }

    public bool IsAttached()
    {
        return UnitsAttached != null;
    }

    public void Detach()
    {
        UnitsAttached = null;
    }

    public void Attach()
    {
        UnitsAttached = new UnitController[Columns, Rows];
    }

    public float GetMaxFrontage(float spacing)
    {
        return (Size/MinRows)*spacing;
    }

    public float GetFormationDepth(float spacing)
    {
        return Rows * spacing;
    }

    public void Reform(UnitGroupController unit, float spacing)
    {
        Reform(LeftAnchor, RightAnchor, unit, spacing);
    }

    public void Reform(Vector3 leftAnchor, Vector3 rightAnchor, UnitGroupController unit, float spacing)
    {
        LeftAnchor = leftAnchor;
        RightAnchor = rightAnchor;
        Size = unit.CurrentSize;

        //Set debug markers
        if (ShowDebugMarkers)
        {
            leftAnchorMarker.transform.position = leftAnchor;
            rightAnchorMarker.transform.position = rightAnchor;
            pivotMarker.transform.position = Vector3.Lerp(leftAnchor, rightAnchor, 0.5f);
        }

        if (1 == 1)//Vector3.Distance(LeftAnchorCorrected, rightAnchor)>=sizeUnit)
        {
            //Get facing direction
            FacingDirection = Vector2.Perpendicular(new Vector2(RightAnchor.x, RightAnchor.z) - new Vector2(LeftAnchor.x, LeftAnchor.z));
            FacingDirection.Normalize();
            FacingDirection.z = FacingDirection.y;
            FacingDirection.y = (LeftAnchor.y - RightAnchor.y) / 2;
            FacingDirection.x *= spacing;
            FacingDirection.z *= spacing;

            float width = Vector3.Distance(LeftAnchor, RightAnchor);

            Columns = (int)Mathf.Max(MinColumns, Mathf.Ceil(width / spacing));
            Rows = (int)Mathf.Max(MinRows, Mathf.Ceil((float)Size / Columns));
            Blanks = (Columns * Rows) - Size;

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
                    Vector3 newPos = Vector3.MoveTowards(LeftAnchor, RightAnchor, spacing * xCorrected);

                    //Move in z axis
                    newPos -= FacingDirection * y;

                    if (i == Positions.Count())
                    {
                        Positions.Add(new Vector3());
                    }
                    Positions[i] = newPos;
                }
            }
        }

        while (Positions.Count()>Size)
        {
            Positions.RemoveAt(Size);
        }
    }

    public bool IsHidden()
    {
        return !gameObject.activeSelf;
    }

    public void Hide()
    {
        Update();
        markerFading();
    }

    private void markerFading()
    {
        Update();

        while (_markersInUse.Count() > 0)
        {
            GameObject marker = _markersInUse.First();
            _markersInUse.Remove(marker);
            marker.GetComponent<UnitMarkerController>().FadeOut();
        }
    }

    public void Visualise()
    {
        gameObject.SetActive(true);

        for(int i = 0; i<Positions.Count(); i++)
        {
            if (i == _markersInUse.Count())
            {
                _markersInUse.Add(Globals.GetFormationGroupController.GetMarkerToUse());
            }
            _markersInUse.ElementAt(i).transform.position = Positions.ElementAt(i);
            _markersInUse.ElementAt(i).SetActive(true);
        }
    }

    public bool IsValid()
    {
        return Columns > MinColumns && Rows > MinRows;
    }

    public void AlignUnitsInArray(List<UnitController> units)
    {
        foreach(UnitController unit in units)
        {
            UnitsAttached[(int)unit.PositionInFormation.x, (int)unit.PositionInFormation.y] = unit;
        }
    }

    public Vector3 PositionInFormationToWorldSpace(Vector2 pos)
    {
        int i = 0;

        i = (int)pos.x + (int)pos.y * Columns;

        if ((int)pos.y==Rows && Blanks>0)
        {
            i -= (int)Mathf.Floor(Blanks / 2f);
        }

        if (i < Positions.Count())
        {
            return Positions.ElementAt(i);
        }
        return Vector3.zero;
    }

    public Vector2 WorldSpaceToPositionInFormation(Vector3 pos)
    {
        for(int i = 0; i<Positions.Count(); i++)
        {
            if (Positions[i] == pos)
            {
                float x = i % Columns;
                float y = i / Columns;

                if(y==Rows-1 && Blanks > 0)
                {
                    x += (Mathf.Floor(Blanks / 2f));
                }

                return new Vector2(x, y);
            }
        }

        return Vector2.zero;
    }
}
