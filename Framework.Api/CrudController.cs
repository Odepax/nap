using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Nap.Framework.Api
{
	[ApiController]
	public abstract class CrudController<T> : ControllerBase where T : ApiEntity
	{
		private readonly DbContext Context;

		public CrudController(DbContext context) =>
			Context = context;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<T>>> Get() =>
			Ok(await Context.Set<T>().AsNoTracking().ToListAsync());

		[HttpGet("{id:guid}")]
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

		[HttpPut("{id:guid}")]
		public async Task<IActionResult> Set(Guid id, T entity)
		{
			entity.Id = id;

			// TODO: Change to ContainsAsync?
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

		[HttpDelete("{id:guid}")]
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
