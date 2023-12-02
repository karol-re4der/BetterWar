using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public GameObject UnitGroupPrefab;
    public string PlayerName = "Default Player";
    public Color PlayerColor = Color.red;
    public bool IsAI = true;

    public List<UnitGroupController> UnitGroups = new List<UnitGroupController>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public GameObject SpawnUnit(Vector3 position)
    {
        GameObject newUnit = Instantiate(UnitGroupPrefab, position, Quaternion.identity, Globals.GetUnitSpace);
        newUnit.GetComponent<UnitGroupController>().CurrentSize = 100;//Random.Range(1, newUnit.GetComponent<UnitGroupController>().InitialSize);
        newUnit.GetComponent<UnitGroupController>().Initialize(this);
        UnitFormation newFormation = Globals.GetFormationGroupController.GetFormationToUse();
        float marginSize = Globals.GetFormationGroupController.GetUnitsMargin();
        newFormation.Reform(position - Vector3.left * marginSize*4, position + Vector3.left * marginSize * 4, newUnit.GetComponent<UnitGroupController>(), marginSize);
        newUnit.GetComponent<UnitGroupController>().SetFormation(newFormation, true);
        UnitGroups.Add(newUnit.GetComponent<UnitGroupController>());
        if (!IsAI)
        {
            Globals.GetInterface.CreateUnitIcon(newUnit.GetComponent<UnitGroupController>());
        }
        return newUnit;
    }

    public static void SwitchPlayer(PlayerController newPlayer)
    {
        GetActivePlayer().IsAI = true;
        newPlayer.IsAI = false;

        Globals.GetInterface.RecreateUnitIcons();
    }

    public static List<PlayerController> GetAIPlayers()
    {
        return GameObject.Find("Players").GetComponents<PlayerController>().Where(x => x.IsAI).ToList();
    }
    public static List<PlayerController> GetPlayers()
    {
        return GameObject.Find("Players").GetComponents<PlayerController>().ToList();
    }
    public static PlayerController GetActivePlayer()
    {
        return GameObject.Find("Players").GetComponents<PlayerController>().First(x=>x.IsAI==false);
    }
}
