using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FormationGroupController : MonoBehaviour
{
    private List<UnitFormation> _formationsInUse = new List<UnitFormation>();

    public float Spacing = 1;
    public Vector3 GroupPivot;

    public GameObject leftAnchorMarker;
    public GameObject rightAnchorMarker;
    public GameObject pivotMarker;

    public GameObject FormationPrefab;
    public GameObject DebugMarkerPrefab;
    public GameObject MarkerPrefab;


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

        leftAnchorMarker.transform.localScale *= 1.5f;
        rightAnchorMarker.transform.localScale *= 1.5f;
        pivotMarker.transform.localScale *= 1f;
    }

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        leftAnchorMarker.SetActive(IsHidden() ? false : ShowDebugMarkers);
        rightAnchorMarker.SetActive(IsHidden() ? false : ShowDebugMarkers);
        pivotMarker.SetActive(IsHidden() ? false : ShowDebugMarkers);
    }

    public void Reform(Vector3 leftAnchor, Vector3 rightAnchor, List<UnitGroupController> unitsSelected)
    {
        //correct right anchor to make selection granular
        if (Vector3.Distance(leftAnchor, rightAnchor) > GetUnitsMargin())
        {
            Debug.Log(Vector3.Distance(leftAnchor, rightAnchor));

            rightAnchor = Vector3.Lerp(rightAnchor, leftAnchor,
            (Vector3.Distance(leftAnchor, rightAnchor) % GetUnitsMargin()) / Vector3.Distance(leftAnchor, rightAnchor));

            Debug.Log(Vector3.Distance(leftAnchor, rightAnchor));

            //Remove too much frontage
            float maxFrontage = _formationsInUse.Sum(x => x.GetMaxFrontage(GetUnitsMargin())) + (_formationsInUse.Count() - 1) * GetFormationsMargin();
            rightAnchor = Vector3.Lerp(leftAnchor, rightAnchor,
            maxFrontage / Vector3.Distance(leftAnchor, rightAnchor));
        }
        Debug.Log(Vector3.Distance(leftAnchor, rightAnchor));


        //Set debug markers
        leftAnchorMarker.transform.position = leftAnchor;
        rightAnchorMarker.transform.position = rightAnchor;
        pivotMarker.transform.position = Vector3.Lerp(leftAnchor, rightAnchor, 0.5f);

        //Prepare enough formations
        for(int i = _formationsInUse.Count(); i<unitsSelected.Count(); i++)
        {
            _formationsInUse.Add(GetFormationToUse());
        }

        //Reform speific formations
        float totalWeight = _formationsInUse.Sum(x => x.GetMaxFrontage(GetUnitsMargin())) ;
        float totalWidth = Mathf.Max(0.01f, Vector3.Distance(leftAnchor, rightAnchor));
        float unitWidth = Mathf.Max(0.01f, Vector3.Distance(leftAnchor, rightAnchor))-(_formationsInUse.Count() - 1) * GetFormationsMargin();
        float frontageInUse = 0;
        for(int i = 0; i < _formationsInUse.Count();i++)
        {
            float formationWeight = _formationsInUse.ElementAt(i).GetMaxFrontage(GetUnitsMargin()) /totalWeight;
            float leftShift = frontageInUse;
            float rightShift = frontageInUse+(formationWeight*unitWidth)/totalWidth;
            Vector3 leftAnchorShifted = Vector3.Lerp(leftAnchor, rightAnchor, leftShift);
            Vector3 rightAnchorShifted = Vector3.Lerp(leftAnchor, rightAnchor, rightShift);

            _formationsInUse.ElementAt(i).Reform(leftAnchorShifted, rightAnchorShifted, unitsSelected.ElementAt(i), GetUnitsMargin());
            frontageInUse = rightShift + GetFormationsMargin()/totalWidth;
        }

        //float totalWeight = _formationsInUse.Sum(x => x.GetMaxFrontage(GetUnitsMargin())) + (_formationsInUse.Count() - 1) * GetFormationsMargin();
        //float totalWidth = Mathf.Max(0.01f, Vector3.Distance(leftAnchor, rightAnchor));
        //float frontageInUse = 0;
        //for (int i = 0; i < _formationsInUse.Count(); i++)
        //{
        //    float formationWeight = _formationsInUse.ElementAt(i).GetMaxFrontage(GetUnitsMargin()) / totalWeight;
        //    float leftShift = frontageInUse;
        //    float rightShift = frontageInUse + formationWeight;
        //    Vector3 leftAnchorShifted = Vector3.Lerp(leftAnchor, rightAnchor, leftShift);
        //    Vector3 rightAnchorShifted = Vector3.Lerp(leftAnchor, rightAnchor, rightShift);

        //    _formationsInUse.ElementAt(i).Reform(leftAnchorShifted, rightAnchorShifted, unitsSelected.ElementAt(i), GetUnitsMargin());
        //    frontageInUse += formationWeight;
        //    frontageInUse += GetFormationsMargin() / totalWidth;
        //}
    }

    public float GetUnitsMargin()
    {
        return Spacing;
    }

    public float GetFormationsMargin()
    {
        return Spacing * 2;
    }

    public bool IsHidden()
    {
        return _formationsInUse.Count() == 0;
    }

    public void Visualise()
    {
        foreach (UnitFormation form in _formationsInUse)
        {
            form.gameObject.SetActive(true);
            form.Visualise();
        }
    }

    public void Hide()
    {
        int i = 0;
        while (_formationsInUse.Count() > 0)
        {
            _formationsInUse.First().Hide();
            _formationsInUse.First().gameObject.SetActive(false);
            ReturnFormationToPool(_formationsInUse.First());
        }
    }

    public void SendToUnits(List<UnitGroupController> unitsSelected)
    {
        for(int i = 0; i<(int)Mathf.Min(_formationsInUse.Count(), unitsSelected.Count()); i++)
        {
            unitsSelected.ElementAt(i).SetFormation(_formationsInUse.ElementAt(i));
        }
    }

    public void Reset()
    {
        foreach(UnitFormation formation in _formationsInUse)
        {

        }
    }

    #region formation pooling
    private List<UnitFormation> _formationsPool = new List<UnitFormation>();

    public UnitFormation GetFormationToUse()
    {
        UnitFormation toUse = getPooledFormation();
        if (toUse)
        {
            return toUse;
        }
        return createNewFormation();
    }

    private UnitFormation getPooledFormation()
    {
        UnitFormation formation = _formationsPool.Find(x => !x.Attached);
        _formationsPool.Remove(formation);
        return formation;
    }

    private UnitFormation createNewFormation()
    {
        UnitFormation newFormation = GameObject.Instantiate(FormationPrefab, Globals.GetMarkersSpace).GetComponent<UnitFormation>();
        newFormation.Initialize();
        return newFormation;
    }

    public void ReturnFormationToPool(UnitFormation formation)
    {
        _formationsInUse.Remove(formation);
        _formationsPool.Add(formation);
    }
    #endregion

    #region marker pooling
    private List<GameObject> _markersPool = new List<GameObject>();

    public GameObject GetMarkerToUse()
    {
        GameObject toUse = getPooledMarker();
        if (toUse)
        {
            _markersPool.Remove(toUse);
            return toUse;
        }
        return createNewMarker();
    }

    private GameObject getPooledMarker()
    {
        return _markersPool.FirstOrDefault();
    }

    private GameObject createNewMarker()
    {
        return GameObject.Instantiate(MarkerPrefab, Globals.GetMarkersSpace);
    }

    public void ReturnMarkerToPool(GameObject marker)
    {
        _markersPool.Add(marker);
    }
    #endregion
}
