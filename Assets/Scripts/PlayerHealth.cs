using System.Collections;
using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int startingLives = 3;
    public float invulnerabilityDuration = 2f;
    public float slowDuration = 1.5f;
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
    public Renderer[] renderers;

    public int Lives { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public int MaxIntegrity => startingLives;
    public event Action<int, int> IntegrityChanged;
    public event Action OnDeath;

    private PlayerRunner runner;
    private bool deathRaised;

    private void Awake()
    {
        runner = GetComponent<PlayerRunner>();
        Lives = startingLives;
    }

    private void Start()
    {
        GameManager.Instance?.ui?.SetLives(Lives);
        IntegrityChanged?.Invoke(Lives, startingLives);
    }

    public void TakeDamage()
    {
        if (GameManager.Instance == null || !GameManager.Instance.AllowsDamage ||
            IsInvulnerable || Lives <= 0)
        {
            return;
        }

        Lives--;
        GameManager.Instance.ui.SetLives(Lives);
        IntegrityChanged?.Invoke(Lives, startingLives);
        GameManager.Instance.celestIA.ShowTemporary("CELESTIA: Impacto detectado. Estabilizando traje.", 2f);

        if (Lives <= 0)
        {
            if (!deathRaised)
            {
                deathRaised = true;
                OnDeath?.Invoke();
            }
            return;
        }

        StartCoroutine(DamageFeedback());
    }

    private IEnumerator DamageFeedback()
    {
        IsInvulnerable = true;
        runner.SetSpeedMultiplier(slowMultiplier);
        float elapsed = 0f;

        while (elapsed < invulnerabilityDuration)
        {
            SetVisible(false);
            yield return new WaitForSeconds(0.12f);
            SetVisible(true);
            yield return new WaitForSeconds(0.12f);
            elapsed += 0.24f;

            if (elapsed >= slowDuration)
            {
                runner.SetSpeedMultiplier(1f);
            }
        }

        runner.SetSpeedMultiplier(1f);
        SetVisible(true);
        IsInvulnerable = false;
    }

    private void SetVisible(bool value)
    {
        foreach (Renderer item in renderers)
        {
            if (item != null)
            {
                item.enabled = value;
            }
        }
    }
}
