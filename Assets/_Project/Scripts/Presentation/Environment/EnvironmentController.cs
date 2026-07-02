/**
 * EnvironmentController: Dong bo anh sang, bau troi, suong mu va muc nuoc theo thoi gian.
 */

using System.Collections;
using UnityEngine;
using ChoNoi.Application;
using ChoNoi.Infrastructure;

namespace ChoNoi.Presentation.Environment
{
    public class EnvironmentController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private EnvironmentProfileSO profile;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Transform waterTransform;
        [SerializeField] private Material runtimeSkyboxMaterial;

        [Header("Transition")]
        [SerializeField] private bool rotateSun = true;
        [SerializeField] private bool enableFog = false;
        [SerializeField] private bool driveAmbientLighting = true;
        [SerializeField] private bool driveSkybox = true;
        [SerializeField] private float sunYaw = 30f;
        [SerializeField] private float transitionSpeed = 6f;

        [Header("Editor Preview")]
        [SerializeField, Range(0f, 24f)] private float editorPreviewHour = 12f;
        [SerializeField] private bool applyInEditor = true;

        private const float NightSkyExposure = 0.00035f;
        private const float DaySkyExposure = 0.0035f;
        private static readonly Color NightSkyTint = new Color(0.08f, 0.12f, 0.22f, 0.5f);
        private static readonly Color DaySkyTint = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        private float displayedHour = 12f;
        private float targetHour = 12f;
        private Coroutine transition;

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnEnable()
        {
            EnsureReferences();
            if (timeManager != null)
            {
                timeManager.OnTimeChanged += HandleTimeChanged;
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.OnTimeChanged -= HandleTimeChanged;
            }

            if (transition != null)
            {
                StopCoroutine(transition);
                transition = null;
            }
        }

        private void HandleTimeChanged(int hour, int minute)
        {
            targetHour = hour + minute / 60f;
            if (transition == null)
            {
                transition = StartCoroutine(TransitionRoutine());
            }
        }

        private IEnumerator TransitionRoutine()
        {
            while (true)
            {
                float deltaDeg = Mathf.DeltaAngle(displayedHour / 24f * 360f, targetHour / 24f * 360f);
                if (Mathf.Abs(deltaDeg) < 0.05f)
                {
                    break;
                }

                float maxStep = transitionSpeed * Time.deltaTime;
                float stepHours = Mathf.Min(maxStep, Mathf.Abs(deltaDeg) / 360f * 24f);
                displayedHour = Mathf.Repeat(displayedHour + Mathf.Sign(deltaDeg) * stepHours, 24f);
                ApplyEnvironment(displayedHour);
                yield return null;
            }

            displayedHour = targetHour;
            ApplyEnvironment(displayedHour);
            transition = null;
        }

        private void ApplyEnvironment(float hour)
        {
            EnsureReferences();
            if (profile == null)
            {
                return;
            }

            float t = Mathf.Repeat(hour, 24f) / 24f;
            float lightIntensity = profile.EvaluateLightIntensity(t);
            Color lightColor = profile.EvaluateLightColor(t);

            if (directionalLight != null)
            {
                directionalLight.color = lightColor;
                directionalLight.intensity = lightIntensity;
                if (rotateSun)
                {
                    directionalLight.transform.rotation = Quaternion.Euler(profile.EvaluateSunPitch(t), sunYaw, 0f);
                }
            }

            if (driveAmbientLighting)
            {
                float ambientFactor = Mathf.Clamp01(Mathf.InverseLerp(0.03f, 0.95f, lightIntensity));
                RenderSettings.ambientIntensity = Mathf.Lerp(0.16f, 1f, ambientFactor);
                RenderSettings.reflectionIntensity = Mathf.Lerp(0.12f, 1f, ambientFactor);
            }

            if (driveSkybox && runtimeSkyboxMaterial != null)
            {
                float skyFactor = Mathf.Clamp01(Mathf.InverseLerp(0.02f, 1f, lightIntensity));

                if (runtimeSkyboxMaterial.HasFloat("_Exposure"))
                {
                    runtimeSkyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(NightSkyExposure, DaySkyExposure, skyFactor));
                }

                if (runtimeSkyboxMaterial.HasColor("_Tint"))
                {
                    Color tint = Color.Lerp(NightSkyTint, DaySkyTint, skyFactor);
                    tint.r *= Mathf.Lerp(0.85f, 1f, lightColor.r);
                    tint.g *= Mathf.Lerp(0.85f, 1f, lightColor.g);
                    tint.b *= Mathf.Lerp(0.9f, 1f, lightColor.b);
                    runtimeSkyboxMaterial.SetColor("_Tint", tint);
                }

                DynamicGI.UpdateEnvironment();
            }

            RenderSettings.fog = enableFog;
            RenderSettings.fogDensity = enableFog ? profile.EvaluateFogDensity(t) : 0f;

            if (waterTransform != null)
            {
                Vector3 position = waterTransform.position;
                position.y = profile.EvaluateWaterHeight(t);
                waterTransform.position = position;
            }
        }

        private void EnsureReferences()
        {
            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeManager>();
            }

            if (directionalLight == null)
            {
                directionalLight = RenderSettings.sun;
                if (directionalLight == null)
                {
                    Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                    foreach (Light sceneLight in sceneLights)
                    {
                        if (sceneLight != null && sceneLight.type == LightType.Directional)
                        {
                            directionalLight = sceneLight;
                            break;
                        }
                    }
                }

                if (directionalLight != null)
                {
                    RenderSettings.sun = directionalLight;
                }
            }

            if (runtimeSkyboxMaterial == null && RenderSettings.skybox != null)
            {
                runtimeSkyboxMaterial = RenderSettings.skybox;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!applyInEditor || UnityEngine.Application.isPlaying)
            {
                return;
            }

            displayedHour = targetHour = editorPreviewHour;
            ApplyEnvironment(editorPreviewHour);
        }
#endif
    }
}
