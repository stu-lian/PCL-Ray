using System;
using System.Collections.Generic;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace PCL.Core.App.Configuration.Storage;

public sealed class YamlNodeEmitter : IEmitter
{
	private enum ContainerKind
	{
		Document,
		Mapping,
		Sequence
	}

	private struct Container
	{
		public ContainerKind Kind;

		public YamlNode? Node;

		public YamlNode? PendingKey;

		public static Container Document()
		{
			return new Container
			{
				Kind = ContainerKind.Document,
				Node = null,
				PendingKey = null
			};
		}

		public static Container Mapping(YamlMappingNode map)
		{
			return new Container
			{
				Kind = ContainerKind.Mapping,
				Node = (YamlNode?)(object)map,
				PendingKey = null
			};
		}

		public static Container Sequence(YamlSequenceNode seq)
		{
			return new Container
			{
				Kind = ContainerKind.Sequence,
				Node = (YamlNode?)(object)seq,
				PendingKey = null
			};
		}
	}

	private readonly List<YamlDocument> _documents = new List<YamlDocument>();

	private readonly Stack<Container> _stack = new Stack<Container>();

	private readonly Dictionary<string, YamlNode> _anchors = new Dictionary<string, YamlNode>(StringComparer.Ordinal);

	private bool _inStream;

	public IReadOnlyList<YamlDocument> Documents => _documents;

	public YamlNode SingleRootNode => _documents.Count switch
	{
		0 => throw new InvalidOperationException("尚未产生任何文档。"), 
		1 => _documents[0].RootNode, 
		_ => throw new InvalidOperationException("存在多个文档，请使用 Documents 访问。"), 
	};

	public void Reset()
	{
		_documents.Clear();
		_stack.Clear();
		_anchors.Clear();
		_inStream = false;
	}

	public void Emit(ParsingEvent @event)
	{
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Expected Obj, but got Unknown
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected Obj, but got Unknown
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Expected Obj, but got Unknown
		//IL_0200: Unknown result type (might be due to invalid IL or missing references)
		//IL_0207: Expected Obj, but got Unknown
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_021f: Unknown result type (might be due to invalid IL or missing references)
		if (!(@event is StreamStart))
		{
			if (!(@event is StreamEnd))
			{
				if (!(@event is DocumentStart))
				{
					if (!(@event is DocumentEnd))
					{
						MappingStart val = (MappingStart)(object)((@event is MappingStart) ? @event : null);
						if (val == null)
						{
							if (!(@event is MappingEnd))
							{
								SequenceStart val2 = (SequenceStart)(object)((@event is SequenceStart) ? @event : null);
								if (val2 == null)
								{
									if (!(@event is SequenceEnd))
									{
										Scalar val3 = (Scalar)(object)((@event is Scalar) ? @event : null);
										if (val3 == null)
										{
											AnchorAlias val4 = (AnchorAlias)(object)((@event is AnchorAlias) ? @event : null);
											if (val4 != null)
											{
												AnchorName value = val4.Value;
												string value2 = value.Value;
												if (!_anchors.TryGetValue(value2, out YamlNode value3))
												{
													throw new InvalidOperationException("未找到锚点 '" + value2 + "' 的定义。");
												}
												_AttachToParent(value3);
											}
										}
										else
										{
											YamlScalarNode node = new YamlScalarNode(val3.Value);
											_ApplyAnchor((NodeEvent)(object)val3, (YamlNode)(object)node);
											_AttachToParent((YamlNode)(object)node);
										}
									}
									else
									{
										if (_stack.Count == 0 || _stack.Peek().Kind != ContainerKind.Sequence)
										{
											throw new InvalidOperationException("SequenceEnd 前缺少对应的 SequenceStart。");
										}
										_stack.Pop();
									}
								}
								else
								{
									YamlSequenceNode val5 = new YamlSequenceNode();
									_ApplyAnchor((NodeEvent)(object)val2, (YamlNode)(object)val5);
									_AttachToParent((YamlNode)(object)val5);
									_stack.Push(Container.Sequence(val5));
								}
							}
							else
							{
								if (_stack.Count == 0 || _stack.Peek().Kind != ContainerKind.Mapping)
								{
									throw new InvalidOperationException("MappingEnd 前缺少对应的 MappingStart。");
								}
								if (_stack.Pop().PendingKey != null)
								{
									throw new InvalidOperationException("映射以键结尾，缺少对应的值。");
								}
							}
						}
						else
						{
							YamlMappingNode val6 = new YamlMappingNode();
							_ApplyAnchor((NodeEvent)(object)val, (YamlNode)(object)val6);
							_AttachToParent((YamlNode)(object)val6);
							_stack.Push(Container.Mapping(val6));
						}
					}
					else
					{
						_RequireStream();
						if (_stack.Count == 0 || _stack.Peek().Kind != ContainerKind.Document)
						{
							throw new InvalidOperationException("DocumentEnd 前缺少对应的 DocumentStart。");
						}
						Container container = _stack.Pop();
						if (container.Node == null)
						{
							throw new InvalidOperationException("空文档：未设置根节点。");
						}
						_documents.Add(new YamlDocument(container.Node));
					}
				}
				else
				{
					_RequireStream();
					_stack.Push(Container.Document());
				}
			}
			else
			{
				_inStream = false;
				if (_stack.Count != 0)
				{
					throw new InvalidOperationException("事件结束时堆栈未清空，事件序列不平衡。");
				}
			}
		}
		else
		{
			Reset();
			_inStream = true;
		}
	}

	private void _RequireStream()
	{
		if (!_inStream)
		{
			throw new InvalidOperationException("必须在 StreamStart 与 StreamEnd 之间接收事件。");
		}
	}

	private void _ApplyAnchor(NodeEvent nodeEvent, YamlNode node)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		AnchorName anchor = nodeEvent.Anchor;
		if (!anchor.IsEmpty)
		{
			anchor = nodeEvent.Anchor;
			string value = anchor.Value;
			node.Anchor = (AnchorName)value;
			if (!_anchors.TryAdd(value, node))
			{
				throw new InvalidOperationException("锚点 '" + value + "' 被重复定义。");
			}
		}
	}

	private void _AttachToParent(YamlNode node)
	{
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		if (_stack.Count == 0)
		{
			throw new InvalidOperationException("缺少 DocumentStart：无法确定根节点所属文档。");
		}
		Container item = _stack.Peek();
		switch (item.Kind)
		{
		case ContainerKind.Document:
			if (item.Node != null)
			{
				throw new InvalidOperationException("一个文档只能包含一个根节点。");
			}
			item.Node = node;
			_stack.Pop();
			_stack.Push(item);
			break;
		case ContainerKind.Sequence:
			((YamlSequenceNode)item.Node).Add(node);
			break;
		case ContainerKind.Mapping:
			if (item.PendingKey == null)
			{
				item.PendingKey = node;
			}
			else
			{
				((YamlMappingNode)item.Node).Add(item.PendingKey, node);
				item.PendingKey = null;
			}
			_stack.Pop();
			_stack.Push(item);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}
}
