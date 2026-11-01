using PetDesk.Core.Models;

namespace PetDesk.Core.Animation;

public sealed class PetAnimationController(PetManifest manifest)
{
    public string Resolve(string requested) => manifest.Animations.ContainsKey(requested) ? requested : "idle";
    public AnimationDefinition Definition(string requested) => manifest.Animations[Resolve(requested)];
}
