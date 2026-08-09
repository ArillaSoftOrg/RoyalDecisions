using UnityEngine;

namespace RoyalDecisions.Composition
{
    public sealed class UnityApplicationQuitter : IApplicationQuitter
    {
        public void Quit() => UnityEngine.Application.Quit();
    }
}
