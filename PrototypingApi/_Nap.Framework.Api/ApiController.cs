using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// TODO: Move to the Nap.Framework.Api project.

namespace Nap.Framework.Api
{
	[ApiController]
	public abstract class ApiController<T> : ControllerBase where T : ApiEntity
	{
		private readonly DbContext Context;

		public ApiController(DbContext context) =>
			Context = context;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<T>>> Get() =>
			Ok(await Context.Set<T>().AsNoTracking().ToListAsync());

		[HttpGet("{id}")]
		public async Task<ActionResult<T>> Get(Guid id)
		{
			T entity = await Context.Set<T>().FindAsync(id);

			return entity is null
				? NotFound()
				: Ok(entity) as ActionResult;
		}

		[HttpPost]
		public async Task<ActionResult<Guid>> New(T entity)
		{
			entity.Id = Guid.NewGuid();

			Context.Set<T>().Add(entity);

			await Context.SaveChangesAsync();

			return CreatedAtAction(nameof(Get), new { id = entity.Id.ToString() }, entity.Id.ToString());
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Set(Guid id, T entity)
		{
			entity.Id = id;

			if (await Context.Set<T>().AnyAsync(e => e.Id == id))
			{
				Context.Set<T>().Update(entity);
			}
			else
			{
				Context.Set<T>().Add(entity);
			}

			await Context.SaveChangesAsync();

			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult<T>> Remove(Guid id)
		{
			T entity = await Context.Set<T>().FindAsync(id);

			if (!(entity is null))
			{
				Context.Set<T>().Remove(entity);

				await Context.SaveChangesAsync();
			}

			return NoContent();
		}
	}
}
