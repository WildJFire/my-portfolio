using UnityEngine;
using System.Collections.Generic;
using UnityNativeHull;
using System;
using System.Linq;
using Unity.Mathematics;
using Common;



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
                var t = transforms[i];
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

        Debug.Log("重建对象");

        this.EnsureDestoryed();

        _hulls = transforms.Where(t => t != null).ToDictionary(t => t.GetInstanceID(), CreateShape);

        SceneView.RepaintAll();
    }

    private TestShape CreateShape(Transform t)
    {
        var hull = CreateHull(t);
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
            DebugDrawer.DrawSphere(transformA.pos, 0.1f, Color.red);
            DebugDrawer.DrawSphere(transformB.pos, 0.1f, Color.red);
        }
    }

    private void OnDestroy() => this.EnsureDestoryed();
    private void OnDisable() => this.EnsureDestoryed();

    private void EnsureDestoryed()
    {
        if (this._hulls == null)
        {
            return;
        }

        foreach (var hull in _hulls.Values)
        {
            if (hull != null)
            {
                hull.Hull.Dispose();
            }
        }
        _hulls.Clear();
    }
}

