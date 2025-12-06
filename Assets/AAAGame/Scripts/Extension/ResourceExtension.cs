using UnityEngine;
using GameFramework;
using GameFramework.Resource;
using UnityGameFramework.Runtime;
using Cysharp.Threading.Tasks;
using System;

namespace GameExtension
{
    /// <summary>
    /// 资源类型枚举
    /// </summary>
    public enum ResourceType
    {
        Sprite = 1,      // 图片
        Prefab = 2,      // 预制体
        Effect = 3,      // 特效
        Material = 4,    // 材质
        Texture = 5,     // 纹理
    }

    /// <summary>
    /// 资源加载扩展类 - 基于配置表的动态资源加载
    /// </summary>
    public static class ResourceExtension
    {
        #region 异步加载（回调方式）

        /// <summary>
        /// 根据配置表ID异步加载资源（回调方式）
        /// </summary>
        public static void LoadAssetByConfigAsync<T>(int configId, Action<T> onSuccess, Action<string> onFailure = null)
            where T : UnityEngine.Object
        {
            // 从配置表获取资源信息
            var config = GetResourceConfig(configId);
            if (config == null)
            {
                string error = $"资源配置不存在: ID={configId}";
                Log.Error(error);
                onFailure?.Invoke(error);
                return;
            }

            // 根据资源类型构建完整路径
            string fullPath = GetFullAssetPath(config.Path, (ResourceType)config.Type);

            // ✅ 使用正确的方式：LoadAsset(string, Type, LoadAssetCallbacks)
            GF.Resource.LoadAsset(
                fullPath,
                typeof(T),
                new LoadAssetCallbacks(
                    (assetName, asset, duration, userData) =>
                    {
                        // 成功回调
                        onSuccess?.Invoke(asset as T);
                    },
                    (assetName, status, errorMessage, userData) =>
                    {
                        // 失败回调
                        Log.Error($"加载资源失败: ConfigId={configId}, Path={fullPath}, Error={errorMessage}");
                        onFailure?.Invoke(errorMessage);
                    }
                )
            );
        }

        /// <summary>
        /// 根据配置表ID异步加载Sprite（回调方式）
        /// </summary>
        public static void LoadSpriteAsync(int configId, Action<Sprite> onSuccess, Action<string> onFailure = null)
        {
            LoadAssetByConfigAsync(configId, onSuccess, onFailure);
        }

        /// <summary>
        /// 根据配置表ID异步加载GameObject（回调方式）
        /// </summary>
        public static void LoadPrefabAsync(int configId, Action<GameObject> onSuccess, Action<string> onFailure = null)
        {
            LoadAssetByConfigAsync(configId, onSuccess, onFailure);
        }

        #endregion

        #region 异步加载（UniTask方式）

        /// <summary>
        /// 根据配置表ID异步加载资源（UniTask方式）
        /// </summary>
        public static async UniTask<T> LoadAssetByConfigAsync<T>(int configId) where T : UnityEngine.Object
        {
            // 从配置表获取资源信息
            var config = GetResourceConfig(configId);
            if (config == null)
            {
                Log.Error($"资源配置不存在: ID={configId}");
                return null;
            }

            // 根据资源类型构建完整路径
            string fullPath = GetFullAssetPath(config.Path, (ResourceType)config.Type);

            try
            {
                // ✅ 使用 AwaitExtension 中的 LoadAssetAwait 方法
                return await GF.Resource.LoadAssetAwait<T>(fullPath);
            }
            catch (Exception ex)
            {
                Log.Error($"加载资源失败: ConfigId={configId}, Path={fullPath}, Error={ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据配置表ID异步加载Sprite（UniTask方式）
        /// </summary>
        public static async UniTask<Sprite> LoadSpriteAsync(int configId)
        {
            return await LoadAssetByConfigAsync<Sprite>(configId);
        }

        /// <summary>
        /// 根据配置表ID异步加载GameObject（UniTask方式）
        /// </summary>
        public static async UniTask<GameObject> LoadPrefabAsync(int configId)
        {
            return await LoadAssetByConfigAsync<GameObject>(configId);
        }

        /// <summary>
        /// 根据配置表ID异步加载Material（UniTask方式）
        /// </summary>
        public static async UniTask<Material> LoadMaterialAsync(int configId)
        {
            return await LoadAssetByConfigAsync<Material>(configId);
        }

        /// <summary>
        /// 根据配置表ID异步加载Texture（UniTask方式）
        /// </summary>
        public static async UniTask<Texture> LoadTextureAsync(int configId)
        {
            return await LoadAssetByConfigAsync<Texture>(configId);
        }

        #endregion
        
        #region 直接路径加载（不依赖配置表）

        /// <summary>
        /// 直接通过路径异步加载Sprite（回调方式）
        /// </summary>
        public static void LoadSpriteByPathAsync(string relativePath, Action<Sprite> onSuccess, Action<string> onFailure = null)
        {
            string fullPath = UtilityBuiltin.AssetsPath.GetSpritesPath(relativePath);

            GF.Resource.LoadAsset(
                fullPath,
                typeof(Sprite),
                new LoadAssetCallbacks(
                    (assetName, asset, duration, userData) =>
                    {
                        onSuccess?.Invoke(asset as Sprite);
                    },
                    (assetName, status, errorMessage, userData) =>
                    {
                        Log.Error($"加载Sprite失败: Path={fullPath}, Error={errorMessage}");
                        onFailure?.Invoke(errorMessage);
                    }
                )
            );
        }

        /// <summary>
        /// 直接通过路径异步加载Texture（回调方式）
        /// </summary>
        public static void LoadTextureByPathAsync(string relativePath, Action<Texture2D> onSuccess, Action<string> onFailure = null)
        {
            string fullPath = UtilityBuiltin.AssetsPath.GetTexturePath(relativePath);

            GF.Resource.LoadAsset(
                fullPath,
                typeof(Texture2D),
                new LoadAssetCallbacks(
                    (assetName, asset, duration, userData) =>
                    {
                        onSuccess?.Invoke(asset as Texture2D);
                    },
                    (assetName, status, errorMessage, userData) =>
                    {
                        Log.Error($"加载Texture失败: Path={fullPath}, Error={errorMessage}");
                        onFailure?.Invoke(errorMessage);
                    }
                )
            );
        }

        #endregion

        #region 实例化预制体

        /// <summary>
        /// 根据配置表ID异步加载预制体并实例化（UniTask方式）
        /// </summary>
        public static async UniTask<GameObject> InstantiatePrefabAsync(int configId, Transform parent = null)
        {
            var prefab = await LoadPrefabAsync(configId);
            if (prefab == null)
            {
                Log.Error($"加载预制体失败: ConfigId={configId}");
                return null;
            }

            return parent != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : UnityEngine.Object.Instantiate(prefab);
        }

        /// <summary>
        /// 根据配置表ID异步加载预制体并实例化（回调方式）
        /// </summary>
        public static void InstantiatePrefabAsync(int configId, Transform parent, Action<GameObject> onSuccess, Action<string> onFailure = null)
        {
            LoadAssetByConfigAsync<GameObject>(
                configId,
                (prefab) =>
                {
                    if (prefab != null)
                    {
                        var instance = parent != null
                            ? UnityEngine.Object.Instantiate(prefab, parent)
                            : UnityEngine.Object.Instantiate(prefab);
                        onSuccess?.Invoke(instance);
                    }
                    else
                    {
                        onFailure?.Invoke($"预制体为空: ConfigId={configId}");
                    }
                },
                onFailure
            );
        }

        #endregion

        #region 批量加载

        /// <summary>
        /// 批量加载资源（根据配置表ID数组）
        /// </summary>
        public static async UniTask<T[]> LoadAssetsByConfigAsync<T>(int[] configIds) where T : UnityEngine.Object
        {
            if (configIds == null || configIds.Length == 0)
            {
                return new T[0];
            }

            var tasks = new UniTask<T>[configIds.Length];
            for (int i = 0; i < configIds.Length; i++)
            {
                tasks[i] = LoadAssetByConfigAsync<T>(configIds[i]);
            }

            return await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 批量加载Sprite
        /// </summary>
        public static async UniTask<Sprite[]> LoadSpritesAsync(int[] configIds)
        {
            return await LoadAssetsByConfigAsync<Sprite>(configIds);
        }

        /// <summary>
        /// 批量加载GameObject
        /// </summary>
        public static async UniTask<GameObject[]> LoadPrefabsAsync(int[] configIds)
        {
            return await LoadAssetsByConfigAsync<GameObject>(configIds);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 从配置表获取资源配置
        /// </summary>
        private static ResourceConfigTable GetResourceConfig(int configId)
        {
            var dataTable = GF.DataTable.GetDataTable<ResourceConfigTable>();
            if (dataTable == null)
            {
                Log.Error("ResourceConfigTable 数据表未加载！请确保在 PreloadProcedure 中加载了该数据表。");
                return null;
            }

            var config = dataTable.GetDataRow(configId);
            if (config == null)
            {
                Log.Error($"资源配置不存在: ID={configId}");
                return null;
            }

            return config;
        }

        /// <summary>
        /// 根据资源类型和相对路径获取完整的资源路径
        /// </summary>
        private static string GetFullAssetPath(string relativePath, ResourceType type)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                Log.Error("资源路径为空！");
                return string.Empty;
            }

            switch (type)
            {
                case ResourceType.Sprite:
                    return UtilityBuiltin.AssetsPath.GetSpritesPath(relativePath);

                case ResourceType.Prefab:
                    return UtilityBuiltin.AssetsPath.GetPrefab(relativePath);

                case ResourceType.Effect:
                    return UtilityBuiltin.AssetsPath.GetEntityPath(relativePath);

                case ResourceType.Material:
                    return UtilityBuiltin.AssetsPath.GetMaterialPath(relativePath);

                case ResourceType.Texture:
                    return UtilityBuiltin.AssetsPath.GetTexturePath(relativePath);

                default:
                    Log.Warning($"未知的资源类型: {type}, 使用相对路径: {relativePath}");
                    return relativePath;
            }
        }

        /// <summary>
        /// 检查资源配置是否存在
        /// </summary>
        public static bool HasResourceConfig(int configId)
        {
            var dataTable = GF.DataTable.GetDataTable<ResourceConfigTable>();
            return dataTable != null && dataTable.GetDataRow(configId) != null;
        }

        /// <summary>
        /// 获取资源路径（不加载资源）
        /// </summary>
        public static string GetResourcePath(int configId)
        {
            var config = GetResourceConfig(configId);
            if (config == null)
            {
                return string.Empty;
            }

            return GetFullAssetPath(config.Path, (ResourceType)config.Type);
        }

        /// <summary>
        /// 获取资源类型
        /// </summary>
        public static ResourceType? GetResourceType(int configId)
        {
            var config = GetResourceConfig(configId);
            if (config == null)
            {
                return null;
            }

            return (ResourceType)config.Type;
        }

        #endregion
    }
}
