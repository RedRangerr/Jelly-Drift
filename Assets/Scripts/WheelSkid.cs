﻿using UnityEngine;

// Token: 0x02000044 RID: 68
[RequireComponent(typeof(WheelCollider))]
public class WheelSkid : MonoBehaviour
{
    // Token: 0x0600017D RID: 381 RVA: 0x00008556 File Offset: 0x00006756
    protected void Awake()
    {
        wheelCollider = base.GetComponent<WheelCollider>();
        lastFixedUpdateTime = Time.time;
    }

    // Token: 0x0600017E RID: 382 RVA: 0x0000856F File Offset: 0x0000676F
    protected void FixedUpdate() => lastFixedUpdateTime = Time.time;

    // Token: 0x0600017F RID: 383 RVA: 0x0000857C File Offset: 0x0000677C
    protected void LateUpdate()
    {
        if (!wheelCollider.GetGroundHit(out wheelHitInfo))
        {
            lastSkid = -1;
            return;
        }
        var num = Mathf.Abs(base.transform.InverseTransformDirection(rb.velocity).x);
        var num2 = wheelCollider.radius * (6.2831855f * wheelCollider.rpm / 60f);
        var num3 = Vector3.Dot(rb.velocity, base.transform.forward);
        var num4 = Mathf.Abs(num3 - num2) * 10f;
        num4 = Mathf.Max(0f, num4 * (10f - Mathf.Abs(num3)));
        num += num4;
        if (num >= 0.5f)
        {
            var opacity = Mathf.Clamp01(num / 20f);
            var pos = wheelHitInfo.point + rb.velocity * (Time.time - lastFixedUpdateTime);
            lastSkid = skidmarksController.AddSkidMark(pos, wheelHitInfo.normal, opacity, lastSkid);
            return;
        }
        lastSkid = -1;
    }

    // Token: 0x0400018F RID: 399
    [SerializeField]
    private Rigidbody rb;

    // Token: 0x04000190 RID: 400
    [SerializeField]
    private Skidmarks skidmarksController;

    // Token: 0x04000191 RID: 401
    private WheelCollider wheelCollider;

    // Token: 0x04000192 RID: 402
    private WheelHit wheelHitInfo;

    // Token: 0x04000196 RID: 406
    private int lastSkid = -1;

    // Token: 0x04000197 RID: 407
    private float lastFixedUpdateTime;
}
