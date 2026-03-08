using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages visual life cards for a single participant.
/// Attach one instance per participant (for example player and house).
/// </summary>
public class UI_LifeCardsView : MonoBehaviour
{
    private const float BurnParticleLifetime = 3f;

    #region Serialized Fields

    [Header("Card Prefab")]
    [SerializeField] private GameObject lifeCardPrefab;

    [Header("Anchor")]
    [SerializeField] private Transform livesAnchor;

    [Header("Layout")]
    [SerializeField] private Vector3 overlapDirection = Vector3.right;
    [SerializeField] private float overlapDistance = 0.25f;
    [SerializeField] private bool centerCards = true;

    [Header("Card Rotation")]
    [SerializeField] private float splayAnglePerCard = 5f;
    [SerializeField] private float cardTiltAngle = 15f;
    [SerializeField] private float verticalOffsetPerCard = 0.05f;

    [Header("Burn Effect Settings")]
    [SerializeField] private GameObject burnParticleEffect;
    [SerializeField] private string burnShaderPropertyName = "_BurnAmount";
    [SerializeField] private float burnEffectDuration = 1.0f;

    #endregion

    #region Private Fields

    private readonly List<GameObject> lifeCards = new List<GameObject>();
    private readonly List<Material> burnMaterials = new List<Material>();

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (livesAnchor == null)
            livesAnchor = transform;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Sync life card visuals with the current life total.
    /// </summary>
    public void SetLives(int targetLives)
    {
        if (livesAnchor == null || lifeCardPrefab == null)
            return;

        targetLives = Mathf.Max(0, targetLives);

        AddMissingCards(targetLives);
        RemoveExtraCards(targetLives);

        RepositionCards();
    }

    /// <summary>
    /// Clears all spawned life cards immediately.
    /// </summary>
    public void ClearLifeCards()
    {
        for (int i = 0; i < lifeCards.Count; i++)
        {
            if (lifeCards[i] != null)
                Destroy(lifeCards[i]);
        }

        lifeCards.Clear();
    }

    #endregion

    #region Card Management

    private void AddMissingCards(int targetLives)
    {
        while (lifeCards.Count < targetLives)
        {
            GameObject newCard = Instantiate(lifeCardPrefab, livesAnchor);
            lifeCards.Add(newCard);
        }
    }

    private void RemoveExtraCards(int targetLives)
    {
        while (lifeCards.Count > targetLives)
        {
            int lastIndex = lifeCards.Count - 1;
            GameObject cardToRemove = lifeCards[lastIndex];
            lifeCards.RemoveAt(lastIndex);

            if (cardToRemove != null)
                StartCoroutine(BurnCardAnimation(cardToRemove));
        }
    }

    private void RepositionCards()
    {
        if (lifeCards.Count == 0)
            return;

        Vector3 direction = overlapDirection.sqrMagnitude > 0f ? overlapDirection.normalized : Vector3.right;
        float spacing = Mathf.Max(0f, overlapDistance);

        Vector3 startOffset = Vector3.zero;
        if (centerCards)
            startOffset = -direction * (spacing * (lifeCards.Count - 1) * 0.5f);

        float centerIndex = (lifeCards.Count - 1) * 0.5f;

        for (int i = 0; i < lifeCards.Count; i++)
        {
            GameObject card = lifeCards[i];
            if (card == null)
                continue;

            Transform cardTransform = card.transform;

            Vector3 position = startOffset + (direction * spacing * i);
            position.y += verticalOffsetPerCard * i;
            cardTransform.localPosition = position;

            float splayAngle = (i - centerIndex) * splayAnglePerCard;
            Quaternion splayRotation = Quaternion.Euler(0f, splayAngle, 0f);
            Quaternion tiltRotation = Quaternion.Euler(0f, 0f, cardTiltAngle);
            cardTransform.localRotation = splayRotation * tiltRotation;

            cardTransform.localScale = Vector3.one;
            cardTransform.SetSiblingIndex(i);
        }
    }

    #endregion

    #region Burn Effect

    private IEnumerator BurnCardAnimation(GameObject card)
    {
        if (card == null)
            yield break;

        if (PostProcessingEffectsController.Instance != null)
            PostProcessingEffectsController.Instance.PulseChromaticAberration();

        // Detach so card animation is unaffected by anchor movement.
        card.transform.SetParent(null);

        CollectBurnMaterials(card, burnMaterials);

        bool hasBurnMaterial = burnMaterials.Count > 0;

        SpawnBurnParticles(card.transform.position, card.transform.rotation);

        if (hasBurnMaterial)
        {
            yield return AnimateBurnOnMaterials();
        }
        else
        {
            yield return new WaitForSeconds(burnEffectDuration);
        }

        Destroy(card);
    }

    private void SpawnBurnParticles(Vector3 position, Quaternion rotation)
    {
        if (burnParticleEffect == null)
            return;

        GameObject particles = Instantiate(burnParticleEffect, position, rotation);
        Destroy(particles, BurnParticleLifetime);
    }

    private IEnumerator AnimateBurnOnMaterials()
    {
        if (burnEffectDuration <= 0f)
        {
            SetBurnValueOnMaterials(1f);
            yield break;
        }

        float elapsedTime = 0f;
        while (elapsedTime < burnEffectDuration)
        {
            elapsedTime += Time.deltaTime;
            float burnProgress = elapsedTime / burnEffectDuration;
            SetBurnValueOnMaterials(burnProgress);
            yield return null;
        }

        SetBurnValueOnMaterials(1f);
    }

    private void SetBurnValueOnMaterials(float burnValue)
    {
        for (int i = 0; i < burnMaterials.Count; i++)
            burnMaterials[i].SetFloat(burnShaderPropertyName, burnValue);
    }

    #endregion

    #region Material Collection

    private void CollectBurnMaterials(GameObject card, List<Material> materials)
    {
        materials.Clear();
        HashSet<int> seenMaterialIds = new HashSet<int>();

        CollectRendererBurnMaterials(card, materials, seenMaterialIds);
        CollectGraphicBurnMaterials(card, materials, seenMaterialIds);
    }

    private void CollectRendererBurnMaterials(GameObject card, List<Material> materials, HashSet<int> seenMaterialIds)
    {

        Renderer[] renderers = card.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] rendererMaterials = renderers[i].materials;
            for (int j = 0; j < rendererMaterials.Length; j++)
            {
                Material material = rendererMaterials[j];
                if (material != null && material.HasProperty(burnShaderPropertyName) && seenMaterialIds.Add(material.GetInstanceID()))
                    materials.Add(material);
            }
        }
    }

    private void CollectGraphicBurnMaterials(GameObject card, List<Material> materials, HashSet<int> seenMaterialIds)
    {
        Graphic[] graphics = card.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            Material graphicMaterial = graphic.material;
            if (graphicMaterial == null || !graphicMaterial.HasProperty(burnShaderPropertyName))
                continue;

            Material instanceMaterial = new Material(graphicMaterial);
            graphic.material = instanceMaterial;

            if (seenMaterialIds.Add(instanceMaterial.GetInstanceID()))
                materials.Add(instanceMaterial);
        }
    }

    #endregion
}