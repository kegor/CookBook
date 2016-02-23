using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using CookBookCommon;
using Microsoft.WindowsAzure.Storage.Table.DataServices;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System.Diagnostics;

namespace CookBookWebRole.Controllers
{
    public class RecipeController : Controller
    {
        private CookBookContext db = new CookBookContext();
        private CloudQueue imagesQueue;
        private static CloudBlobContainer imagesBlobContainer;

        public RecipeController()
        {
            InitializeStorage();
        }

        private void InitializeStorage()
        {
            // Open storage account using credentials from .cscfg file.
            var storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));

            // Get context object for working with blobs, and 
            // set a default retry policy appropriate for a web user interface.
            var blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            // Get a reference to the blob container.
            imagesBlobContainer = blobClient.GetContainerReference("images");

            // Get context object for working with queues, and 
            // set a default retry policy appropriate for a web user interface.
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            queueClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 3);

            // Get a reference to the queue.
            imagesQueue = queueClient.GetQueueReference("images");
        }

        // GET: Recipe
        public async Task<ActionResult> Index(int? category)
        {
            // This code executes an unbounded query; don't do this in a production app,
            // it could return too many rows for the web app to handle. For an example
            // of paging code, see:
            // http://www.asp.net/mvc/tutorials/getting-started-with-ef-using-mvc/sorting-filtering-and-paging-with-the-entity-framework-in-an-asp-net-mvc-application
            var recipesList = db.Recipes.AsQueryable();
            if (category != null)
            {
                recipesList = recipesList.Where(a => a.Category == (Category)category);
            }
            return View(await recipesList.ToListAsync());
        }

        // GET: Recipe/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Recipe recipe = await db.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return HttpNotFound();
            }
            return View(recipe);
        }

        // GET: Recipe/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Recipe/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(
            [Bind(Include = "Title,Calories,Description,Category,CookingTime")] Recipe recipe,
            HttpPostedFileBase imageFile)
        {
            CloudBlockBlob imageBlob = null;
            // A production app would implement more robust input validation.
            // For example, validate that the image file size is not too large.
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.ContentLength != 0)
                {
                    imageBlob = await UploadAndSaveBlobAsync(imageFile);
                    recipe.ImageURL = imageBlob.Uri.ToString();
                }
                recipe.PostedDate = DateTime.Now;
                db.Recipes.Add(recipe);
                await db.SaveChangesAsync();
                Trace.TraceInformation("Created RecipeId {0} in database", recipe.RecipeId);

                if (imageBlob != null)
                {
                    var queueMessage = new CloudQueueMessage(recipe.RecipeId.ToString());
                    await imagesQueue.AddMessageAsync(queueMessage);
                    Trace.TraceInformation("Created queue message for RecipeId {0}", recipe.RecipeId);
                }
                return RedirectToAction("Index");
            }

            return View(recipe);
        }

        // GET: Recipe/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Recipe recipe = await db.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return HttpNotFound();
            }
            return View(recipe);
        }

        // POST: Recipe/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(
            [Bind(Include = "RecipeId,Title,Calories,Description,ImageURL,ThumbnailURL,PostedDate,Category,CookingTime")] Recipe recipe,
            HttpPostedFileBase imageFile)
        {
            CloudBlockBlob imageBlob = null;
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.ContentLength != 0)
                {
                    // User is changing the image -- delete the existing
                    // image blobs and then upload and save a new one.
                    await DeleteAdBlobsAsync(recipe);
                    imageBlob = await UploadAndSaveBlobAsync(imageFile);
                    recipe.ImageURL = imageBlob.Uri.ToString();
                }
                db.Entry(recipe).State = EntityState.Modified;
                await db.SaveChangesAsync();
                Trace.TraceInformation("Updated RecipeId {0} in database", recipe.RecipeId);

                if (imageBlob != null)
                {
                    var queueMessage = new CloudQueueMessage(recipe.RecipeId.ToString());
                    await imagesQueue.AddMessageAsync(queueMessage);
                    Trace.TraceInformation("Created queue message for AdId {0}", recipe.RecipeId);
                }
                return RedirectToAction("Index");
            }
            return View(recipe);
        }

        // GET: Recipe/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Recipe recipe = await db.Recipes.FindAsync(id);
            if (recipe == null)
            {
                return HttpNotFound();
            }
            return View(recipe);
        }

        // POST: Recipe/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            Recipe recipe = await db.Recipes.FindAsync(id);

            await DeleteAdBlobsAsync(recipe);

            db.Recipes.Remove(recipe);
            await db.SaveChangesAsync();
            Trace.TraceInformation("Deleted ad {0}", recipe.RecipeId);
            return RedirectToAction("Index");
        }

        private async Task<CloudBlockBlob> UploadAndSaveBlobAsync(HttpPostedFileBase imageFile)
        {
            Trace.TraceInformation("Uploading image file {0}", imageFile.FileName);

            string blobName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
            // Retrieve reference to a blob. 
            CloudBlockBlob imageBlob = imagesBlobContainer.GetBlockBlobReference(blobName);
            // Create the blob by uploading a local file.
            using (var fileStream = imageFile.InputStream)
            {
                await imageBlob.UploadFromStreamAsync(fileStream);
            }

            Trace.TraceInformation("Uploaded image file to {0}", imageBlob.Uri.ToString());

            return imageBlob;
        }

        private async Task DeleteAdBlobsAsync(Recipe recipe)
        {
            if (!string.IsNullOrWhiteSpace(recipe.ImageURL))
            {
                Uri blobUri = new Uri(recipe.ImageURL);
                await DeleteAdBlobAsync(blobUri);
            }
            if (!string.IsNullOrWhiteSpace(recipe.ThumbnailURL))
            {
                Uri blobUri = new Uri(recipe.ThumbnailURL);
                await DeleteAdBlobAsync(blobUri);
            }
        }

        private static async Task DeleteAdBlobAsync(Uri blobUri)
        {
            string blobName = blobUri.Segments[blobUri.Segments.Length - 1];
            Trace.TraceInformation("Deleting image blob {0}", blobName);
            CloudBlockBlob blobToDelete = imagesBlobContainer.GetBlockBlobReference(blobName);
            await blobToDelete.DeleteAsync();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
