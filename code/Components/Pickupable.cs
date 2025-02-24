using Sandbox;
using Sandbox.Diagnostics;

namespace VRBase;

public sealed class Pickupable : Component
{
	[RequireComponent()]
	private Rigidbody? rigidbody {get; set;}
	public List<Vrpickup> handsHolding = new List<Vrpickup>();

	public PIDController? pid;

	private Logger log = new Logger("Pickupable: ");

	protected override void OnUpdate()
	{
		if(pid.IsValid())
		{
			//if we have a PID, but no hands holding destroy our PID
			if(handsHolding.Count == 0)
			{
				pid.Destroy();
			}
			//otherwise update our target PID position to the average of all hands holding
			else
			{
				pid.targetPos = averagedPos();
				pid.targetRot = averagedRot();
			}
		}
	}

	//gets the averaged position of all hands currently holding this object, using their pickupData and position
	private Vector3 averagedPos()
	{
		Vector3 outVect = Vector3.Zero;
		foreach(Vrpickup hand in handsHolding)
		{
			PickupData? data = hand.GetHeldObject();
			
			if(data != null && data.Value.Object == this)
			{
				outVect += hand.WorldPosition - data.Value.RelativePos;
			}
			else
			{
				log.Error("Our hand contained bad data, forcing drop");
				drop(null);
			}
		}
		outVect = outVect/handsHolding.Count;
		return outVect;
	}

	//get the averaged rotation of all hands currently holding this object, using their pickupData and rotation
	private Rotation averagedRot()
	{
		if(handsHolding.Count == 1)
		{
			return handsHolding[0].WorldRotation;
		}
		return Rotation.Identity;
	}

	private PIDController createPID(Vrpickup hand)
	{
		PIDController pid = this.AddComponent<PIDController>();
		return pid;
	}

	//called whenever the player attempts to pick up an object 
	//TODO: implement some interface with the hand animation tree to control pose
	public PickupData? pickup(Vrpickup hand)
	{
		if(!pid.IsValid())
		{
			this.pid = createPID(hand);
			if(rigidbody.IsValid())
			{
				pid.Rigidbody = rigidbody;
			}
		}

		if(!handsHolding.Contains(hand))
		{
			handsHolding.Add(hand);
			//this is temporary, make it actually use an algoritim to get these positions
			return new PickupData(this, hand.WorldPosition, hand.WorldRotation);
		}
		return null;
	}

	//selected hand drops the object, all hands drop if null
	public void drop(Vrpickup? hand)
	{
		if(!hand.IsValid())
		{
			foreach(Vrpickup h in handsHolding)
			{
				h.dropHeldObject();
			}
			handsHolding.Clear();
			return;
		}
		hand.dropHeldObject();
		handsHolding.Remove(hand);
	}
}
