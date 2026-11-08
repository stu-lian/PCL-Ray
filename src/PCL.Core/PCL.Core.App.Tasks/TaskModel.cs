using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using CommunityToolkit.Mvvm.Input;

namespace PCL.Core.App.Tasks;

public class TaskModel : ObservableObject
{
	[ObservableProperty]
	private TaskState _state;

	[ObservableProperty]
	private string _stateMessage = string.Empty;

	[ObservableProperty]
	private double _progress;

	private static readonly Action _EmptyAction = () =>
	{
	};

	[ObservableProperty]
	private bool _isGroup;

	public required string Title { get; init; }

	public required bool SupportProgress { get; init; }

	public required Action? OnCancel { private get; init; }

	public RelayCommand Cancel
	{
		get
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Expected Obj, but got Unknown
			//IL_0032: Expected Obj, but got Unknown
			RelayCommand obj = field;
			if (obj == null)
			{
				RelayCommand val = new RelayCommand(OnCancel ?? _EmptyAction, (Func<bool>)(() => OnCancel != null));
				RelayCommand val2 = val;
				field = val;
				obj = val2;
			}
			return obj;
		}
	}

	public required Action? OnPause { private get; init; }

	public RelayCommand Pause
	{
		get
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Expected Obj, but got Unknown
			//IL_0032: Expected Obj, but got Unknown
			RelayCommand obj = field;
			if (obj == null)
			{
				RelayCommand val = new RelayCommand(OnPause ?? _EmptyAction, (Func<bool>)(() => OnPause != null));
				RelayCommand val2 = val;
				field = val;
				obj = val2;
			}
			return obj;
		}
	}

	public ObservableCollection<TaskModel> Children { get; } = new ObservableCollection<TaskModel>();

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public TaskState State
	{
		get
		{
			return _state;
		}
		set
		{
			if (!EqualityComparer<TaskState>.Default.Equals(_state, value))
			{
				this.OnPropertyChanging(__KnownINotifyPropertyChangingArgs.State);
				_state = value;
				this.OnPropertyChanged(__KnownINotifyPropertyChangedArgs.State);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public string StateMessage
	{
		get
		{
			return _stateMessage;
		}
		[MemberNotNull("_stateMessage")]
		set
		{
			if (!EqualityComparer<string>.Default.Equals(_stateMessage, value))
			{
				this.OnPropertyChanging(__KnownINotifyPropertyChangingArgs.StateMessage);
				_stateMessage = value;
				this.OnPropertyChanged(__KnownINotifyPropertyChangedArgs.StateMessage);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public double Progress
	{
		get
		{
			return _progress;
		}
		set
		{
			if (!EqualityComparer<double>.Default.Equals(_progress, value))
			{
				this.OnPropertyChanging(__KnownINotifyPropertyChangingArgs.Progress);
				_progress = value;
				this.OnPropertyChanged(__KnownINotifyPropertyChangedArgs.Progress);
			}
		}
	}

	[GeneratedCode("CommunityToolkit.Mvvm.SourceGenerators.ObservablePropertyGenerator", "8.4.0.0")]
	[ExcludeFromCodeCoverage]
	public bool IsGroup
	{
		get
		{
			return _isGroup;
		}
		set
		{
			if (!EqualityComparer<bool>.Default.Equals(_isGroup, value))
			{
				this.OnPropertyChanging(__KnownINotifyPropertyChangingArgs.IsGroup);
				_isGroup = value;
				this.OnPropertyChanged(__KnownINotifyPropertyChangedArgs.IsGroup);
			}
		}
	}

	public TaskModel()
	{
		Children.CollectionChanged += (object? sender, NotifyCollectionChangedEventArgs _) =>
		{
			if (sender is ObservableCollection<TaskModel> observableCollection)
			{
				IsGroup = observableCollection.Count > 0;
			}
		};
	}
}
