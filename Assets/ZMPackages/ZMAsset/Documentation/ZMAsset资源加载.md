# ZMAsset 资源加载说明

本文档说明本工程中 **ZMAsset** 的资源路径规则、初始化顺序、对外 API 及与 **BundleSettings**、热更目录的关系。实现细节以 `Assets/ZMPackages/ZMAsset` 下运行时代码为准。

---

## 1. 核心概念

### 1.1 资源路径（逻辑路径）

- 加载时传入的 **`path`** 必须与打 AB 时写入 **`{模块名小写}assetbundleconfig.json`**（如 `windowassetbundleconfig.json`）里 **`BundleInfo.path`** 的字符串 **一致**（含 `Assets/`、扩展名等），框架用 **`Crc32.GetCrc32(path)`** 在已初始化的模块表里查找资源。
- **同一资源**在 Editor 直连与 AB 模式下应使用 **同一条逻辑路径**，便于切换 `loadAssetType`。

### 1.2 多模块（bundleModule）

- 每个业务模块（如 `Window`、`Common`、`DialogueSO`）对应一套 **`{module}bundleconfig{ABSUFFIX}`** 包（如 `windowbundleconfig.uab`）及 JSON 配置表。
- 使用某模块下的资源前，必须 **`await ZMAsset.InitAssetsModule("模块名")`** 成功，将该模块的 **`bundleInfoList`** 注册进内存；否则 CRC 查表失败，加载返回 `null`。

### 1.3 配置：`Resources/AssetsBundleSettings.asset`（`BundleSettings`）

| 字段 | 含义 |
|------|------|
| **loadAssetType** | **Editor**：编辑器下用 `AssetDatabase.LoadAssetAtPath`（仍建议路径与 bundle 表一致）。**AssetBundle**：走 AB（及热更路径逻辑）。 |
| **bundleHotType** | **NoHot**：不按热更目录拉取；**Hot**：若 `persistentDataPath/HotAssets/{模块}/` 下存在对应文件则优先使用。 |
| **ABSUFFIX** | AB 配置文件包名后缀，如 `.uab`，需与打包产物一致。 |
| **AssetBundleDownLoadUrl** | 热更清单与 AB 下载使用的 HTTP 根地址（热更流程）。 |
| **MAX_THREAD_COUNT** | 热更下载最大并发模块数相关（见 `HotAssetsManager`）。 |

### 1.4 物理路径（运行时读 AB 文件）

- **热更目录**：`Application.persistentDataPath + "/HotAssets/" + 模块名 + "/"`  
  在 **Hot** 且该路径下 **存在同名文件** 时，**优先**从这里 `LoadFromFile`。
- **内嵌目录**：`Application.streamingAssetsPath + "/AssetBundle/" + 模块名 + "/"`  
  热更文件不存在时回退（含 `windowbundleconfig.uab` 等）。
- **解压目录**：`Application.persistentDataPath + "/DecompressAssets/" + 模块名 + "/"`（热更校验 MD5 时的二次对照路径，见 `HotAssetsModule`）。

---

## 2. 推荐初始化顺序

```text
1. 确保场景中存在 ZMAsset（MonoSingleton），以便 Update 驱动热更下载等逻辑
2. ZMAsset.InitFrameWork();
3. （可选）热更：ZMAsset.CheckAssetsVersion / ZMAsset.HotAssets，完成后会 InitAssetsModule
4. 对每个要用到的模块：await ZMAsset.InitAssetsModule("Window");
5. 再调用 InstantiateObject / LoadScriptableObject / PreLoadResourceAsync 等
```

**注意**：`InitAssetsModule` 为 **`async UniTask<bool>`**，必须在 **`async` 方法中 `await`**，不可在同步方法里“ fire-and-forget ”后立刻加载资源，否则易出现表未注册导致的加载失败。

---

## 3. 静态 API 一览（`ZM.ZMAsset.ZMAsset`）

以下均在 **`namespace ZM.ZMAsset`**，需 **`using ZM.ZMAsset`**（及异步处 **`using Cysharp.Threading.Tasks`**）。

### 3.1 框架与模块

| API | 说明 |
|-----|------|
| `InitFrameWork()` | 初始化 `ResourceManager`、`HotAssetsManager`、对象池根节点等。 |
| `InitAssetsModule(string bundleModule)` | 加载该模块的 bundle 配置包，解析 JSON 填充 CRC 字典。**返回 `UniTask<bool>`**。 |
| `HotAssets(...)` | 按模块开始热更下载（见热更管线）。 |
| `CheckAssetsVersion(module, callback)` | 检测是否需要热更及大约体积。 |
| `GetHotAssetsModule(module)` | 取 `HotAssetsModule` 做进度等扩展。 |

### 3.2 实例化（GameObject）

| API | 说明 |
|-----|------|
| `InstantiateObject(path, parent)` | 同步实例化；内部会将未以 `.prefab` 结尾的 path **自动补 `.prefab`** 再算 CRC。 |
| `InstantiateObject(path, parent, localPos, localScale, rot)` | 同上，带初始变换。 |
| `InstantiateObjectAsync(path, parent, callback, p1, p2)` | 异步回调式。 |
| `InstantiateObjectAsync(path, parent, ...)` | **`async UniTask<AssetsRequest>`**，可 `await`。 |
| `InstantiateObjectAndLoad(...)` | 带下载等待的克隆（热更未完成时排队回调）。 |
| `PreLoadObjct(path, count)` | 预加载并实例化进池（同步）。 |
| `PreLoadObjectAsync<T>(path, count)` | 预加载异步。 |

### 3.3 资源加载（不实例化场景物体）

| API | 说明 |
|-----|------|
| `PreLoadResource<T>(path)` | 同步预加载到缓存。 |
| `PreLoadResourceAsync<T>(path)` | **`UniTask<T>`**，内部为 `LoadResourceAsync<T>`。 |
| `LoadScriptableObject<T>(fullPath)` | 未以 `.asset` 结尾会自动 **补 `.asset`**，再走 `LoadResource<T>`。 |
| `LoadSprite(path)` | 未以 `.png` 结尾会 **补 `.png`**。 |
| `LoadTexture(path)` | 未以 `.jpg` 结尾会 **补 `.jpg`**。 |
| `LoadAudio(path)` | 加载 `AudioClip`。 |
| `LoadTextAsset(fullPath)` | `TextAsset`。 |
| `LoadTextAssetAsync(fullPath)` | 异步 `TextAsset`。 |
| `LoadAtlasSprite(atlasPath, spriteName)` | 图集子图。 |
| `LoadPNGAtlasSprite(atlasPath, spriteName)` | TexturePacker 类图集。 |
| `LoadSceneAsync(fullPath, mode)` | 异步加场景；**Editor 模式**下需场景在 **Build Settings** 中。 |

### 3.4 异步图片（带回调或 UniTask）

| API | 说明 |
|-----|------|
| `LoadTextureAsync(path, callback, param)` | 返回 long 任务 id。 |
| `LoadTextureAsync(path)` | **`UniTask<Texture>`**（自动补 `.jpg`）。 |
| `LoadSpriteAsync(path, image, setNativeSize, callback)` | 绑定到 `Image`。 |
| `LoadSpriteAsync(path)` | **`UniTask<Sprite>`**（自动补 `.png`）。 |

### 3.5 释放与清理

| API | 说明 |
|-----|------|
| `Release(GameObject obj, bool destroy)` | 按框架引用计数释放/销毁克隆体。 |
| `Release(Texture texture)` | 释放纹理占用。 |
| `Release(AssetsRequest request)` | 释放异步请求包装。 |
| `RemoveObjectLoadCallBack(long loadId)` | 取消异步加载回调。 |
| `ClearAllAsyncLoadTask()` | 清空异步加载任务。 |
| `ClearResourcesAssets(bool absoluteCleaning)` | **true**：深度清理 AB 加载对象；**false**：偏对象池与引用计数策略。 |

---

## 4. 寻址资源（可选）

若使用 **Addressables / 寻址** 扩展，可通过 **`ZMAddressableAsset`**（见 `AddresAPIDemo.cs`）并传入 **`BundleModuleName.AdressAsset`** 等模块常量。与常规模块的 **`InitAssetsModule("Window")`** 流程不同，需按示例单独接入。

内置常量见 **`BundleModuleName`**（如 `AdressAsset`）。

---

## 5. 与热更的关系

- **`bundleHotType == Hot`** 时，具体 **`.uab`** 文件在 **热更目录存在** 则优先加载热更文件。
- **`HotAssets` / `CheckAssetsVersion`** 下载完成后，`HotAssetsManager` 会 **`await InitAssetsModule(bundleModule)`**，此时再加载该模块资源即指向新包。
- **`bundleHotType == NoHot`**：`HotAssets` 内会直接结束回调；加载主要依赖 **StreamingAssets 内嵌** 路径。

---

## 6. 常见问题

1. **`InitAssetsModule` 返回 false**  
   检查 `StreamingAssets/AssetBundle/{模块}/` 或热更目录是否存在 **`{模块小写}bundleconfig{ABSUFFIX}`**，以及 `BundleSettings` 中 **Hot / NoHot** 与编辑器回退逻辑是否匹配。

2. **加载返回 null、CRC 找不到**  
   路径字符串与 **assetbundleconfig.json** 中 **`path`** 不一致，或 **未先 InitAssetsModule**。

3. **预制体上脚本丢失**  
   多为 **AB 未重新打** 或 **StreamingAssets 内嵌文件过旧**，与逻辑路径无关。

4. **`ZMAsset` Update**  
   热更下载主线程回调依赖 **`ZMAsset` 实例的 `Update`**，请保证该组件在运行时常驻。

---

## 7. 相关文件索引

| 内容 | 路径 |
|------|------|
| 对外 API | `Runtime/ZMAsset.Interface.cs` |
| 框架入口 | `Runtime/ZMAsset.cs` |
| 加载与实例化 | `Runtime/BundleLoad/ResourceManager.cs` |
| AB 与 CRC | `Runtime/BundleLoad/AssetBundleManager.cs` |
| 全局配置 | `Config/BundleSettings.cs` |
| 热更 | `Runtime/BundleHot/HotAssetsManager.cs`、`HotAssetsModule.cs` |
| 打包与清单 | `Editor/BundleBuild/BuildBundleCompiler.cs` |

---

*文档随工程整理，若框架升级请以源码为准。*
