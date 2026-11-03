using UnityEngine;

namespace SibJam.Weapons
{
    // Выдать игроку оружие вызовом без параметров — например «Действием» Equip на таймлайне катсцены
    [RequireComponent(typeof(MeleeWeapon))]
    public class WeaponEquipper : MonoBehaviour
    {
        public void Equip() => GetComponent<MeleeWeapon>().enabled = true;
    }
}
