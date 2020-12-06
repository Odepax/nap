using Microsoft.AspNetCore.Mvc;
using SampleApi.Data;

namespace SampleApi.Controllers
{
	[Route("api/cats")]
	public class CatsController : NapApiController<Cat>
	{
		public CatsController(SampleApiContext context) : base(context)
		{
		}
	}
}
