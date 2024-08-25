using UnityEngine;
using Cysharp.Threading.Tasks;

// Cartoon FX  - (c) 2015, Jean Moreno

// Drag/Drop this script on a Particle System (or an object having Particle System objects as children) to prevent a Shuriken bug
// where a system would emit at its original instantiated position before being translated, resulting in particles in-between
// the two positions.
// Possibly a threading bug from Unity (as of 3.5.4)
// Neon Raven made this better, but this is still hacky lmao

public class CFX_ShurikenThreadFix : MonoBehaviour
{
	private ParticleSystem[] _systems;
	
	private async void OnEnable()
	{
		await UniTask.DelayFrame(1);
		_systems = GetComponentsInChildren<ParticleSystem>();
		
		foreach(var ps in _systems)
		{
			ps.Stop(true);
			ps.Clear(true);
		}
		
		await UniTask.DelayFrame(1);
		foreach(var ps in _systems) ps.Play(true);
	}
}