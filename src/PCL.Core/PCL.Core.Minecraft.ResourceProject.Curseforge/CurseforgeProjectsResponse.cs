using System;
using System.Collections.Generic;

namespace PCL.Core.Minecraft.ResourceProject.Curseforge;

[Serializable]
public record CurseforgeProjectsResponse(List<CurseforgeProject> data);
