using Sandbox;

namespace VRBase;

public sealed class Pickupable : Component
{
	[RequireComponent()]
	private Rigidbody? rigidbody {get; set;}
	public List<Vrpickup> handsHolding = new List<Vrpickup>();

	public PIDController? pid;

	protected override void OnUpdate()
	{
		if(pid.IsValid())
		{
			if(handsHolding.Count == 0)
			{
				pid.Destroy();
			}
			else
			{
				pid.targetPos = averagedPos();
				pid.targetRot = averagedRot();
			}
		}
	}

	private Vector3 averagedPos()
	{
		if(handsHolding.Count == 1)
		{
			return handsHolding[0].WorldPosition;
		}

		Vector3 outVect = Vector3.Zero;
		foreach(Vrpickup hand in handsHolding)
		{
			outVect += hand.WorldPosition;
		}
		outVect = outVect/handsHolding.Count;
		return outVect;
	}

	private Rotation averagedRot()
	{
		if(handsHolding.Count == 1)
		{
			return handsHolding[0].WorldRotation;
		}
		return Rotation.Identity;
	}

	public Pickupable? pickup(Vrpickup hand)
	{
		if(!pid.IsValid())
		{
			pid = this.AddComponent<PIDController>();
			if(rigidbody.IsValid())
			{
				pid.Rigidbody = rigidbody;
			}
		}

		if(!handsHolding.Contains(hand))
		{
			handsHolding.Add(hand);
			return this;
		}
		return null;
	}

	public void drop(Vrpickup? hand)
	{
		if(!hand.IsValid())
		{
			handsHolding.Clear();
			return;
		}
		handsHolding.Remove(hand);
	}
}
