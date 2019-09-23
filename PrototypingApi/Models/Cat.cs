using Nap.Framework.Api;

namespace Nap.PrototypingApi.Models
{
	public sealed class Cat : ApiEntity
	{
		public string? Name { get; set; }
		public float? PurrPower { get; set; }
		public bool IsGrumpy { get; set; } = false;
	}
}
