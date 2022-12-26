using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Targeted : MonoBehaviour
{
    public static List<Targeted> ENEMY_TARGETS = new List<Targeted>(), TURRET_TARGETS = new List<Targeted>();
    public enum targetedtype { EnemyTarget, TurretTarget }
    public targetedtype TargetType;

    private void OnEnable()
    {
        if (TargetType == targetedtype.EnemyTarget) { ENEMY_TARGETS.Add(this); }
        if (TargetType == targetedtype.TurretTarget) { TURRET_TARGETS.Add(this); }
    }

    private void OnDisable()
    {
        if (TargetType == targetedtype.EnemyTarget) { ENEMY_TARGETS.Remove(this); }
        if (TargetType == targetedtype.TurretTarget) { TURRET_TARGETS.Remove(this); }
    }
}
