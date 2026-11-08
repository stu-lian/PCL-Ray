namespace PCL.Core.App.Configuration;

public record ConfigObserver(ConfigEvent Event, ConfigEventHandler Handler, bool IsPreview = false);
