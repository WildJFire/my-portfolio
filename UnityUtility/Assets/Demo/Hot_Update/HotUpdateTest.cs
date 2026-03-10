using HotUpdate;
using UnityEngine;

public class HotUpdateTest : MonoBehaviour
{
    void Start()
    {
        HotUpdateManager.Instance.StartHotUpdate();
    }
}
