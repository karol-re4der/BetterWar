using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Globals
{
    public static UserControlsController GetUserControls
    {
        get
        {
            return GameObject.Find("Main Camera").GetComponent<UserControlsController>();
        }
    }

    public static InterfaceController GetInterface
    {
        get
        {
            return GameObject.Find("UserInterface").GetComponent<InterfaceController>();
        }
    }

    public static Transform GetUnitSpace
    {
        get
        {
            return GameObject.Find("Map/Units").transform;
        }
    }

    public static Transform GetMarkersSpace
    {
        get
        {
            return GameObject.Find("Map/Markers").transform;
        }
    }

    public static PlayerController GetActivePlayer
    {
        get
        {
            foreach(PlayerController player in GameObject.Find("Players").GetComponents<PlayerController>())
            {
                if (!player.IsAI)
                {
                    return player;
                }
            }
            return null;
        }
    }

    public static ProjectileSystem GetProjectileSystem
    {
        get
        {
            return GameObject.Find("Map/Projectiles").GetComponent<ProjectileSystem>();
        }
    }

    public static Transform GetProjectileSpace
    {
        get
        {
            return GameObject.Find("Map/Projectiles").transform;
        }
    }

    public static Transform GetEffectsSpace
    {
        get
        {
            return GameObject.Find("Map/Effects").transform;
        }
    }

    public static FormationGroupController GetFormationGroupController
    {
        get
        {
            return GameObject.Find("Map/Markers").GetComponent<FormationGroupController>();
        }
    }
}
