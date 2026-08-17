using UnityEngine;
using System.Collections.Generic;
using UnityNativeHull;
using System;
using System.Linq;
using Unity.Mathematics;
using Common;
using System.Diagnostics;
using Unity.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class HullTester : MonoBehaviour
{
    public List<Transform> Transforms;// 这个列表将包含所有需要测试的物体的 Transform 组件。
    public DebugHullFlags HullDrawingOptions = DebugHullFlags.Outline;//凸包绘制选项

    [Header("可视化选项")]
    public bool DrawIsCollided;// 绘制碰撞结果。
    public bool DrawIntersection;// 绘制交集结果。

    [Header("控制台日志")]
    public bool LogContact;// 是否在控制台输出接触点信息。

    private Dictionary<int, TestShape> _hulls;

    private void Update()
    {
        HandleTransformChanged();
        HandleHullCollisions();
    }

    // 处理变换变化
    private void HandleTransformChanged()
    {
        // ToList()为了得到副本不影响原来的列表，Distinct()为了去重，Where()为了过滤掉不活跃的物体。
        // 这里主要是为了测试方便
        var transforms = Transforms.ToList().Distinct().Where(t => t.gameObject.activeSelf).ToList();
        bool newTransformFound = false;
        int transformCount = 0;

        if (_hulls != null)
        {
            for (int i = 0; i < transforms.Count; i++)
            {
                Transform t = transforms[i];
                if (!t)
                {
                    continue;
                }
                transformCount++;
                bool foundNewHull = !_hulls.ContainsKey(t.GetInstanceID());
                if (foundNewHull)
                {// 如果找到了新的 Transform，设置标志并跳出循环。
                    newTransformFound = true;
                    break;
                }
            }

            if (!newTransformFound && transformCount == _hulls.Count)
            {// 如果没有找到新的 Transform，并且当前的 Transform 数量与之前的 Hull 数量相同，说明没有变化，可以直接返回。
                return;
            }
        }

        UnityEngine.Debug.Log("重建对象");

        this.EnsureDestroyed();

        _hulls = transforms.Where(t => t != null).ToDictionary(t => t.GetInstanceID(), CreateShape);

        SceneView.RepaintAll();
    }

    private TestShape CreateShape(Transform t)
    {
        NativeHull hull = CreateHull(t);
        return new TestShape
        {
            Id = t.GetInstanceID(),
            Hull = hull,
        };
    }

    private NativeHull CreateHull(Transform t)
    {
        var collider = t.GetComponent<Collider>();
        if (collider is MeshCollider meshCollider)
        {
            return HullFactory.CreateFromMesh(meshCollider.sharedMesh);
        }

        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            return HullFactory.CreateFromMesh(mf.sharedMesh);
        }
        throw new InvalidOperationException("无法为指定的 Transform 创建凸体。");
    }

    // 处理凸包碰撞检测
    private void HandleHullCollisions()
    {
        for (int i = 0; i < Transforms.Count; i++)
        {
            Transform tA = Transforms[i];
            if (!tA)
            {
                continue;
            }
            NativeHull hullA = _hulls[tA.GetInstanceID()].Hull;
            //RigidTransform是Unity.Mathematics下的高性能结构体，就是为了之后的变换速度快，只关心位置角度这些信息
            var transformA = new RigidTransform(tA.rotation, tA.position);
            // 绘制凸包调试信息，主要就是外部轮廓
            HullDrawingUtility.DrawDebugHull(hullA, transformA, HullDrawingOptions);

            for (int j = i + 1; j < Transforms.Count; j++)
            {
                Transform tB = Transforms[j];
                if (!tB)
                {
                    continue;
                }

                if (!tA.hasChanged && !tB.hasChanged)
                    continue;

                NativeHull hullB = _hulls[tB.GetInstanceID()].Hull;
                var transformB = new RigidTransform(tB.rotation, tB.position);
                DrawHullCollision(tA.gameObject, tB.gameObject, transformA, hullA, transformB, hullB);
            }
        }
    }

    // 绘制凸包碰撞信息
    private void DrawHullCollision(GameObject a, GameObject b, RigidTransform transformA, NativeHull hullA, RigidTransform transformB, NativeHull hullB)
    {
        var collision = HullCollision.GetDebugCollisionInfo(transformA, hullA, transformB, hullB);
        if (collision.IsColliding)
        {

            // 绘制相交区域
            if (DrawIntersection)
            {
                HullIntersection.DrawNativeHullHullIntersection(transformA, hullA, transformB, hullB);
            }

            // 绘制接触信息
            if (LogContact)
            {
                var sw1 = Stopwatch.StartNew();
                var tmp = new NativeManifold(Allocator.Persistent);
                var normalResult = HullIntersection.NativeHullHullContact(ref tmp, transformA, hullA, transformB, hullB);
                sw1.Stop();
                tmp.Dispose();

                var sw2 = Stopwatch.StartNew();
                var burstResult = HullOperations.TryGetContact.Invoke(out NativeManifold manifold, transformA, hullA, transformB, hullB);
                sw2.Stop();

                if (LogContact)
                {
                    UnityEngine.Debug.Log($"'{a.name}'与'{b.name}'的接触计算耗时: {sw1.Elapsed.TotalMilliseconds:N4}ms (普通), {sw2.Elapsed.TotalMilliseconds:N4}ms (Burst)");
                }
            }

            if (DrawIsCollided)
            {
                DebugDrawer.DrawSphere(transformA.pos, 0.1f, Color.red);
                DebugDrawer.DrawSphere(transformB.pos, 0.1f, Color.red);
            }
        }
    }

    private void OnDestroy() => this.EnsureDestroyed();
    private void OnDisable() => this.EnsureDestroyed();

    private void EnsureDestroyed()
    {
        if (this._hulls == null)
        {
            return;
        }

        foreach (TestShape hull in _hulls.Values)
        {
            if (hull.Hull.IsValid)
            {
                hull.Hull.Dispose();
            }
        }
        _hulls.Clear();
    }
    void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    // 编辑器播放模式状态变化回调
    private void EditorApplication_playModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
            case PlayModeStateChange.ExitingPlayMode:
                EnsureDestroyed();
                break;
        }
    }
#endif

}

