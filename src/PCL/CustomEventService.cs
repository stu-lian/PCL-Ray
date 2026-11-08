using System.Windows;

namespace PCL;

public static class CustomEventService
{
	public static readonly DependencyProperty EventsProperty = DependencyProperty.RegisterAttached("Events", typeof(CustomEventCollection), typeof(CustomEventService), new PropertyMetadata((PropertyChangedCallback)null));

	public static readonly DependencyProperty EventTypeProperty = DependencyProperty.RegisterAttached("EventType", typeof(EventType), typeof(CustomEventService), new PropertyMetadata((object)EventType.None));

	public static readonly DependencyProperty EventDataProperty = DependencyProperty.RegisterAttached("EventData", typeof(string), typeof(CustomEventService), new PropertyMetadata((PropertyChangedCallback)null));

	[AttachedPropertyBrowsableForType(typeof(DependencyObject))]
	public static void SetEvents(DependencyObject d, CustomEventCollection value)
	{
		d.SetValue(EventsProperty, (object)value);
	}

	[AttachedPropertyBrowsableForType(typeof(DependencyObject))]
	public static CustomEventCollection GetEvents(DependencyObject d)
	{
		if (d.GetValue(EventsProperty) == null)
		{
			d.SetValue(EventsProperty, (object)new CustomEventCollection());
		}
		return (CustomEventCollection)d.GetValue(EventsProperty);
	}

	[AttachedPropertyBrowsableForType(typeof(DependencyObject))]
	public static void SetEventType(DependencyObject d, EventType value)
	{
		d.SetValue(EventTypeProperty, (object)value);
	}

	[AttachedPropertyBrowsableForType(typeof(DependencyObject))]
	public static EventType GetEventType(DependencyObject d)
	{
		return (EventType)d.GetValue(EventTypeProperty);
	}

	[AttachedPropertyBrowsableForType(typeof(DependencyObject))]
	public static void SetEventData(DependencyObject d, string value)
	{
		d.SetValue(EventDataProperty, (object)value);
	}

	[AttachedPropertyBrowsableForType(typeof(DependencyObject))]
	public static string GetEventData(DependencyObject d)
	{
		return (string)d.GetValue(EventDataProperty);
	}

	static CustomEventService()
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected Obj, but got Unknown
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected Obj, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected Obj, but got Unknown
	}
}
