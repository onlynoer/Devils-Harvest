using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools ▸ Audio ▸ Assign Generated SFX: fills every EMPTY sound slot on WeaponData and PowerupData assets with
/// the placeholder sounds in Assets/Audio/SFX, picked by weapon type. Slots that already have clips are left
/// alone, so it's safe to run again after replacing sounds.
/// </summary>
internal static class GeneratedSfxAssigner
{
    private const string Root = "Assets/Audio/SFX/";

    [MenuItem("Tools/Audio/Assign Generated SFX")]
    private static void Assign()
    {
        int filled = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponData"))
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
            if (weapon == null) continue;
            int before = filled;
            switch (weapon.useType)
            {
                case WeaponUseType.Gun:
                    bool heavy = weapon.name.Contains("Barrett");
                    filled += Fill(weapon.useSound, heavy
                        ? new[] { "Weapons/Guns/gun_shot_heavy" }
                        : new[] { "Weapons/Guns/gun_shot_1", "Weapons/Guns/gun_shot_2", "Weapons/Guns/gun_shot_3" });
                    filled += Fill(weapon.emptySound, "Weapons/Guns/gun_dry_fire");
                    filled += Fill(weapon.reloadSound, "Weapons/Guns/gun_reload");
                    filled += Fill(weapon.equipSound, "Weapons/Guns/gun_equip");
                    break;
                case WeaponUseType.Bow:
                    filled += Fill(weapon.useSound, "Weapons/Bow/bow_release");
                    filled += Fill(weapon.emptySound, "Weapons/Guns/gun_dry_fire");
                    filled += Fill(weapon.reloadSound, "Weapons/Bow/bow_reload");
                    filled += Fill(weapon.equipSound, "Weapons/Bow/bow_equip");
                    break;
                case WeaponUseType.Staff:
                    filled += Fill(weapon.useSound, "Weapons/Staff/staff_cast");
                    filled += Fill(weapon.emptySound, "Weapons/Staff/staff_fizzle");
                    filled += Fill(weapon.reloadSound, "Weapons/Staff/staff_recharge");
                    filled += Fill(weapon.equipSound, "Weapons/Bow/bow_equip");
                    break;
                case WeaponUseType.Melee:
                    filled += Fill(weapon.useSound, "Weapons/Melee/sword_swing_1", "Weapons/Melee/sword_swing_2", "Weapons/Melee/sword_swing_3");
                    filled += Fill(weapon.hitSound, "Weapons/Melee/sword_hit");
                    filled += Fill(weapon.equipSound, "Weapons/Melee/sword_equip");
                    break;
            }
            if (filled != before) EditorUtility.SetDirty(weapon);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:PowerupData"))
        {
            var powerup = AssetDatabase.LoadAssetAtPath<PowerupData>(AssetDatabase.GUIDToAssetPath(guid));
            if (powerup == null) continue;
            int before = filled;
            filled += Fill(powerup.collectSound, "Powerups/powerup_collect");
            filled += Fill(powerup.expireSound, "Powerups/powerup_expire");
            if (filled != before) EditorUtility.SetDirty(powerup);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Assign Generated SFX: filled " + filled + " empty sound slot(s).");
    }

    private static int Fill(SoundEffect sound, params string[] names)
    {
        if (sound == null || sound.HasClip) return 0;
        var clips = new System.Collections.Generic.List<AudioClip>();
        foreach (string name in names)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + name + ".wav");
            if (clip != null) clips.Add(clip);
            else Debug.LogWarning("Assign Generated SFX: missing " + Root + name + ".wav");
        }
        if (clips.Count == 0) return 0;
        sound.clips = clips.ToArray();
        return 1;
    }
}
