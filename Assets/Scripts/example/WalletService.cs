using System;
using System.Threading.Tasks;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;
using UnityEngine;
using Frictionless;
using System.Collections;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public enum StorageMethod { Json, SimpleTxt }

    [RequireComponent(typeof(MainThreadDispatcher))]
    public class WalletService : MonoBehaviour, IMultiSceneSingleton
    {
        [SerializeField]
        private RpcCluster rpcCluster = RpcCluster.DevNet;
        [HideIfEnumValue("rpcCluster", HideIf.NotEqual, (int) RpcCluster.Custom)]
        public string customRpc;
        public bool autoConnectOnStartup;
        public string webSocketsRpc;

        private StorageMethod _storageMethod;
        
        public Web3AuthWalletOptions web3AuthWalletOptions;
        
        public PhantomWalletOptions phantomWalletOptions;
        
        private const string StorageMethodStateKey = "StorageMethodKey";

        public WalletBase Wallet;

        public static WalletService Instance;
        
        // Convenience shortnames for accessing commonly used wallet methods
        public static IRpcClient Rpc => Instance != null ? Instance.Wallet?.ActiveRpcClient : null;
        public static IStreamingRpcClient WsRpc => Instance != null ? Instance.Wallet?.ActiveStreamingRpcClient : null;
        public static Account Account => Instance != null ? Instance.Wallet?.Account : null;
        public static WalletBase Base => Instance != null ? Instance.Wallet : null;

        public void Awake()
        {
            if (ServiceFactory.Resolve<WalletService>() != null)
            {
                Debug.LogWarning("wallet service is already exist!");
                Destroy(gameObject);
                return;
            }

            ServiceFactory.RegisterSingleton(this);

            Instance = ServiceFactory.Resolve<WalletService>();
        }
        
        public void Start()
        {
            ChangeState(_storageMethod.ToString());
            if (PlayerPrefs.HasKey(StorageMethodStateKey))
            {
                var storageMethodString = LoadPlayerPrefs(StorageMethodStateKey);

                if(storageMethodString != _storageMethod.ToString())
                {
                    storageMethodString = _storageMethod.ToString();
                    ChangeState(storageMethodString);
                }

                if (storageMethodString == StorageMethod.Json.ToString())
                    StorageMethodReference = StorageMethod.Json;
                else if (storageMethodString == StorageMethod.SimpleTxt.ToString())
                    StorageMethodReference = StorageMethod.SimpleTxt;
            }
            else
                StorageMethodReference = StorageMethod.SimpleTxt;          
        }

        public async Task<Account> LoginInGameWallet(string password)
        {
            var inGameWallet = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await inGameWallet.Login(password);
            if (acc != null)
                Wallet = inGameWallet;
            return acc;
        }
        
        public async Task<Account> CreateAccount(string mnemonic, string password)
        {
            Wallet = new InGameWallet(rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            return await Wallet.CreateAccount( mnemonic, password);
        }
        
        public async Task<Account> LoginInWeb3Auth(Provider provider)
        {
            var web3AuthWallet = new Web3AuthWallet(web3AuthWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await web3AuthWallet.LoginWithProvider(provider);
            if (acc != null)
                Wallet = web3AuthWallet;
            return acc;
        }
        
        public async Task<Account> LoginPhantom()
        {
            var phantomWallet = new PhantomWallet(phantomWalletOptions, rpcCluster, customRpc, webSocketsRpc, autoConnectOnStartup);
            var acc = await phantomWallet.Login();
            if (acc != null)
                Wallet = phantomWallet;
            return acc;
        }

        public async Task<Account> LoginXNFT()
        {
            var XNFTWallet = new XNFTWallet(rpcCluster, customRpc, webSocketsRpc, false);
            var acc = await XNFTWallet.Login();
            if (acc != null)
                Wallet = XNFTWallet;
            return acc;
        }

        public void RpcNodeDropdownSelected(int value)
        {
            rpcCluster = RpcCluster.Custom;
            customRpc = value switch
            {
                (int) RpcCluster.MainNet => "https://rpc.ankr.com/solana",
                _ => "https://rpc.ankr.com/solana_devnet"
            };
        }
        
        public void Logout()
        {
            Wallet.Logout();
            Wallet = null;
        }

        private void ChangeState(string state)
        {
            SavePlayerPrefs(StorageMethodStateKey, _storageMethod.ToString());
        }

        public StorageMethod StorageMethodReference
        {
            get => _storageMethod;
            private set { _storageMethod = value; ChangeState(_storageMethod.ToString()); }
        }
        
        #region Data Functions

        private static void SavePlayerPrefs(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            #if UNITY_WEBGL
            PlayerPrefs.Save();
            #endif
        }

        private static string LoadPlayerPrefs(string key)
        {
            return PlayerPrefs.GetString(key);
        }
        #endregion
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Setup()
        {
            MainThreadUtil.Setup();
        }

        public IEnumerator HandleNewSceneLoaded()
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// Keeps SimpleWallet for compatibility with older versions of the SDK
    /// </summary>
    [Obsolete("Deprecated, use WalletH instead", true)]
    public static class SimpleWallet
    {
        public static WalletService Instance => WalletService.Instance;
    }
}
