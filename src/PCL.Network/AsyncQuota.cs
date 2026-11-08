using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PCL.Network;

internal sealed class AsyncQuota
{
	private sealed class QuotaWaiter(long amount, Func<long> getCapacity)
	{
		public long Amount { get; } = amount;

		public Func<long> GetCapacity { get; } = getCapacity;

		public TaskCompletionSource Tcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		public LinkedListNode<QuotaWaiter>? Node { get; set; }

		public bool Assigned { get; set; }
	}

	private readonly Lock _lock = new Lock();

	private readonly LinkedList<QuotaWaiter> _waiters = new LinkedList<QuotaWaiter>();

	private long _used;

	public async ValueTask<DownloadQuotaLease> AcquireAsync(long amount, Func<long> getCapacity, CancellationToken cancellationToken)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount, "amount");
		QuotaWaiter waiter;
		using (_lock.EnterScope())
		{
			long num = Math.Max(amount, getCapacity());
			if (_waiters.Count == 0 && _used + amount <= num)
			{
				_used += amount;
				return new DownloadQuotaLease(this, amount);
			}
			waiter = new QuotaWaiter(amount, getCapacity);
			waiter.Node = _waiters.AddLast(waiter);
		}
		try
		{
			await waiter.Tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return new DownloadQuotaLease(this, amount);
		}
		catch
		{
			using (_lock.EnterScope())
			{
				if (waiter.Node?.List != null)
				{
					_waiters.Remove(waiter.Node);
				}
				else if (waiter.Assigned)
				{
					_used = Math.Max(0L, _used - amount);
					PromoteWaitersLocked();
				}
			}
			throw;
		}
	}

	public void Release(long amount)
	{
		List<QuotaWaiter> list = new List<QuotaWaiter>();
		using (_lock.EnterScope())
		{
			_used = Math.Max(0L, _used - amount);
			PromoteWaitersLocked(list);
		}
		foreach (QuotaWaiter item in list)
		{
			item.Tcs.TrySetResult();
		}
	}

	public void NotifyCapacityChanged()
	{
		List<QuotaWaiter> list = new List<QuotaWaiter>();
		using (_lock.EnterScope())
		{
			PromoteWaitersLocked(list);
		}
		foreach (QuotaWaiter item in list)
		{
			item.Tcs.TrySetResult();
		}
	}

	private void PromoteWaitersLocked(List<QuotaWaiter>? waitersToWake = null)
	{
		LinkedListNode<QuotaWaiter> linkedListNode = _waiters.First;
		while (linkedListNode != null)
		{
			QuotaWaiter value = linkedListNode.Value;
			long num = Math.Max(value.Amount, value.GetCapacity());
			if (_used + value.Amount <= num)
			{
				_used += value.Amount;
				value.Assigned = true;
				LinkedListNode<QuotaWaiter>? next = linkedListNode.Next;
				_waiters.Remove(linkedListNode);
				if (waitersToWake != null)
				{
					waitersToWake.Add(value);
				}
				else
				{
					value.Tcs.TrySetResult();
				}
				linkedListNode = next;
				continue;
			}
			break;
		}
	}
}
