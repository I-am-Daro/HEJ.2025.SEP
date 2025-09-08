using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UIStatusBar : MonoBehaviour
{
    [SerializeField] Slider bar;         // a "Bar" Slider
    [SerializeField] TextMeshProUGUI label;     // "Label"
    [SerializeField] TextMeshProUGUI valueText; // "ValueText"

    public void SetLabel(string text) => label.text = text;

    public void SetRange(float min, float max)
    {
        bar.minValue = min;
        bar.maxValue = max;
    }

    public void SetValue(float v, bool showPercent = true)
    {
        bar.value = v;
        if (showPercent)
        {
            float pct = bar.maxValue > 0 ? (v / bar.maxValue) * 100f : 0f;
            valueText.text = Mathf.RoundToInt(pct) + "%";
        }
        else
        {
            valueText.text = Mathf.RoundToInt(v).ToString();
        }
    }

    public void SetColors(Color fillColor, Color bgColor, float bgAlpha = 0.35f)
    {
        var fill = bar.fillRect ? bar.fillRect.GetComponent<Image>() : null;
        var bg = bar.GetComponentInChildren<Image>(); // elsõ Image a Slideren általában a Background
        if (fill) fill.color = fillColor;
        if (bg) bg.color = new Color(bgColor.r, bgColor.g, bgColor.b, bgAlpha);
    }
}
