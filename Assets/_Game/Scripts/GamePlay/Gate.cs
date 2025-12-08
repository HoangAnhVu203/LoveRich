using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Gate : MonoBehaviour
{
    [Header("Tiền nhận mỗi lần Heart đi qua")]
    public int moneyPerHeart = 10;


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Heart"))
        {
            if (PlayerMoney.Instance != null)
            {
                PlayerMoney.Instance.AddMoney(moneyPerHeart);
            }
        }
    }
}
