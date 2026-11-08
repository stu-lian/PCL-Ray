using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using PCL.Core.App;
using PCL.Core.App.Configuration;
using PCL.Core.App.Configuration.Storage;

namespace PCL;

/// <summary>
/// 实例文件夹复制、重命名与整合包更新时共用的辅助逻辑。
/// </summary>
internal static class ModInstanceHelper
{
    /// <summary>
    /// 把实例文件夹内以旧实例名命名的 JSON、Jar、Natives 文件夹改为新实例名，并修正 PCL\Setup.ini 中的路径。
    /// </summary>
    public static void RenameInstanceFiles(string targetPath, string oldName, string newName, string oldPathForIni)
    {
        if (!targetPath.EndsWith("\\", StringComparison.Ordinal))
        {
            targetPath += "\\";
        }
        if (string.Equals(oldName, newName, StringComparison.Ordinal))
        {
            return;
        }

        string newJson = Path.Combine(targetPath, newName + ".json");
        string oldJson = Path.Combine(targetPath, oldName + ".json");
        if (!File.Exists(oldJson))
        {
            foreach (string candidate in Directory.GetFiles(targetPath, "*.json", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    if (ModBase.GetJson(ModBase.ReadFile(candidate)) is JsonObject candidateObject &&
                        string.Equals(candidateObject["id"]?.ToString(), oldName, StringComparison.OrdinalIgnoreCase))
                    {
                        oldJson = candidate;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    ModBase.Log(ex, "检查实例 JSON 时失败：" + candidate);
                }
            }
        }
        if (File.Exists(oldJson))
        {
            JsonObject json = (JsonObject)ModBase.GetJson(ModBase.ReadFile(oldJson));
            json["id"] = newName;
            ModBase.WriteFile(newJson, json.ToString());
            if (!string.Equals(Path.GetFullPath(oldJson), Path.GetFullPath(newJson), StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(oldJson);
            }
        }

        string oldJar = Path.Combine(targetPath, oldName + ".jar");
        string newJar = Path.Combine(targetPath, newName + ".jar");
        if (File.Exists(oldJar) && !string.Equals(oldJar, newJar, StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(newJar))
            {
                File.Delete(newJar);
            }
            File.Move(oldJar, newJar);
        }

        string oldNatives = Path.Combine(targetPath, oldName + "-natives");
        string newNatives = Path.Combine(targetPath, newName + "-natives");
        if (Directory.Exists(oldNatives) && !string.Equals(oldNatives, newNatives, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(newNatives))
            {
                ModBase.DeleteDirectory(newNatives);
            }
            Directory.Move(oldNatives, newNatives);
        }

        string setupIni = Path.Combine(targetPath, "PCL", "Setup.ini");
        if (File.Exists(setupIni))
        {
            string content = ModBase.ReadFile(setupIni);
            string newPath = targetPath;
            if (!oldPathForIni.EndsWith("\\", StringComparison.Ordinal))
            {
                oldPathForIni += "\\";
            }
            if (!newPath.EndsWith("\\", StringComparison.Ordinal))
            {
                newPath += "\\";
            }
            string replaced = content.Replace(oldPathForIni, newPath, StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(content, replaced, StringComparison.Ordinal))
            {
                ModBase.WriteFile(setupIni, replaced);
            }
            ModBase.IniClearCache(setupIni);
        }
    }

    /// <summary>
    /// 只复制目标中不存在的文件，用于把旧实例的配置、资源包等用户数据合并进新实例。
    /// </summary>
    public static void MergeDirectory(string source, string target, ModLoader.LoaderBase loader, double weight)
    {
        if (!Directory.Exists(source))
        {
            if (loader != null)
            {
                loader.Progress += weight;
            }
            return;
        }
        List<string> files;
        try
        {
            files = Directory.GetFiles(source, "*", SearchOption.AllDirectories).ToList();
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "枚举用户数据文件夹失败：" + source);
            if (loader != null)
            {
                loader.Progress += weight;
            }
            return;
        }
        if (files.Count == 0)
        {
            if (loader != null)
            {
                loader.Progress += weight;
            }
            return;
        }
        double step = weight / files.Count;
        foreach (string file in files)
        {
            try
            {
                string relative = Path.GetRelativePath(source, file);
                string destination = Path.Combine(target, relative);
                if (!File.Exists(destination))
                {
                    ModBase.CopyFile(file, destination);
                }
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "合并用户数据文件失败：" + file);
            }
            if (loader != null)
            {
                loader.Progress += step;
            }
        }
    }

    /// <summary>
    /// 生成不会被实例列表识别为正式实例的临时实例名。
    /// </summary>
    public static string BuildTempInstanceName(string instanceName)
    {
        string baseName = instanceName + "-update-temp";
        string candidate = baseName;
        int index = 1;
        while (Directory.Exists(Path.Combine(ModFolder.mcFolderSelected, "versions", candidate)))
        {
            index++;
            candidate = baseName + "-" + index;
        }
        return candidate;
    }

    /// <summary>
    /// 读取上次安装的整合包文件 ID 与发布时间，用于精确判断是否有新版本。
    /// </summary>
    public static string ReadInstalledFileMarker(string instancePath)
    {
        try
        {
            string marker = Path.Combine(instancePath, "PCL", "ModpackUpdate.txt");
            if (File.Exists(marker))
            {
                string[] parts = ModBase.ReadFile(marker).Split('|');
                if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                {
                    return parts[0].Trim();
                }
            }
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "读取整合包更新标记失败");
        }
        return "";
    }

    /// <summary>
    /// 记录本次安装的整合包文件 ID、发布时间与版本号。
    /// </summary>
    public static void WriteInstalledFileMarker(string instancePath, ModComp.CompFile file)
    {
        try
        {
            string marker = Path.Combine(instancePath, "PCL", "ModpackUpdate.txt");
            string content = file.Id + "|" + file.ReleaseDate.ToString("O") + "|" + (file.Version ?? file.DisplayName ?? "");
            ModBase.WriteFile(marker, content);
        }
        catch (Exception ex)
        {
            ModBase.Log(ex, "写入整合包更新标记失败");
        }
    }

    /// <summary>
    /// 判断整合包文件是否与当前记录的版本一致。
    /// </summary>
    public static bool IsSameModpackVersion(ModComp.CompFile file, string currentVersion, bool fromCurseForge)
    {
        if (file == null || string.IsNullOrWhiteSpace(currentVersion))
        {
            return false;
        }
        if (string.Equals(file.Id, currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (!string.IsNullOrWhiteSpace(file.Version) &&
            string.Equals(file.Version, currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (!string.IsNullOrWhiteSpace(file.DisplayName) &&
            (file.DisplayName.Contains(currentVersion, StringComparison.OrdinalIgnoreCase) ||
             currentVersion.Contains(file.DisplayName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 把旧实例的用户数据合并进新实例。旧实例未开启版本隔离时，用户数据位于游戏根目录。
    /// </summary>
    public static void MergeUserData(string oldPath, string newPath, ModLoader.LoaderBase task, double weight)
    {
        if (!oldPath.EndsWith("\\", StringComparison.Ordinal))
        {
            oldPath += "\\";
        }
        if (!newPath.EndsWith("\\", StringComparison.Ordinal))
        {
            newPath += "\\";
        }

        string oldConfig = Path.Combine(oldPath, "PCL", "config.v1.yml");
        string newConfig = Path.Combine(newPath, "PCL", "config.v1.yml");
        DynamicCacheConfigStorage provider = (DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance);

        // 先把整合包安装写入的版本信息刷到磁盘，再记录下来，避免被旧实例的配置覆盖。
        provider.InvalidateCache(newPath);
        string modpackVersion = States.Instance.ModpackVersion[newPath];
        string modpackSource = States.Instance.ModpackSource[newPath];
        string modpackId = States.Instance.ModpackId[newPath];
        string customInfo = States.Instance.CustomInfo[newPath];

        string oldIndiePath = new McInstance(oldPath).PathIndie;
        if (!oldIndiePath.EndsWith("\\", StringComparison.Ordinal))
        {
            oldIndiePath += "\\";
        }

        // 旧实例的配置写入是异步的，先落盘再复制，避免丢失最近修改的设置。
        provider.InvalidateCache(oldPath);
        if (File.Exists(oldConfig))
        {
            provider.InvalidateCache(newPath);
            ModBase.CopyFile(oldConfig, newConfig);
        }
        provider.InvalidateCache(newPath);
        Config.Instance.IndieV2[newPath] = true;
        if (!string.IsNullOrWhiteSpace(modpackVersion))
        {
            States.Instance.ModpackVersion[newPath] = modpackVersion;
        }
        if (!string.IsNullOrWhiteSpace(modpackSource))
        {
            States.Instance.ModpackSource[newPath] = modpackSource;
        }
        if (!string.IsNullOrWhiteSpace(modpackId))
        {
            States.Instance.ModpackId[newPath] = modpackId;
        }
        if (!string.IsNullOrWhiteSpace(customInfo))
        {
            States.Instance.CustomInfo[newPath] = customInfo;
        }

        string oldLogo = Path.Combine(oldPath, "PCL", "Logo.png");
        if (File.Exists(oldLogo))
        {
            ModBase.CopyFile(oldLogo, Path.Combine(newPath, "PCL", "Logo.png"));
        }

        double savesWeight = weight * 0.5;
        double filesWeight = weight * 0.1;
        double foldersWeight = weight * 0.4;

        string saves = Path.Combine(oldIndiePath, "saves");
        if (Directory.Exists(saves))
        {
            ModBase.CopyDirectory(saves, Path.Combine(newPath, "saves"), delta => task.Progress += delta * savesWeight);
        }
        else
        {
            task.Progress += savesWeight;
        }

        string[] rootFiles = { "options.txt", "optionsof.txt", "servers.dat", "servers.dat_old", "usercache.json" };
        double fileStep = filesWeight / rootFiles.Length;
        foreach (string fileName in rootFiles)
        {
            string source = Path.Combine(oldIndiePath, fileName);
            if (File.Exists(source))
            {
                ModBase.CopyFile(source, Path.Combine(newPath, fileName));
            }
            task.Progress += fileStep;
        }

        string[] dataFolders = { "config", "screenshots", "schematics", "blueprints", "journeymap", "xaero", ".xaero", "map", "local", "resourcepacks", "shaderpacks", "datapacks" };
        double folderStep = foldersWeight / dataFolders.Length;
        foreach (string folder in dataFolders)
        {
            MergeDirectory(Path.Combine(oldIndiePath, folder), Path.Combine(newPath, folder), task, folderStep);
        }
    }

    /// <summary>
    /// 用临时实例替换旧实例，并把旧实例移动到备份目录。
    /// </summary>
    public static string ReplaceInstance(string instanceName, string instancePath, string tempName, string tempPath)
    {
        string backupRoot = Path.Combine(ModFolder.mcFolderSelected, "PCL", "InstanceBackups");
        Directory.CreateDirectory(backupRoot);
        string backupPath = Path.Combine(backupRoot, instanceName + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        string oldPath = instancePath.TrimEnd('\\');
        string newPath = tempPath.TrimEnd('\\');

        // 两个实例的配置写入都是异步的，移动目录前必须落盘。
        DynamicCacheConfigStorage provider = (DynamicCacheConfigStorage)ConfigService.GetProvider(ConfigSource.GameInstance);
        provider.InvalidateCache(instancePath);
        provider.InvalidateCache(tempPath);

        if (Directory.Exists(oldPath))
        {
            if (Directory.Exists(backupPath))
            {
                ModBase.DeleteDirectory(backupPath);
            }
            Directory.Move(oldPath, backupPath);
        }
        try
        {
            if (Directory.Exists(oldPath))
            {
                ModBase.DeleteDirectory(oldPath);
            }
            Directory.Move(newPath, oldPath);
        }
        catch
        {
            try
            {
                if (Directory.Exists(oldPath))
                {
                    ModBase.DeleteDirectory(oldPath);
                }
                if (Directory.Exists(backupPath))
                {
                    Directory.Move(backupPath, oldPath);
                }
            }
            catch (Exception ex)
            {
                ModBase.Log(ex, "回滚整合包更新失败，备份位于：" + backupPath);
            }
            throw;
        }

        RenameInstanceFiles(oldPath + "\\", tempName, instanceName, tempPath);
        provider.InvalidateCache(oldPath + "\\");
        provider.InvalidateCache(tempPath);
        ModBase.Log("[ModPack] 整合包更新完成，旧实例已备份至：" + backupPath);
        return backupPath;
    }
}
