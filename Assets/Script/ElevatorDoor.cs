using System.Collections;
using UnityEngine;

public class ElevatorDoor : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] string boolParam = "IsOpen";

    // NEW: pontos �llapotn�v, amire �oda tudunk ugrani�
    [SerializeField] string closedStateName = "Closed";
    [SerializeField] string openStateName = "Open";

    [SerializeField] SpriteRenderer frontOverlay;

    void Reset()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    // --- �J: instant be�ll�t�s anim�ci� lej�tsz�sa n�lk�l ---
    public void SnapTo(bool open)
    {
        if (animator)
        {
            animator.SetBool(boolParam, open);
            // 0. layer, normalizedTime=1f (klip v�ge), azonnali �ugr�s�
            animator.Play(open ? openStateName : closedStateName, 0, 1f);
            animator.Update(0f); // friss�tj�k az �llapotot m�g ugyanabban a frame-ben
        }
        if (frontOverlay) frontOverlay.enabled = open;
    }

    public IEnumerator Open(float duration)
    {
        if (animator) animator.SetBool(boolParam, true);
        if (frontOverlay) frontOverlay.enabled = true;
        if (duration > 0) yield return new WaitForSeconds(duration);
    }

    public IEnumerator Close(float duration)
    {
        if (animator) animator.SetBool(boolParam, false);
        if (duration > 0) yield return new WaitForSeconds(duration);
        if (frontOverlay) frontOverlay.enabled = false;
    }

    public void SetOverlay(bool on)
    {
        if (frontOverlay) frontOverlay.enabled = on;
    }
}
