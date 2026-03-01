using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// UI panel that displays dice information including name, description, and face values.
/// Closes when clicking the dice again or clicking elsewhere on screen.
/// </summary>
public class DiceInfoPanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dieNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI faceValuesText;
    [SerializeField] private Image dieIconImage;

    private GraphicRaycaster graphicRaycaster;
    private CanvasGroup canvasGroup;
    private System.Action onCloseCallback;
    private bool ignoreClickUntilRelease = true;

    private void OnEnable()
    {
        ignoreClickUntilRelease = true;
    }

    private void Start()
    {
        // Make sure we can detect clicks on the panel
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Find the raycaster for this canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
        }
    }

    private void Update()
    {
        if (ignoreClickUntilRelease)
        {
            if (!Input.GetMouseButton(0))
            {
                ignoreClickUntilRelease = false;
            }
            return;
        }

        // Check for clicks outside the panel
        if (Input.GetMouseButtonDown(0))
        {
            // Check if the click is on this panel
            if (graphicRaycaster != null)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;
                
                List<RaycastResult> results = new List<RaycastResult>();
                graphicRaycaster.Raycast(pointerData, results);

                // Check if this panel was clicked
                bool clickedOnPanel = false;
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject.transform.IsChildOf(transform) || result.gameObject == gameObject)
                    {
                        clickedOnPanel = true;
                        break;
                    }
                }

                // Close if clicked outside the panel
                if (!clickedOnPanel)
                {
                    if (!IsClickOnDice())
                    {
                        Close();
                    }
                }
            }
        }
    }

    private bool IsClickOnDice()
    {
        Camera cameraToUse = Camera.main;
        if (cameraToUse == null)
            return false;

        Ray ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.collider.GetComponentInParent<DB_DiceController>() != null;
        }

        return false;
    }

    /// <summary>
    /// Populate the panel with dice data.
    /// </summary>
    public void SetDiceInfo(DieData dieData)
    {
        if (dieData == null)
        {
            Debug.LogWarning("Cannot set dice info - DieData is null");
            return;
        }

        // Set name
        if (dieNameText != null)
        {
            dieNameText.text = dieData.dieName;
        }

        // Set description
        if (descriptionText != null)
        {
            descriptionText.text = dieData.description;
        }

        // Set face values
        if (faceValuesText != null)
        {
            string faceValuesStr = "Face Values: ";
            if (dieData.faceValues != null && dieData.faceValues.Length > 0)
            {
                faceValuesStr += string.Join(", ", dieData.faceValues);
            }
            else
            {
                faceValuesStr += "No face values assigned";
            }
            faceValuesText.text = faceValuesStr;
        }

        // Set icon if available
        if (dieIconImage != null && dieData.dieIcon != null)
        {
            dieIconImage.sprite = dieData.dieIcon;
        }
    }

    /// <summary>
    /// Set a callback to invoke when the panel closes.
    /// </summary>
    public void SetCloseCallback(System.Action callback)
    {
        onCloseCallback = callback;
    }

    /// <summary>
    /// Close and destroy this panel.
    /// </summary>
    public void Close()
    {
        onCloseCallback?.Invoke();
        gameObject.SetActive(false);
    }
}
