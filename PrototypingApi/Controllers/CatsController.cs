using Microsoft.AspNetCore.Mvc;
using Nap.Framework.Api;
using Nap.PrototypingApi.Data;
using Nap.PrototypingApi.Models;

namespace Nap.PrototypingApi.Controllers
{
	[Route("api/cats")]
	public sealed class CatsController : CrudController<Cat>
	{
		public CatsController(PrototypingDbContext context) : base(context)
		{
		}
	}
}
