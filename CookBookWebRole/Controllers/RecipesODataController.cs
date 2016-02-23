using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.Http.OData;
using System.Web.Http.OData.Routing;
using CookBookCommon;

namespace CookBookWebRole.Controllers
{
    /*
    The WebApiConfig class may require additional changes to add a route for this controller. Merge these statements into the Register method of the WebApiConfig class as applicable. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using CookBookCommon;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Recipe>("RecipesOData");
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
    public class RecipesODataController : ODataController
    {
        private CookBookContext db = new CookBookContext();

        // GET: odata/RecipesOData
        [EnableQuery]
        public IQueryable<Recipe> GetRecipesOData()
        {
            return db.Recipes;
        }

        // GET: odata/RecipesOData(5)
        [EnableQuery]
        public SingleResult<Recipe> GetRecipe([FromODataUri] int key)
        {
            return SingleResult.Create(db.Recipes.Where(recipe => recipe.RecipeId == key));
        }

        // PUT: odata/RecipesOData(5)
        public IHttpActionResult Put([FromODataUri] int key, Delta<Recipe> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Recipe recipe = db.Recipes.Find(key);
            if (recipe == null)
            {
                return NotFound();
            }

            patch.Put(recipe);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RecipeExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(recipe);
        }

        // POST: odata/RecipesOData
        public IHttpActionResult Post(Recipe recipe)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Recipes.Add(recipe);
            db.SaveChanges();

            return Created(recipe);
        }

        // PATCH: odata/RecipesOData(5)
        [AcceptVerbs("PATCH", "MERGE")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Recipe> patch)
        {
            Validate(patch.GetEntity());

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Recipe recipe = db.Recipes.Find(key);
            if (recipe == null)
            {
                return NotFound();
            }

            patch.Patch(recipe);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RecipeExists(key))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Updated(recipe);
        }

        // DELETE: odata/RecipesOData(5)
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            Recipe recipe = db.Recipes.Find(key);
            if (recipe == null)
            {
                return NotFound();
            }

            db.Recipes.Remove(recipe);
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool RecipeExists(int key)
        {
            return db.Recipes.Count(e => e.RecipeId == key) > 0;
        }
    }
}
