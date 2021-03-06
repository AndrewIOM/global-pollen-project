﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using GlobalPollenProject.WebUI.Models;
using GlobalPollenProject.WebUI.Services;
using GlobalPollenProject.WebUI.Services.Abstract;
using GlobalPollenProject.WebUI.ViewModels.Reference;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GlobalPollenProject.WebUI.Controllers
{
    public class ReferenceController : Controller
    {
        public UserManager<AppUser> UserManager { get; set; }
        private IFileStoreService _fileService;
        private IReferenceService _refService;
        private IUserService _userService;
        private readonly IEmailSender _emailSender;
        private readonly ITaxonomyBackbone _backbone;
        private ITaxonomyService _taxonomyService;
        public ReferenceController(
            IFileStoreService fileService,
            IReferenceService refService,
            IUserService userService,
            IEmailSender emailSender,
            ITaxonomyBackbone backbone,
            ITaxonomyService taxonomyService,
            IServiceProvider services)
        {
            _fileService = fileService;
            _refService = refService;
            _userService = userService;
            _emailSender = emailSender;
            _backbone = backbone;
            _taxonomyService = taxonomyService;
            UserManager = services.GetRequiredService<UserManager<AppUser>>();
        }

        public IActionResult Index()
        {
            var model = _refService.ListCollections();
            return View(model);
        }

        public IActionResult Collection(int id)
        {
            var model = _refService.GetCollectionById(id);
            return View(model);
        }

        public IActionResult Grain(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var model = _refService.GetGrainById(id);
            return View(model);
        }

        [HttpGet]
        public IActionResult RequestAccess()
        {
            var model = new RequestAccessViewModel();
            var user = _userService.GetById(UserManager.GetUserId(User));
            if (user != null)
            {
                model.HasRequestedAccess = user.RequestedDigitisationRights;
            }
            return View(model);
        }

        public IActionResult Help()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public IActionResult RequestAccess(RequestAccessViewModel result)
        {
            if (!ModelState.IsValid) return BadRequest();
            var user = _userService.GetById(UserManager.GetUserId(User));
            user.RequestedDigitisationRights = true;
            _userService.Update(user);

            var adminEmail = "admin@globalpollenproject.org"; // TODO remove hardcoding
            _emailSender.SendEmailAsync(adminEmail, "Request for digitisation rights",
                user.FullName() + " has requested digitisation rights. They write: " + result.Comments).Wait();
            return RedirectToAction("RequestAccess");
        }

        [HttpGet]
        [Authorize(Roles = "Digitise")]
        public IActionResult AddCollection()
        {
            var model = new ReferenceCollection();
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Digitise")]
        public IActionResult AddCollection(ReferenceCollection result)
        {
            if (!ModelState.IsValid)
            {
                return View(result);
            }

            result.User = _userService.GetById(UserManager.GetUserId(User));
            var saved = _refService.AddCollection(result);
            return RedirectToAction("Collection", new { id = saved.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Digitise")]
        public IActionResult EditCollection(int id)
        {
            var collection = _refService.GetCollectionById(id);
            if (collection == null)
            {
                return BadRequest();
            }
            if (collection.User.Id != UserManager.GetUserId(User))
            {
                return Unauthorized();
            }

            return View("AddCollection", collection);
        }

        [HttpPost]
        [Authorize(Roles = "Digitise")]
        public IActionResult EditCollection(ReferenceCollection model)
        {
            var collection = _refService.GetCollectionById(model.Id);
            if (collection == null)
            {
                return BadRequest();
            }
            if (collection.User.Id != UserManager.GetUserId(User))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return View("AddCollection", model);
            }

            collection.CountryCode = model.CountryCode;
            collection.Description = model.Description;
            collection.FocusRegion = model.FocusRegion;
            collection.Institution = model.Institution;
            collection.OwnedBy = model.OwnedBy;
            collection.Name = model.Name;
            collection.WebAddress = model.WebAddress;
            collection.ContactEmail = model.ContactEmail;

            _refService.UpdateCollection(collection);
            return RedirectToAction("Collection", new { id = model.Id });
        }

        [HttpGet]
        [Authorize(Roles = "Digitise")]
        public IActionResult AddGrain(int id)
        {
            if (id == 0)
            {
                return BadRequest();
            }
            var model = _refService.GetCollectionById(id);
            if (model.User.Id != UserManager.GetUserId(User)) return BadRequest();
            return View(new ReferenceGrainViewModel()
            {
                CollectionId = model.Id
            });
        }

        [HttpPost]
        [Authorize(Roles = "Digitise")]
        public async Task<IActionResult> AddGrain(ReferenceGrainViewModel result)
        {
            var collection = _refService.GetCollectionById(result.CollectionId.Value);
            if (collection == null)
            {
                ModelState.AddModelError("CollectionId", "The collection specified does not exist.");
            } else
            {
                if (collection.User.Id != UserManager.GetUserId(User))
                {
                    ModelState.AddModelError("CollectionId", "You can only add grains to collections you own.");
                }
            }

            if (!_backbone.IsValidTaxon(result.Rank, result.Family, result.Genus, result.Species))
            {
                ModelState.AddModelError("TaxonomicBackbone", "The taxon specified was not matched by our taxonomic backbone. Check your spellings and try again");
            }

            foreach (var image in result.Images)
            {
                if (!string.IsNullOrEmpty(image)) if (!IsBase64String(image)) ModelState.AddModelError("Images", "There was an encoding error when uploading your image. Please try a different image, or report the problem.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var standardImages = await _fileService.UploadBase64Image(result.Images);
            var taxon = _taxonomyService.CreateOrUpdateTaxonomy(result.Family, result.Genus, result.Species);
            var toSave = new ReferenceGrain()
            {
                Collection = collection,
                Taxon = taxon,
                SubmittedBy = _userService.GetById(UserManager.GetUserId(User)),
                TimeAdded = DateTime.Now,
                MaxSizeNanoMetres = result.MaxGrainSize.Value,
                Images = new List<GrainImage>()
            };

            foreach (var file in standardImages)
            {
                toSave.Images.Add(new GrainImage()
                {
                    FileName = file.Url,
                    FileNameThumbnail = file.ThumbnailUrl,
                    IsFocusImage = false
                });
            }

            foreach (var image in result.FocusImages)
            {
                var low = await _fileService.UploadBase64Image(image.FocusLowUrl);
                var medLow = await _fileService.UploadBase64Image(image.FocusMedLowUrl);
                var med = await _fileService.UploadBase64Image(image.FocusMedUrl);
                var medHigh = await _fileService.UploadBase64Image(image.FocusMedHighUrl);
                var high = await _fileService.UploadBase64Image(image.FocusHighUrl);
                toSave.Images.Add(new GrainImage()
                {
                    FileName = med.Url,
                    FileNameThumbnail = med.ThumbnailUrl,
                    IsFocusImage = true,
                    FocusLowUrl = low.Url,
                    FocusMedLowUrl = medLow.Url,
                    FocusMedUrl = med.Url,
                    FocusMedHighUrl = medHigh.Url,
                    FocusHighUrl = high.Url
                });
            }

            var saved = _refService.AddGrain(toSave);
            return Ok();
        }

        private bool IsBase64String(string s)
        {
            try
            {
                byte[] data = Convert.FromBase64String(s);
                return (s.Replace(" ", "").Length % 4 == 0);
            }
            catch
            {
                return false;
            }
        }

        [Authorize(Roles = "Digitise")]
        public IActionResult DeleteGrain(int id)
        {
            var grain = _refService.GetGrainById(id);
            if (grain == null) return BadRequest();
            if (User.Identity.Name != grain.Collection.User.UserName) return BadRequest();
            _refService.DeleteGrain(id);
            return RedirectToAction("Collection", new { id = grain.Collection.Id });
        }

        private string GetName(Taxonomy rank, ReferenceGrain grain)
        {
            string species = null;
            string genus = null;
            string family = null;
            if (grain.Taxon != null)
            {
                if (grain.Taxon.Rank == Taxonomy.Species)
                {
                    species = grain.Taxon.LatinName;
                    genus = grain.Taxon.ParentTaxa.LatinName;
                    family = grain.Taxon.ParentTaxa.ParentTaxa.LatinName;
                }
                else if (grain.Taxon.Rank == Taxonomy.Genus)
                {
                    genus = grain.Taxon.LatinName;
                    family = grain.Taxon.ParentTaxa.LatinName;
                }
                else
                {
                    family = grain.Taxon.LatinName;
                }
            }
            if (rank == Taxonomy.Species) return species;
            if (rank == Taxonomy.Genus) return genus;
            return family;
        }

    }
}
