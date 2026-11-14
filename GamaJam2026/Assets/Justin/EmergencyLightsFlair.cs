using UnityEngine;

public class EmergencyLightsFlair : MonoBehaviour
{
    [SerializeField] Renderer lightRenderer;
    [SerializeField] Light flairMatch;
    [SerializeField] float speed = 2f;
    [SerializeField] float maxEmission = 4f;
    [SerializeField] float maxLight = 3f;

    [SerializeField] float minPulse = 0.3f;
    MaterialPropertyBlock mpb;

    void Start() => mpb = new MaterialPropertyBlock();

    void Update()
    {
         float wave = Mathf.Abs(Mathf.Sin(Time.time * speed));
        float pulse = Mathf.Lerp(minPulse, 1f, wave);

        lightRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_EmissionColor", Color.red * maxEmission * pulse);
        lightRenderer.SetPropertyBlock(mpb);

        flairMatch.intensity = maxLight * pulse;
    }
}