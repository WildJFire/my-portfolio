using AssetBundleFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestUI : MonoBehaviour
{
    void Start()
    {
        _backgroundIndex = -1;
        _iconIndex = -1;
        _roleIndex = -1;
    }
    private string[] _roles = new string[]
    {
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_000.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_001.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_002.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_003.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_004.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_005.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_006.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_007.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_008.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_009.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_010.png",
        "Assets/AssetBundle/Atlas/Role/Hog_Attack_011.png",
    };

    private string[] _icons = new string[]
    {
        "Assets/AssetBundle/Icon/1.png",
        "Assets/AssetBundle/Icon/2.png",
        "Assets/AssetBundle/Icon/3.png",
        "Assets/AssetBundle/Icon/4.png",
        "Assets/AssetBundle/Icon/5.png",
        "Assets/AssetBundle/Icon/6.png",
        "Assets/AssetBundle/Icon/7.png",
        "Assets/AssetBundle/Icon/8.png",
        "Assets/AssetBundle/Icon/9.png",
        "Assets/AssetBundle/Icon/10.png",
        "Assets/AssetBundle/Icon/11.png",
        "Assets/AssetBundle/Icon/12.png",
        "Assets/AssetBundle/Icon/13.png",
        "Assets/AssetBundle/Icon/14.png",
        "Assets/AssetBundle/Icon/15.png",
        "Assets/AssetBundle/Icon/16.png",
        "Assets/AssetBundle/Icon/17.png",
        "Assets/AssetBundle/Icon/18.png",
        "Assets/AssetBundle/Icon/19.png",
    };
    
    private string[] _backgrounds = new string[]
    {
        "Assets/AssetBundle/Background/1.png",
        "Assets/AssetBundle/Background/2.png",
        "Assets/AssetBundle/Background/3.png",
        "Assets/AssetBundle/Background/4.png",
        "Assets/AssetBundle/Background/5.png",
        "Assets/AssetBundle/Background/6.png",
        "Assets/AssetBundle/Background/7.png",
    };

    [SerializeField] private RawImage _rawImage_bg = null;
    [SerializeField] private RawImage _rawImage_icon = null;
    [SerializeField] private Image _rawImage_Bear = null;

    [SerializeField] private Transform _modelRoot;
    [SerializeField] private TextMeshProUGUI _text;
    private GameObject _modelGO;
    private IResource _modelResource;

    private string _modelUrl = "Assets/AssetBundle/Model/Ji.prefab";


    private int _backgroundIndex = -1;
    private int _roleIndex = -1;
    private int _iconIndex = -1;
    
    public void OnChangeBackground()
    {
        if (_backgrounds.Length == 0)
        {
            return;
        }
        _backgroundIndex = ++_backgroundIndex % _backgrounds.Length;
        
        string url = _backgrounds[_backgroundIndex];
        
        IResource resource = ResourceManager.Instance.Load(url, false);
        _rawImage_bg.texture = resource.GetAsset() as Texture;
    }

    /// <summary>
    /// 切换人物的sprite
    /// </summary>
    public void OnChangeBear()
    {
        if (_roles.Length == 0)
            return;

        _roleIndex = ++_roleIndex % _roles.Length;

        string bearUrl = _roles[_roleIndex];

        //同步加载人物的sprite
        IResource resource = ResourceManager.Instance.Load(bearUrl, false);
        _rawImage_Bear.sprite = resource.GetAsset<Sprite>();
    }
    
    /// <summary>
    /// 切换道具图标
    /// </summary>
    public void OnChangeIcon()
    {
        if (_icons.Length == 0)
            return;

        _iconIndex = ++_iconIndex % _icons.Length;
        string iconUrl = _icons[_iconIndex];

        //同步加载icon
        IResource resource = ResourceManager.Instance.Load(iconUrl, false);
        _rawImage_icon.texture = resource.GetAsset<Texture>();
    }

    /// <summary>
    /// 加载模型
    /// </summary>
    public void OnLoadModel()
    {
        if (_modelResource != null)
            return;

        //同步加载
        _modelResource = ResourceManager.Instance.Load(_modelUrl, false);
        _modelGO = _modelResource.Instantiate(_modelRoot, false);
        _modelGO.transform.eulerAngles = new Vector3(0, 180, 0);
    }

    /// <summary>
    /// 卸载模型
    /// </summary>
    public void OnUnloadModel()
    {
        if (_modelResource == null)
            return;

        ResourceManager.Instance.Unload(_modelResource);
        _modelResource = null;
        if (_modelGO)
        {
            Destroy(_modelGO);
            _modelGO = null;
        }
    }
}