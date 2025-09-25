using UnityEngine;
using UnityEngine.UI;

public class CreditsPanelUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] GameObject creditsPanel; // maga a Credits panel
    [SerializeField] Button openButton;       // pl. MainMenu-n a "Credits" gomb
    [SerializeField] Button closeButton;      // a panelen levõ "Close" gomb

    void Awake()
    {
        if (creditsPanel) creditsPanel.SetActive(false);

        if (openButton) openButton.onClick.AddListener(OpenCredits);
        if (closeButton) closeButton.onClick.AddListener(CloseCredits);
    }

    public void OpenCredits()
    {
        if (creditsPanel) creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel) creditsPanel.SetActive(false);
    }
}
