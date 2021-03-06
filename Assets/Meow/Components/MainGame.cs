﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using Meow.AssetLoader;
using Meow.AssetLoader.Core;
using Meow.AssetUpdater;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XLua;

namespace Meow.Framework
{
    [LuaCallCSharp]
    public class MainGame : MonoBehaviour
    {
        private static readonly Dictionary<string, MainGame> _mainGames = new Dictionary<string, MainGame>();
    
        public string ProjectName;
        public string RemoteUrl;
        public string VersionFileName;
        public string LuaBundleName;
        public string LuaScriptRootFolderName;
        public string SceneRootFolderPath;
        public string MainSceneName = "01Main.unity";
        public string EntranceScenePath;

        public bool IsAutoDownload;

        public Text SingleSizeText;
        public Slider SingleSlider;
        public Text TotalSizeText;
        public Slider TotalSlider;
        public Text AssetBundleCountText;
        public Button StartGameButton;

        private MainLoader _loader;
        private MainUpdater _updater;
        private LuaLoader _luaLoader;
        private LuaHelper _luaHelper;

        private UpdateOperation _updateOperation;
        private int _totalCount;

        private void Awake()
        {
            _updater = gameObject.AddComponent<MainUpdater>();
            _loader = gameObject.AddComponent<MainLoader>();
            _luaLoader = gameObject.AddComponent<LuaLoader>();
            _luaHelper = gameObject.AddComponent<LuaHelper>();
            
            DontDestroyOnLoad(gameObject);
        }

        private IEnumerator Start()
        {
            StartGameButton.gameObject.SetActive(false);
            
            _mainGames.Add(ProjectName, this);
            if (IsAutoDownload)
            {
                yield return Download();
                StartGameButton.onClick.AddListener(() =>
                {
                    StartInitializeCoroutine(null);
                });
            }
        }

        private IEnumerator Download()
        {
            _updater.Initialize(RemoteUrl, ProjectName, VersionFileName);
            
            yield return _updater.LoadAllVersionFiles();
            yield return _updater.UpdateFromStreamingAsset();
            
            _updateOperation = _updater.UpdateFromRemoteAsset();
            _totalCount = _updateOperation.RemainBundleCount;
            yield return _updateOperation;
            
            StartGameButton.gameObject.SetActive(true);
        }

        private IEnumerator Initialize()
        {
            yield return _loader.Initialize(_updater.GetAssetbundleRootPath(true), _updater.GetManifestName());
            
            yield return _luaLoader.Initialize(ProjectName, _loader, LuaBundleName, LuaScriptRootFolderName);

            _luaHelper.Initialize(_loader, _updater);
            
            LoadLevelAsync(Path.Combine(SceneRootFolderPath, MainSceneName));
            
        }

        private void Update()
        {
            if (_updateOperation != null)
            {
                if(SingleSizeText != null)
                    SingleSizeText.text = _updateOperation.SingleDownloadedSize + " / " + _updateOperation.SingleSize;
                if(SingleSlider != null)
                    SingleSlider.value = _updateOperation.SingleProgress;
                if(TotalSizeText != null)
                    TotalSizeText.text = _updateOperation.TotalownloadedSize + " / " + _updateOperation.TotalSize;
                if(TotalSlider != null)
                    TotalSlider.value = _updateOperation.TotalProgress;
                if(AssetBundleCountText != null)
                    AssetBundleCountText.text = ( _totalCount - _updateOperation.RemainBundleCount ) + " / " + _totalCount;
            }
        }
        
        public LoadLevelOperation LoadLevelAsync(string levelPath, bool isAdditive = false)
        {
            var bundlePath = _updater.GetAssetbundlePathByAssetPath(levelPath);
            var op = _loader.GetLoadLevelOperation(bundlePath, levelPath, isAdditive);
            StartCoroutine(op);
            return op;
        }
        
        public void ReturnToEntrance()
        {
            SceneManager.LoadScene(EntranceScenePath);
            
            foreach (var mainGame in _mainGames.Values)
            {
                mainGame._loader.UnLoadAssetBundles();
                Destroy(mainGame.gameObject);
            }
            _mainGames.Clear();
        }

        public static MainGame GetInstance(string projectName)
        {
            Debug.AssertFormat(_mainGames.ContainsKey(projectName), "Cannot find main game with project name [{0}]", projectName);
            return _mainGames[projectName];
        }

        public static LuaLoader GetLuaLoader(string projectName)
        {
            var mainGame = GetInstance(projectName);
            return mainGame._luaLoader;
        }

        public static LuaHelper GetLuaHelper(string projectName)
        {
            var mainGame = GetInstance(projectName);
            return mainGame._luaHelper;
        }

        public void StartUpdateCoroutine()
        {
            StartCoroutine(Download());
        }

        public void StartInitializeCoroutine(string sourceProjectName)
        {
            if (!string.IsNullOrEmpty(sourceProjectName))
            {
                var mainGame = _mainGames[sourceProjectName];
                mainGame._loader.UnLoadAssetBundles();
                _mainGames.Remove(sourceProjectName);
                Destroy(mainGame.gameObject);
            }
            StartCoroutine(Initialize());
        }

    }

}
