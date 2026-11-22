using UnityEngine;

public sealed class IslandDaylight : MonoBehaviour
{
    private FirstQuotaDay clock;
    private Light sun;
    private Material originalSky, sky;
    private Color sunColor, fog, ambientSky, equator, ground;
    private float strength, exposure, yaw;

    public void Bind(FirstQuotaDay day)
    {
        clock = day;
        sun = RenderSettings.sun;
        if (sun == null) { enabled = false; return; }
        strength = sun.intensity;
        sunColor = sun.color;
        yaw = sun.transform.eulerAngles.y;
        fog = RenderSettings.fogColor;
        ambientSky = RenderSettings.ambientSkyColor;
        equator = RenderSettings.ambientEquatorColor;
        ground = RenderSettings.ambientGroundColor;
        originalSky = RenderSettings.skybox;
        if (originalSky != null && originalSky.HasProperty("_Exposure"))
        {
            sky = new Material(originalSky);
            exposure = sky.GetFloat("_Exposure");
            RenderSettings.skybox = sky;
        }
        Apply(day.DayFraction);
    }

    private void LateUpdate()
    {
        if (clock != null) Apply(clock.DayFraction);
    }

    private void Apply(float day)
    {
        float night = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.52f, .93f, day));
        float dusk = Mathf.Max(0f, 1f - Mathf.Abs(day - .7f) / .18f);
        float height = 22f + 40f * Mathf.Sin(Mathf.PI * day);
        sun.transform.rotation = Quaternion.Euler(height, yaw + 125f * day, 0f);
        sun.intensity = strength * Mathf.Lerp(1f, .34f, night);
        sun.color = Color.Lerp(Color.Lerp(sunColor, new Color(.58f,.70f,1f), night), new Color(1f,.59f,.35f), dusk * .8f);
        RenderSettings.fogColor = Color.Lerp(Color.Lerp(fog, new Color(.16f,.22f,.32f), night), new Color(.63f,.43f,.38f), dusk * .65f);
        RenderSettings.ambientSkyColor = Color.Lerp(ambientSky, new Color(.34f,.40f,.54f), night);
        RenderSettings.ambientEquatorColor = Color.Lerp(equator, new Color(.24f,.29f,.39f), night);
        RenderSettings.ambientGroundColor = Color.Lerp(ground, new Color(.16f,.19f,.25f), night);
        if (sky != null) sky.SetFloat("_Exposure", exposure * Mathf.Lerp(1f,.23f,night));
    }

    private void OnDestroy()
    {
        if (sky == null) return;
        if (RenderSettings.skybox == sky) RenderSettings.skybox = originalSky;
        Destroy(sky);
    }
}