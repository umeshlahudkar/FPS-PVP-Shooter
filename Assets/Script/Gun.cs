
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private bool isAutomatic;
    [SerializeField] private float timeBetweenShot = 0.1f;
    [SerializeField] private float heatPerShot = 1f;
    [SerializeField] private GameObject muzzleFlash;
    [SerializeField] private int shotDamage;
    [SerializeField] private AudioSource shotSound;

    public bool IsMuzzleFlashActive()
    {
        return muzzleFlash.activeInHierarchy;
    }

    public void ToggleMuzzleFlash(bool status)
    {
        muzzleFlash.SetActive(status);
    }

    public bool IsAutomatic()
    {
        return isAutomatic;
    }

    public int GetShotDamage()
    {
        return shotDamage;
    }

    public float GetTimeBetweenShot() { return timeBetweenShot; }

    public float GetHeatPerShot() { return heatPerShot; }

    public void PlaySound() { shotSound.Play(); }

    public void StopSound() { shotSound.Stop(); }
}
