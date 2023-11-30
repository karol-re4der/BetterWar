using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ProjectileSystem : MonoBehaviour
{
    private List<ProjectileController> projectiles = new List<ProjectileController>();
    private List<GameObject> smokes = new List<GameObject>();
    public GameObject SmokePrefab;
    public GameObject ProjectilePrefab;

    public bool CreateSmoke = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void NewProjectileAtPoint(Vector3 from, Vector3 to, bool addSmoke)
    {
        ProjectileController pooledProjectile = projectiles.Find(x => !x.IsActive);
        if (pooledProjectile == null)
        {
            Globals.GetStats.RegisterEvent("ProjectilesInPool", 1);

            pooledProjectile = GameObject.Instantiate(ProjectilePrefab, Globals.GetProjectileSpace).GetComponent<ProjectileController>();
            projectiles.Add(pooledProjectile);
        }

        if (CreateSmoke)
        {
            Vector3 smokePos = Vector3.MoveTowards(from, to, 1f);
            if (addSmoke) AddSmoke(smokePos);
        }
        
        pooledProjectile.LaunchAndHit(from, to);
    }

    public void NewProjectileInDirection(Vector3 from, Vector3 direction, bool addSmoke)
    {
        ProjectileController pooledProjectile = projectiles.Find(x => !x.IsActive);
        if (pooledProjectile == null)
        {
            Globals.GetStats.RegisterEvent("ProjectilesInPool", 1);

            pooledProjectile = GameObject.Instantiate(ProjectilePrefab, Globals.GetProjectileSpace).GetComponent<ProjectileController>();
            projectiles.Add(pooledProjectile);
        }

        if (CreateSmoke)
        {
            Vector3 smokePos = Vector3.MoveTowards(from, direction, 1f);
            if (addSmoke) AddSmoke(smokePos);
        }
        pooledProjectile.LaunchInDirection(from, direction);
    }

    public float GetTimeToHit(Vector3 from, Vector3 to)
    {
        return Vector3.Distance(from, to) / ProjectilePrefab.GetComponent<ProjectileController>().Speed;
    }

    public void AddSmoke(Vector3 pos)
    {
        GameObject pooledSmoke = smokes.Find(x => !x.GetComponent<ParticleSystem>().isPlaying);
        if (pooledSmoke == null)
        {
            Globals.GetStats.RegisterEvent("SmokesInPool", 1);
            pooledSmoke = GameObject.Instantiate(SmokePrefab, pos, Quaternion.identity, Globals.GetEffectsSpace);
            smokes.Add(pooledSmoke);
        }
        else
        {
            pooledSmoke.transform.position = pos;
            pooledSmoke.GetComponent<ParticleSystem>().Play();
        }
    }
}
