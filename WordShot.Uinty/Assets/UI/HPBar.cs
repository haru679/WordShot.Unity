
using UnityEngine;
using UnityEngine.UI;

public class HPBar : MonoBehaviour
{
    [Header("HPê›íË")]
    [SerializeField] private int maxHP = 50;

    [Header("ï\é¶ê›íË")]
    [SerializeField] private Image fillImage;

    private int currentHP;

    private void Awake()
    {
        maxHP = Mathf.Max(1, maxHP);
        currentHP = maxHP;

        UpdateBar();
    }

    public void SetHP(int hp)
    {
        currentHP = Mathf.Clamp(hp, 0, maxHP);
        UpdateBar();
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        SetHP(currentHP - damage);
    }

    public void Heal(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        SetHP(currentHP + amount);
    }

    private void UpdateBar()
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.fillAmount =
            (float)currentHP / maxHP;
    }

    public int GetCurrentHP()
    {
        return currentHP;
    }

    public int GetMaxHP()
    {
        return maxHP;
    }
}
