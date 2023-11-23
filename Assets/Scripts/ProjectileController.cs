using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileController:MonoBehaviour
{
    private float distanceTravelled = 0;
    private Vector3 from;
    private Vector3 to;
    private Vector3 dir;
    private float range;

    [Header("Settings")]
    public float Speed;
    public float MaxRange;
    public GameObject ProjectileModel;
    public bool IsActive = false;

    public void LaunchAndHit(Vector3 from, Vector3 to)
    {
        ProjectileModel.SetActive(true);
        this.from = from;
        transform.position = from;
        this.to = to;
        distanceTravelled = 0;
        dir = (to - from).normalized;
        range = Mathf.Abs(Vector3.Distance(from, to));

        IsActive = true;
    }

    public void LaunchInDirection(Vector3 from, Vector3 direction)
    {
        ProjectileModel.SetActive(true);
        this.from = from;
        transform.position = from;
        this.to = to;
        distanceTravelled = 0;
        dir = direction;
        range = MaxRange;

        IsActive = true;
    }

    void Start()
    {

    }

    void Update()
    {
        if (IsActive)
        {
            float dist = Time.deltaTime * Speed;
            transform.position += dir * dist;
            distanceTravelled += dist;

            if (distanceTravelled > range)
            {
                ProjectileModel.SetActive(false);
                IsActive = false;
            }
        }
    }
}
