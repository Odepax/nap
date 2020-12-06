using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SampleApi.Data;

namespace SampleApi.Controllers
{
	[ApiController]
	public class NapApiController<T> : ControllerBase where T : ApiEntity
	{
		private readonly DbContext Context;

		public NapApiController(DbContext context) =>
			Context = context;

		[HttpGet]
		public async Task<ActionResult<IEnumerable<T>>> Get() =>
			Ok(await Context.Set<T>().ToListAsync());

		[HttpGet("{id}")]
		public async Task<ActionResult<T>> Get(Guid id)
		{
			T entity = await Context.Set<T>().FindAsync(id);

			return entity is null
				? NotFound()
				: Ok(entity) as ActionResult<T>;
		}

		[HttpPost]
		public async Task<ActionResult<Guid>> New(T entity)
		{
			entity.Id = Guid.NewGuid();

			Context.Set<T>().Add(entity);

			await Context.SaveChangesAsync();

			return CreatedAtAction(nameof(Get), new { id = entity.Id.ToString("N") }, entity.Id.ToString("N"));
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Set(Guid id, T entity)
		{
			entity.Id = id;

			Context.Set<T>().Add(entity);

			try
			{
				await Context.SaveChangesAsync();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!await Context.Set<T>().AnyAsync(entity => entity.Id == id))
				{
					return NotFound();
				}
				else
				{
					throw;
				}
			}

			return NoContent();
		}

		[HttpDelete("{id}")]
		public async Task<ActionResult<T>> Delete(Guid id)
		{
			T entity = await Context.Set<T>().FindAsync(id);

			Context.Set<T>().Remove(entity);

			await Context.SaveChangesAsync();

			return NoContent();
		}
	}
}