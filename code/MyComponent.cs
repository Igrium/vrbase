
using Sandbox.VR;

public sealed class MyComponent : Component
{
	[Property] public string StringProperty { get; set; }

	protected override void OnUpdate()
	{
		VRHand hand = new VRHand();
	}
}
