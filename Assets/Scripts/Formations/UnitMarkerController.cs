using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class UnitMarkerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void FadeOut()
    {
        Color newCol = GetComponent<MeshRenderer>().materials.First().color;
        if (newCol.a > 0.01f)
        {
            newCol.a *= 0.8f;
            GetComponent<MeshRenderer>().materials.First().color = newCol;
            Invoke("FadeOut", 0.033f);
        }
        else
        {
            gameObject.SetActive(false);
            Globals.GetFormationGroupController.ReturnMarkerToPool(gameObject);
            ResetFade();
        }
    }

    public void ResetFade()
    {
        Color newCol = GetComponent<MeshRenderer>().materials.First().color;
        newCol.a = 1f;
        GetComponent<MeshRenderer>().materials.First().color = newCol;
    }
}
