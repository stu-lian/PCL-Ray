# PCL Ray

PCL Ray 是基于 [Plain Craft Launcher](https://github.com/Meloong-Git/PCL) 与 [PCL CE](https://github.com/PCL-Community/PCL-CE) 制作的 Minecraft 启动器改版。

- 上游项目：PCL CE（Apache License 2.0）
- 原作者：龙腾猫跃
- PCL Ray 改版作者：Stu_lian
- 仓库地址：https://github.com/stu-lian/PCL-Ray

## 功能概览

- 内置地图查看器（支持主世界 / 下界 / 末地，含结构标记）
- 陶瓦联机（Terracotta）
- AI 日志分析 / 模组翻译
- 内置 Minecraft 工具：召唤指令、盾牌设计、烟花设计、信标颜色计算、盔甲纹饰、玩家数据、极限存档复活、NBT 编辑器、Title 指令、头颅生成器
- 自动补齐模组前置依赖（可在设置中开关）
- 国际化的界面与主题

## 构建要求

- Windows 10 / 11（x64）
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- WebView2 Runtime（Windows 11 已内置）

## 构建

```powershell
dotnet build .\src\PCL2_CE_Release_x64.csproj -c Release -r win-x64
```

生成单文件可执行程序：

```powershell
dotnet publish .\src\PCL2_CE_Release_x64.csproj -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o .\publish
```

第三方依赖 DLL 位于仓库根目录的 `_bundle`，项目通过相对路径引用。

## 配置凭据

本仓库**不包含任何 API Key、Client ID 或密钥**。需要相关功能的开发者请自行通过环境变量提供：

| 环境变量 | 用途 |
| --- | --- |
| `PCL_MS_CLIENT_ID` | Microsoft OAuth 登录 |
| `PCL_CURSEFORGE_API_KEY` | CurseForge 模组源 |
| `PCL_AI_API_KEY` | AI 日志分析 / 模组翻译 |
| `PCL_AI_ENDPOINT` | AI 接口地址（必填，例如自建或第三方兼容 OpenAI 的接口） |
| `PCL_MINECRAFT_HEADS_API_KEY` | 头颅生成器 |
| `PCL_SENTRY_DSN` | Sentry 错误上报（可选） |
| `PCL_NAID_CLIENT_ID` / `PCL_NAID_CLIENT_SECRET` | Natayark 相关服务（可选） |
| `PCL_LINK_SERVER_ROOT` | 联机服务端地址（可选） |
| `PCL_GITHUB_SHA` | 构建提交号（可选） |

未配置对应变量时，相关功能会提示不可用，但不会影响启动器核心功能。

## 许可证

本项目基于 GNU General Public License v3.0 发布，详见 `LICENSE` 与 `NOTICE`。