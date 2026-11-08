using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PCL;

public class MyPageRight : AdornerDecorator
{
	public enum PageStates
	{
		Empty,
		LoaderWait,
		LoaderEnter,
		LoaderStayForce,
		LoaderStay,
		LoaderExit,
		ContentEnter,
		ContentStay,
		ContentExit,
		PageExit
	}

	public delegate void PageEnterEventHandler();

	public delegate void PageExitEventHandler();

	private static readonly DependencyProperty PanScrollProperty = DependencyProperty.Register("PanScroll", typeof(MyScrollViewer), typeof(MyPageRight));

	private bool _panScrollNullWarned;

	public int pageUuid = ModBase.GetUuid();

	private ModLoader.LoaderBase pageLoader;

	private Func<object>? pageLoaderInputInvoke;

	private MyLoading? pageLoaderUi;

	private FrameworkElement panLoader;

	private FrameworkElement panContent;

	private FrameworkElement? panAlways;

	private bool pageLoaderAutoRun;

	public List<FrameworkElement> disabledPageAnimControls = new List<FrameworkElement>();

	public MyScrollViewer PanScroll
	{
		get
		{
			object value = ((DependencyObject)this).GetValue(PanScrollProperty);
			if (value == null && !_panScrollNullWarned)
			{
				_panScrollNullWarned = true;
				ModBase.Log("[MyPageRight] 获取到 PanScroll(来自 " + Name + ") 的值为 null", ModBase.LogLevel.Debug);
			}
			return (MyScrollViewer)value;
		}
		set
		{
			((DependencyObject)this).SetValue(PanScrollProperty, (object)value);
		}
	}

	public PageStates PageState
	{
		get
		{
			return field;
		}
		set
		{
			if (field != value)
			{
				field = value;
				if (ModBase.modeDebug)
				{
					ModBase.Log("[UI] 页面状态切换为 " + ModBase.GetStringFromEnum(value));
				}
			}
		}
	}

	public event PageEnterEventHandler? PageEnter;

	public event PageExitEventHandler? PageExit;

	public void PageLoaderInit(MyLoading loaderUi, FrameworkElement panLoader, FrameworkElement panContent, FrameworkElement? panAlways, ModLoader.LoaderBase realLoader, Action<ModLoader.LoaderBase>? finishedInvoke = null, Func<object>? inputInvoke = null, bool autoRun = true)
	{
		this.panLoader = panLoader;
		this.panContent = panContent;
		this.panAlways = panAlways;
		pageLoader = realLoader;
		pageLoaderUi = loaderUi;
		pageLoaderInputInvoke = inputInvoke;
		pageLoaderAutoRun = autoRun;
		if (finishedInvoke != null)
		{
			realLoader.PreviewFinish += (ModLoader.LoaderBase _) =>
			{
				while (PageState == PageStates.PageExit || PageState == PageStates.ContentExit)
				{
					Thread.Sleep(10);
				}
				ModBase.RunInUiWait(() =>
				{
					finishedInvoke(realLoader);
				});
				Thread.Sleep(20);
			};
		}
		realLoader.OnStateChangedUi += (ModLoader.LoaderBase loader, ModBase.LoadState newState, ModBase.LoadState oldState) =>
		{
			ModBase.RunInUi(() =>
			{
				PageLoaderState(loader, newState, oldState);
			});
		};
		panLoader.Visibility = Visibility.Collapsed;
		panContent.Visibility = Visibility.Collapsed;
		if (panAlways != null)
		{
			panAlways.Visibility = Visibility.Collapsed;
		}
		if (pageLoaderAutoRun)
		{
			if (pageLoader is ModLoader.LoaderTask loaderTask)
			{
				loaderTask.Start(loaderTask.StartGetInputNoType(null, pageLoaderInputInvoke));
			}
			else
			{
				object input = null;
				if (pageLoaderInputInvoke != null)
				{
					input = pageLoaderInputInvoke();
				}
				pageLoader.Start(input);
			}
		}
		if (pageLoader.State == ModBase.LoadState.Finished && finishedInvoke != null)
		{
			ModBase.RunInUiWait(() =>
			{
				finishedInvoke(realLoader);
			});
		}
		pageLoaderUi.State = realLoader;
		pageLoaderUi.Click += (object _, MouseButtonEventArgs _) =>
		{
			if (realLoader.State == ModBase.LoadState.Failed)
			{
				PageLoaderRestart();
			}
		};
	}

	public void PageLoaderRestart(object input = null, bool isForceRestart = true)
	{
		if (!pageLoaderAutoRun)
		{
			return;
		}
		if (pageLoader is ModLoader.LoaderTask loaderTask)
		{
			pageLoader.Start(loaderTask.StartGetInputNoType(input, pageLoaderInputInvoke), isForceRestart);
			return;
		}
		if (input == null && pageLoaderInputInvoke != null)
		{
			input = pageLoaderInputInvoke;
		}
		pageLoader.Start(input, isForceRestart);
	}

	public void PageOnEnter()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnEnter");
		}
		PageEnter?.Invoke();
		switch (PageState)
		{
		case PageStates.Empty:
			if (pageLoader == null || pageLoader.State == ModBase.LoadState.Finished || pageLoader.State == ModBase.LoadState.Waiting || pageLoader.State == ModBase.LoadState.Aborted)
			{
				PageState = PageStates.ContentEnter;
				TriggerEnterAnimation(panAlways, (FrameworkElement)(panContent ?? Child));
			}
			else if (pageLoader.State == ModBase.LoadState.Loading)
			{
				PageState = PageStates.LoaderWait;
				ModAnimation.AniStart(ModAnimation.AaCode(PageOnLoaderWaitFinished, 400), "PageRight PageChange " + pageUuid);
			}
			else
			{
				PageState = PageStates.LoaderEnter;
				TriggerEnterAnimation(panAlways, panLoader);
			}
			break;
		case PageStates.ContentExit:
			if (pageLoader == null || pageLoader.State == ModBase.LoadState.Finished || pageLoader.State == ModBase.LoadState.Waiting || pageLoader.State == ModBase.LoadState.Aborted)
			{
				PageState = PageStates.ContentEnter;
				TriggerEnterAnimation((FrameworkElement)(panContent ?? Child));
			}
			else if (pageLoader.State == ModBase.LoadState.Loading)
			{
				PageState = PageStates.LoaderWait;
				ModAnimation.AniStart(ModAnimation.AaCode(PageOnLoaderWaitFinished, 400), "PageRight PageChange " + pageUuid);
			}
			else
			{
				PageState = PageStates.LoaderEnter;
				TriggerEnterAnimation(panLoader);
			}
			break;
		default:
			throw new Exception("在状态为 " + ModBase.GetStringFromEnum(PageState) + " 时触发了 PageOnEnter 事件。");
		case PageStates.ContentEnter:
			break;
		}
	}

	public void PageOnExit()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnExit");
		}
		PageExit?.Invoke();
		switch (PageState)
		{
		case PageStates.ContentEnter:
		case PageStates.ContentStay:
			PageState = PageStates.PageExit;
			TriggerExitAnimation(panAlways, (FrameworkElement)(panContent ?? Child));
			break;
		case PageStates.LoaderEnter:
		case PageStates.LoaderStayForce:
		case PageStates.LoaderStay:
			PageState = PageStates.PageExit;
			TriggerExitAnimation(panAlways, panLoader);
			break;
		case PageStates.LoaderWait:
			PageState = PageStates.PageExit;
			TriggerExitAnimation(panAlways);
			break;
		case PageStates.LoaderExit:
		case PageStates.ContentExit:
			PageState = PageStates.PageExit;
			if (panAlways != null)
			{
				TriggerExitAnimation(panAlways, (FrameworkElement)(panContent ?? Child));
			}
			break;
		case PageStates.Empty:
		case PageStates.PageExit:
			break;
		}
	}

	public void PageOnForceExit()
	{
		if (PageState == PageStates.Empty)
		{
			return;
		}
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnForceExit");
		}
		PageState = PageStates.Empty;
		ModAnimation.AniStop("PageRight PageChange " + pageUuid);
		if (pageLoader == null && Child != null)
		{
			Child.Visibility = Visibility.Collapsed;
			return;
		}
		panContent.Visibility = Visibility.Collapsed;
		panLoader.Visibility = Visibility.Collapsed;
		if (panAlways != null)
		{
			panAlways.Visibility = Visibility.Collapsed;
		}
	}

	public void PageOnContentExit()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnContentExit");
		}
		if (pageLoader != null && pageLoader.State == ModBase.LoadState.Loading)
		{
			throw new Exception("在调用 PageOnContentExit 时，加载器不能为 Loading 状态");
		}
		switch (PageState)
		{
		case PageStates.ContentEnter:
		case PageStates.ContentStay:
			PageState = PageStates.ContentExit;
			TriggerExitAnimation(panContent);
			break;
		case PageStates.LoaderExit:
			PageState = PageStates.ContentExit;
			break;
		case PageStates.LoaderEnter:
		case PageStates.LoaderStayForce:
		case PageStates.LoaderStay:
			PageState = PageStates.ContentExit;
			TriggerExitAnimation(panLoader);
			break;
		case PageStates.Empty:
		case PageStates.LoaderWait:
			PageOnEnter();
			break;
		}
	}

	private void PageOnEnterAnimationFinished()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnEnterAnimationFinished");
		}
		switch (PageState)
		{
		case PageStates.ContentEnter:
			PageState = PageStates.ContentStay;
			break;
		case PageStates.LoaderEnter:
			PageState = PageStates.LoaderStayForce;
			ModAnimation.AniStart(ModAnimation.AaCode(PageOnLoaderStayFinished, 400), "PageRight PageChange " + pageUuid);
			break;
		default:
			throw new Exception("在状态为 " + ModBase.GetStringFromEnum(PageState) + " 时触发了 PageOnEnterAnimationFinished 事件。");
		}
	}

	private void PageOnExitAnimationFinished()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnExitAnimationFinished");
		}
		switch (PageState)
		{
		case PageStates.PageExit:
			PageState = PageStates.Empty;
			break;
		case PageStates.ContentExit:
			PageOnEnter();
			break;
		case PageStates.LoaderExit:
			PageState = PageStates.ContentEnter;
			TriggerEnterAnimation(panContent);
			break;
		default:
			throw new Exception("在状态为 " + ModBase.GetStringFromEnum(PageState) + " 时触发了 PageOnExitAnimationFinished 事件。");
		}
	}

	private void PageOnLoaderWaitFinished()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnLoaderWaitFinished");
		}
		if (PageState == PageStates.LoaderWait)
		{
			PageState = PageStates.LoaderEnter;
			if (panAlways != null && panAlways.Visibility == Visibility.Collapsed)
			{
				TriggerEnterAnimation(panAlways, panLoader);
			}
			else
			{
				TriggerEnterAnimation(panLoader);
			}
			return;
		}
		throw new Exception("在状态为 " + ModBase.GetStringFromEnum(PageState) + " 时触发了 PageOnLoaderWaitFinished 事件。");
	}

	private void PageOnLoaderStayFinished()
	{
		if (ModBase.modeDebug)
		{
			ModBase.Log("[UI] 已触发 PageOnLoaderStayFinished");
		}
		if (PageState == PageStates.LoaderStayForce)
		{
			if (pageLoader.State == ModBase.LoadState.Finished)
			{
				PageState = PageStates.LoaderExit;
				TriggerExitAnimation(panLoader);
			}
			else
			{
				PageState = PageStates.LoaderStay;
			}
			return;
		}
		throw new Exception("在状态为 " + ModBase.GetStringFromEnum(PageState) + " 时触发了 PageOnLoaderWaitFinished 事件。");
	}

	private void PageLoaderState(object sender, ModBase.LoadState newState, ModBase.LoadState oldState)
	{
		switch (newState)
		{
		case ModBase.LoadState.Loading:
		case ModBase.LoadState.Failed:
			if (oldState != ModBase.LoadState.Failed && oldState != ModBase.LoadState.Loading)
			{
				if (ModBase.modeDebug)
				{
					ModBase.Log("[UI] 已触发 PageLoaderState (Start/Refresh)");
				}
				switch (PageState)
				{
				case PageStates.ContentEnter:
				case PageStates.ContentStay:
					PageState = PageStates.ContentExit;
					TriggerExitAnimation(panContent);
					break;
				case PageStates.LoaderExit:
					PageState = PageStates.ContentExit;
					break;
				}
			}
			break;
		case ModBase.LoadState.Waiting:
		case ModBase.LoadState.Finished:
		case ModBase.LoadState.Aborted:
			if (oldState != ModBase.LoadState.Failed && oldState != ModBase.LoadState.Loading)
			{
				break;
			}
			if (ModBase.modeDebug)
			{
				ModBase.Log("[UI] 已触发 PageLoaderState (Stop/Abort)");
			}
			switch (PageState)
			{
			case PageStates.LoaderWait:
				PageState = PageStates.ContentEnter;
				if (panAlways != null && panAlways.Visibility == Visibility.Collapsed)
				{
					TriggerEnterAnimation(panAlways, panContent);
				}
				else
				{
					TriggerEnterAnimation(panContent);
				}
				break;
			case PageStates.LoaderStay:
				PageState = PageStates.LoaderExit;
				TriggerExitAnimation(panLoader);
				break;
			}
			break;
		}
	}

	public void TriggerEnterAnimation(params FrameworkElement[] elements)
	{
		IEnumerable<FrameworkElement> enumerable = elements.Where((FrameworkElement e) => e != null);
		foreach (FrameworkElement item in enumerable)
		{
			item.Visibility = Visibility.Visible;
		}
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		int num = 0;
		foreach (FrameworkElement item2 in enumerable)
		{
			foreach (FrameworkElement allAnimControl in GetAllAnimControls(item2, ignoreInvisibility: true))
			{
				allAnimControl.IsHitTestVisible = true;
				if (allAnimControl.RenderTransform is TranslateTransform)
				{
					allAnimControl.RenderTransform = null;
				}
			}
			foreach (FrameworkElement allAnimControl2 in GetAllAnimControls(item2))
			{
				if (allAnimControl2 is MyExtraTextButton)
				{
					((MyExtraTextButton)allAnimControl2).Show = true;
					continue;
				}
				allAnimControl2.Opacity = 0.0;
				allAnimControl2.RenderTransform = new TranslateTransform(0.0, -16.0);
				list.Add(ModAnimation.AaOpacity(allAnimControl2, 1.0, 100, num, new ModAnimation.AniEaseOutFluent(ModAnimation.AniEasePower.Weak)));
				list.Add(ModAnimation.AaTranslateY(allAnimControl2, 5.0, 250, num, new ModAnimation.AniEaseOutFluent()));
				list.Add(ModAnimation.AaTranslateY(allAnimControl2, 11.0, 350, num, new ModAnimation.AniEaseOutBack()));
				num += 25;
			}
		}
		MyScrollBar firstScrollViewer = GetFirstScrollViewer(enumerable);
		if (firstScrollViewer != null)
		{
			if (!(firstScrollViewer.RenderTransform is TranslateTransform))
			{
				firstScrollViewer.RenderTransform = new TranslateTransform(10.0, 0.0);
			}
			list.Add(ModAnimation.AaTranslateX(firstScrollViewer, 0.0 - ((TranslateTransform)firstScrollViewer.RenderTransform).X, 350, 0, new ModAnimation.AniEaseOutFluent()));
		}
		list.Add(ModAnimation.AaCode(() =>
		{
			PageOnEnterAnimationFinished();
		}, 0, after: true));
		ModAnimation.AniStart(list, "PageRight PageChange " + pageUuid, refreshTime: true);
	}

	public void TriggerExitAnimation(params FrameworkElement[] elements)
	{
		IEnumerable<FrameworkElement> realElements = elements.Where((FrameworkElement e) => e != null);
		List<ModAnimation.AniData> list = new List<ModAnimation.AniData>();
		int num = 0;
		foreach (FrameworkElement item in realElements)
		{
			foreach (FrameworkElement allAnimControl in GetAllAnimControls(item))
			{
				if (allAnimControl is MyExtraTextButton)
				{
					((MyExtraTextButton)allAnimControl).Show = false;
					continue;
				}
				allAnimControl.IsHitTestVisible = false;
				list.Add(ModAnimation.AaOpacity(allAnimControl, -1.0, 70, num));
				list.Add(ModAnimation.AaTranslateY(allAnimControl, -6.0, 70, num));
				num += 15;
			}
		}
		MyScrollBar firstScrollViewer = GetFirstScrollViewer(realElements);
		if (firstScrollViewer != null)
		{
			if (!(firstScrollViewer.RenderTransform is TranslateTransform))
			{
				firstScrollViewer.RenderTransform = new TranslateTransform();
			}
			list.Add(ModAnimation.AaTranslateX(firstScrollViewer, 10.0 - ((TranslateTransform)firstScrollViewer.RenderTransform).X, 90, 0, new ModAnimation.AniEaseInFluent()));
		}
		list.Add(ModAnimation.AaCode(() =>
		{
			foreach (FrameworkElement item2 in realElements)
			{
				item2.Visibility = Visibility.Collapsed;
			}
			PageOnExitAnimationFinished();
		}, 0, after: true));
		ModAnimation.AniStart(list, "PageRight PageChange " + pageUuid);
	}

	internal IEnumerable<FrameworkElement> GetAllAnimControls(FrameworkElement element, bool ignoreInvisibility = false)
	{
		List<FrameworkElement> allControls = new List<FrameworkElement>();
		_GetAllAnimControls(element, ref allControls, ignoreInvisibility);
		return allControls.Except(disabledPageAnimControls);
	}

	private void _GetAllAnimControls(FrameworkElement element, ref List<FrameworkElement> allControls, bool ignoreInvisibility)
	{
		if (!ignoreInvisibility && element.Visibility == Visibility.Collapsed)
		{
			return;
		}
		if (element is MyCard || element is MyHint || element is MyExtraTextButton || element is TextBlock || element is MyTextButton)
		{
			allControls.Add(element);
		}
		else if (element is ContentControl)
		{
			object content = ((ContentControl)element).Content;
			if (content is FrameworkElement)
			{
				_GetAllAnimControls((FrameworkElement)content, ref allControls, ignoreInvisibility);
			}
		}
		else
		{
			if (!(element is Panel))
			{
				return;
			}
			foreach (object child in ((Panel)element).Children)
			{
				if (child is FrameworkElement)
				{
					_GetAllAnimControls((FrameworkElement)child, ref allControls, ignoreInvisibility);
				}
			}
		}
	}

	private MyScrollBar GetFirstScrollViewer(IEnumerable<FrameworkElement> elements)
	{
		foreach (FrameworkElement element in elements)
		{
			if (element is MyScrollViewer myScrollViewer)
			{
				if (myScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
				{
					return myScrollViewer.scrollBar;
				}
				continue;
			}
			foreach (object child in LogicalTreeHelper.GetChildren(element))
			{
				if (child is MyScrollViewer myScrollViewer2)
				{
					if (myScrollViewer2.ComputedVerticalScrollBarVisibility != Visibility.Visible)
					{
						return null;
					}
					return myScrollViewer2.scrollBar;
				}
			}
		}
		return null;
	}
}
