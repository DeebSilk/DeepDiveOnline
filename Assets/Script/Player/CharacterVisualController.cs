using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CharacterVisualController : MonoBehaviour
{
    [Header("=== CHARACTER VISUAL ===")]
    [SerializeField] private Animator animator;

    [Header("=== WEAPON ===")]
    [SerializeField] private Transform weaponEquipPoint;

    [Header("=== FIRST PERSON ===")]
    [SerializeField] private bool hideCharacterRenderersInFirstPerson = true;

    public event Action VisualChanged;

    public Animator Animator => animator;

    public Transform WeaponEquipPoint => weaponEquipPoint;

    private bool firstPersonMode;

    private void Awake()
    {
        RefreshReferences(false);
    }

    public void RefreshReferences(bool notify = true)
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (weaponEquipPoint == null)
        {
            weaponEquipPoint = FindWeaponEquipPoint();
        }

        if (animator == null)
        {
            Debug.LogError(
                "[CharacterVisualController] Animator not found.",
                this
            );
        }

        if (weaponEquipPoint == null)
        {
            Debug.LogWarning(
                "[CharacterVisualController] WeaponEquipPoint not found.",
                this
            );
        }

        ApplyFirstPersonVisualState();

        if (notify)
        {
            VisualChanged?.Invoke();
        }
    }

    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;

        if (weaponEquipPoint == null)
        {
            weaponEquipPoint = FindWeaponEquipPoint();
        }

        ApplyFirstPersonVisualState();

        VisualChanged?.Invoke();
    }

    public void SetWeaponEquipPoint(
        Transform newWeaponEquipPoint)
    {
        weaponEquipPoint = newWeaponEquipPoint;

        VisualChanged?.Invoke();
    }

    public void SetFirstPersonMode(bool firstPerson)
    {
        firstPersonMode = firstPerson;

        ApplyFirstPersonVisualState();
    }

    private void ApplyFirstPersonVisualState()
    {
        if (!hideCharacterRenderersInFirstPerson)
            return;

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            /*
             * WeaponEquipPoint находится внутри персонажа,
             * но само оружие может быть дочерним объектом этой точки.
             *
             * Поэтому Renderer оружия НЕ отключаем.
             */
            if (weaponEquipPoint != null &&
                renderer.transform.IsChildOf(weaponEquipPoint))
            {
                renderer.enabled = true;
                continue;
            }

            renderer.enabled = !firstPersonMode;
        }
    }

    private Transform FindWeaponEquipPoint()
    {
        Transform[] children =
            GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null)
                continue;

            if (string.Equals(
                child.name,
                "WeaponEquipPoint",
                StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>(true);
        }

        if (weaponEquipPoint == null)
        {
            weaponEquipPoint =
                FindWeaponEquipPoint();
        }
    }
}