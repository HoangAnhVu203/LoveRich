using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Gate : MonoBehaviour
{
    public int moneyPerHeart = 10;

    [System.Serializable]
    public class HeartVFXEntry
    {
        public string heartLayerName;

        [Tooltip("GameObject VFX tương ứng (con của Gate)")]
        public GameObject vfxObject;

        [Tooltip("Thời gian bật VFX rồi tắt (giây)")]
        public float activeTime = 0.5f;
    }

    [Header("Danh sách map Layer → VFX")]
    public HeartVFXEntry[] heartVFXList;

    // Để tránh bật chồng VFX liên tục
    Coroutine _vfxRoutine;

    void OnTriggerEnter(Collider other)
    {
        // Nếu muốn chỉ đếm tiền với Tag = Heart
        if (!other.CompareTag("Heart"))
            return;

        // 1. Cộng tiền
        PlayerMoney.Instance?.AddMoney(moneyPerHeart);

        // 2. Bật đúng VFX theo Layer
        int otherLayer = other.gameObject.layer;

        // Dò trong list
        for (int i = 0; i < heartVFXList.Length; i++)
        {
            var entry = heartVFXList[i];
            if (string.IsNullOrEmpty(entry.heartLayerName) || entry.vfxObject == null)
                continue;

            int layerFromName = LayerMask.NameToLayer(entry.heartLayerName);
            if (layerFromName == otherLayer)
            {
                PlayVFX(entry);
                break;
            }
        }
    }

    void PlayVFX(HeartVFXEntry entry)
    {
        if (entry.vfxObject == null) return;

        // Nếu đang có coroutine cũ -> dừng, để không bị chồng
        if (_vfxRoutine != null)
            StopCoroutine(_vfxRoutine);

        // Bật VFX
        entry.vfxObject.SetActive(false);   // reset
        entry.vfxObject.SetActive(true);    // bật lại

        _vfxRoutine = StartCoroutine(DisableVFXAfterDelay(entry));
    }

    IEnumerator DisableVFXAfterDelay(HeartVFXEntry entry)
    {
        yield return new WaitForSeconds(entry.activeTime);

        if (entry.vfxObject != null)
            entry.vfxObject.SetActive(false);

        _vfxRoutine = null;
    }
}
