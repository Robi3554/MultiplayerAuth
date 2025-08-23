using UnityEngine;

[CreateAssetMenu(fileName = "BaseMeleeWeapon", menuName = "Scriptable Objects/BaseMeleeWeapon")]
public class BaseMeleeWeapon : ScriptableObject
{
    [Header("Melee Weapon Stats")]
    public float CooldownTime = 1f;
    public float AttackRange = 3f;
    public int Damage = 10;
}
