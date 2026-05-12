using UnityEngine;

public class ProjectileSkill : Skill
{
    public GameObject projectilePrefab; // 需要在 Boss 初始化时赋值
    private float _projectileSpeed = 8f;
    private string _animTrigger = "Attack_Range";

    public ProjectileSkill(Boss boss, GameObject prefab) : base("Range_01", 3.0f, 8.0f, boss)
    {
        projectilePrefab = prefab;
    }

    public override void OnCast(Transform target)
    {
        Debug.Log($"Skill: 释放远程弹幕！目标：{target.name}");

        // 1. 播放动画
        if (owner.Animator != null)
            owner.Animator.SetTrigger(_animTrigger);

        // 2. 生成投射物
        if (projectilePrefab == null)
        {
            projectilePrefab = owner.ProjectilePrefab;
        }

        if (projectilePrefab != null)
        {
            Vector2 spawnPos = owner.Transform.position + Vector3.up * 1f;
            GameObject proj = GameObject.Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            //3.增加投射物逻辑
        }
    }
}
