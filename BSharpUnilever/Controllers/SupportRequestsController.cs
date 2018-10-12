﻿using AutoMapper;
using BSharpUnilever.Controllers.Util;
using BSharpUnilever.Controllers.ViewModels;
using BSharpUnilever.Data;
using BSharpUnilever.Data.Entities;
using BSharpUnilever.Services;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BSharpUnilever.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // TODO: Make sure the user is active too
    public class SupportRequestsController : ControllerBase
    {
        private const int DEFAULT_PAGE_SIZE = 50;
        private const int MAX_PAGE_SIZE = 5000;

        private readonly BSharpContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UsersController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public SupportRequestsController(BSharpContext context, IMapper mapper, ILogger<UsersController> logger,
            UserManager<User> userManager, IEmailSender emailSender)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupportRequestVM>>> GetAll(int top = DEFAULT_PAGE_SIZE,
            int skip = 0, string orderby = nameof(SupportRequestVM.SerialNumber), bool desc = true, string search = null)
        {
            try
            {
                // First get a readonly query
                IQueryable<SupportRequest> query = _context.SupportRequests.AsNoTracking();

                // Apply row level security: if you are a KAE you only see your records
                query = await ApplyRowLevelSecurityAsync(query);

                // Apply the searching
                if (!string.IsNullOrWhiteSpace(search))
                {
                    if (int.TryParse(search, out int serial))
                    {
                        // IF the user input is an int number => search by serial number
                        query = query.Where(e => e.SerialNumber == serial);
                    }
                    else if (DateTime.TryParse(search, out DateTime date))
                    {
                        // IF the user input is a valid date => search by date
                        query = query.Where(e => e.Date == date);
                    }
                    else
                    {
                        // ELSE search KAE and store names and comments
                        query = query.Where(e =>
                        e.AccountExecutive.FullName.Contains(search) ||
                        e.Store.Name.Contains(search) ||
                        e.Comment.Contains(search));
                    }
                }

                // Before ordering or paging, retrieve the total count
                int totalCount = query.Count();

                // Apply the ordering (If orderby does not exist the system just ignores it)
                if (!string.IsNullOrWhiteSpace(orderby) && typeof(SupportRequestVM).HasProperty(orderby))
                {
                    string key = orderby; // TODO get the mapped property from the entity
                    query = query.OrderBy(key, isDescending: desc);
                }

                // Apply the paging (Protect against DOS attacks by enforcing a maximum page size)
                top = Math.Min(top, MAX_PAGE_SIZE);
                query = query.Skip(skip).Take(top);

                // Load the data, transform it and wrap it in some metadata
                var memoryList = await query
                    .Include(e => e.AccountExecutive)
                    .Include(e => e.Manager)
                    .Include(e => e.Store)
                    .ToListAsync();

                var resultData = _mapper.Map<List<SupportRequest>, List<SupportRequestVM>>(memoryList);
                var result = new ListResultVM<SupportRequestVM>
                {
                    Skip = skip,
                    Top = resultData.Count,
                    OrderBy = orderby,
                    Desc = desc,
                    TotalCount = totalCount,
                    Data = resultData
                };

                // Finally return the result
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupportRequestVM>> Get(int id)
        {
            try
            {
                // Retrieve the record and if missing return a 404
                var result = await InternalGetAsync(id);
                if (result == null)
                {
                    return NotFound($"Could not find a record with id='{id}'");
                }

                // All is good
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("generateddocuments/{id}")]
        public async Task<ActionResult> GetGeneratedDocument(int id)
        {
            // Get the document
            var creditNote = await _context.GeneratedDocuments
                .Include(e => e.SupportRequest.LineItems)
                .FirstOrDefaultAsync(e => e.Id == id);

            // Make sure it exists and that it isn't void
            if (creditNote == null)
            {
                return NotFound($"Could not find the document with Id={id}");
            }
            else if (creditNote.State < 0) // 0 == Void
            {
                return BadRequest($"This credit note has been voided");
            }
            else
            {
                using (MemoryStream resultStream = new MemoryStream())
                {
                    PdfWriter writer = new PdfWriter(resultStream);
                    PdfDocument pdf = new PdfDocument(writer);
                    Document document = new Document(pdf);
                    document.Add(new Paragraph($"Unilever Document Number: {creditNote.SupportRequest.SerialNumber:D5}"));
                    document.Add(new Paragraph($"Credit Note Number: {creditNote.SerialNumber:D2}"));
                    document.Add(new Paragraph($"Store Name: {creditNote.SupportRequest.Store.Name}"));
                    document.Add(new Paragraph($"Date: {creditNote.Date}"));
                    document.Add(new Paragraph($"This credit note is to confirm a credit amount of {creditNote.SupportRequest.LineItems.Sum(e => e.UsedValue):N2} AED")); // AED is hardcoded throughout the app
                    document.Add(new Paragraph($"Note: This file can be made more pretty"));
                    document.Close();

                    return File(resultStream.ToArray(), "application/pdf", $"CrNote_SR{creditNote.SupportRequest.SerialNumber:D5}.pdf");
                }
            }
        }

        [HttpPost]
        public async Task<ActionResult<SupportRequestVM>> Post(SupportRequestVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("There is something wrong with the request payload"); // TODO: Return friendlier validation errors
            }

            try
            {
                var user = await GetCurrentUserAsync();

                // Validation: use a simple list of strings to represent the errors
                // In a larger app, we use a more sophisticated setup
                var errors = new List<string>();
                await ValidateAsync(model, user, errors);

                // If the validation returned errors, return a 400
                if (errors.Any())
                    return BadRequest("The following validation errors were found:\n - " + string.Join("\n - ", errors));

                if (model.Id == 0) // Insert logic
                {
                    // Inserting logic
                    await InsertingAsync(model, user);

                    // Prepare the new record
                    SupportRequest newRecord = _mapper.Map<SupportRequestVM, SupportRequest>(model);
                    List<SupportRequestLineItem> lineItems = _mapper
                        .Map<List<SupportRequestLineItemVM>, List<SupportRequestLineItem>>(model.LineItems);
                    newRecord.LineItems = lineItems;

                    // Save it to the database, this automatically saves the line items too
                    _context.SupportRequests.Add(newRecord);
                    await _context.SaveChangesAsync();

                    // Map and return the newly created record in the same format as a GET request
                    var resultModel = await InternalGetAsync(newRecord.Id);
                    var resourceUrl = Url.Action(nameof(Get), nameof(SupportRequestsController)); // TODO: Fix this bug

                    return Created(resourceUrl, resultModel);
                }
                else // Update logic
                {
                    // Retrieve the record from the DB
                    IQueryable<SupportRequest> secureQuery = await ApplyRowLevelSecurityAsync(_context.SupportRequests, user);
                    SupportRequest dbRecord = await Includes(secureQuery).FirstOrDefaultAsync(e => e.Id == model.Id);
                    if (dbRecord == null)
                    {
                        return NotFound($"Could not find a record with id='{model.Id}'");
                    }

                    var originalModel = _mapper.Map<SupportRequest, SupportRequestVM>(dbRecord);

                    // updating logic
                    await UpdatingAsync(model, originalModel, user);

                    // Update the header
                    _mapper.Map(model, dbRecord);

                    // Synchronize the line items
                    HashSet<int> hashedModelIds = new HashSet<int>(model.LineItems.Select(e => e.Id));
                    foreach (var dbLineItem in dbRecord.LineItems)
                    {
                        if (!hashedModelIds.Contains(dbLineItem.Id)) // Deleted line
                        {
                            _context.SupportRequestLineItems.Remove(dbLineItem);
                        }
                    }

                    Dictionary<int, SupportRequestLineItem> dbLookup = dbRecord.LineItems.ToDictionary(e => e.Id);
                    foreach (var modelLine in model.LineItems)
                    {
                        if (modelLine.Id == 0) // New line
                        {
                            // Map it and add it
                            var newDbLine = _mapper.Map<SupportRequestLineItem>(modelLine);
                            dbRecord.LineItems.Add(newDbLine);
                        }
                        else // Updated line
                        {
                            if (!dbLookup.ContainsKey(modelLine.Id))
                            {
                                // This is only possible when 2 users run into a concurrency issue
                                return BadRequest($"Line item with Id {modelLine.Id} was not found");
                            }

                            // Update existing item
                            var existingDbLine = dbLookup[modelLine.Id];
                            _mapper.Map(modelLine, existingDbLine);
                        }
                    }


                    // Save the changes
                    await _context.SaveChangesAsync();



                    // Finally return the same result you would get with a GET request
                    var resultModel = await InternalGetAsync(model.Id);
                    return Ok(resultModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        // Either returns error messages or simply modifies the model to adhere to validation
        private Task ValidateAsync(SupportRequestVM model, User currentUser, List<string> errors)
        {
            // Never trust the API caller, only trust your DB
            var test = _mapper.Map<UserVM>(null); // TODO: remove
            model.AccountExecutive = _mapper.Map<UserVM>(_context.Users.FirstOrDefault(e => e.Id == model.AccountExecutive.Id));
            model.Manager = _mapper.Map<UserVM>(_context.Users.FirstOrDefault(e => e.Id == model.Manager.Id));
            model.Store = _mapper.Map<StoreVM>(_context.Stores.FirstOrDefault(e => e.Id == model.Store.Id));

            if (model.AccountExecutive == null)
            {
                errors.Add("The account executive field is required");
            }
            else if (model.AccountExecutive.Role != Roles.KAE)
            {
                if (currentUser.Role != Roles.Administrator)
                {
                    errors.Add("The selected account executive must have a role of KAE");
                }
            }

            if (model.Manager == null)
            {
                errors.Add("The manager field is required");
            }
            else if (model.Manager.Role != Roles.Manager)
            {
                if (currentUser.Role != Roles.Administrator)
                {
                    errors.Add("The selected manager must have a role of manager");
                }
            }

            if (model.Store == null)
            {
                errors.Add("The store field is required");
            }
            else if (!model.Store.IsActive)
            {
                errors.Add("The selected store must be active");
            }

            // Never trust the API caller, only trust your DB
            foreach (var modelLine in model.LineItems)
            {
                if (modelLine.Product != null)
                {
                    // This has a terrible performance, but it's acceptable here as the line items will never exceed 5
                    modelLine.Product = _mapper.Map<ProductVM>(_context.Products.FirstOrDefault(e => e.Id == modelLine.Product.Id));
                }
            }

            if (model.Reason == Reasons.PriceReduction)
            {
                // At least one item is mandatory
                if (!model.LineItems.Any())
                {
                    errors.Add("At least one request line is required");
                }

                foreach (var modelLine in model.LineItems)
                {
                    modelLine.RequestedValue = modelLine.RequestedSupport * modelLine.Quantity;
                    modelLine.ApprovedValue = modelLine.ApprovedSupport * modelLine.Quantity;
                    modelLine.UsedValue = modelLine.UsedSupport * modelLine.Quantity;
                }
            }
            else // Any other reason
            {
                // In this case, a singleton line will store the values directly
                if (model.LineItems.Count != 1)
                {
                    // A good client app will prevent this error from happening
                    errors.Add("The support values are required");
                }
                else
                {
                    var modelLine = model.LineItems.Single();
                    modelLine.Product = null;
                    modelLine.Quantity = 0;
                    modelLine.RequestedSupport = 0;
                    modelLine.ApprovedSupport = 0;
                    modelLine.UsedSupport = 0;
                }
            }

            // KAE cannot exceed the approved amount in the request unless it's from balance
            if (model.Reason != Reasons.FromBalance)
            {
                var violations = model.LineItems.Where(e => e.UsedValue > e.ApprovedValue);
                if (violations.Count() > 0)
                {
                    var violation = violations.First();
                    errors.Add($"The used support {violation.UsedValue:N2} cannot be more than the approved support {violation.ApprovedValue:N2}");
                }
            }

            return Task.CompletedTask;
        }

        private Task InsertingAsync(SupportRequestVM model, User currentUser)
        {
            // Auto-set the serial number
            int maxSerial = _context.SupportRequests.Max(e => (int?)e.SerialNumber) ?? 0;
            model.SerialNumber = maxSerial + 1;

            // Ensure that all requests are inserted in state draft
            model.State = SupportRequestStates.Draft;

            // Initialize readonly properties and audit info
            model.Date = DateTime.Today;
            model.CreatedBy = currentUser.UserName;
            model.ModifiedBy = currentUser.UserName;
            var now = DateTimeOffset.Now;
            model.Created = now;
            model.Modified = now;

            // Ensure that a KAE cannot request on someone else's behalf
            if (currentUser.Role == Roles.KAE)
            {
                model.AccountExecutive = _mapper.Map<UserVM>(currentUser);
            }

            return Task.CompletedTask;
        }

        // Returns non transactional actions (like email sending) so that they are safely executed by the controller near the very end
        private Task<List<Func<Task>>> UpdatingAsync(SupportRequestVM newModel, SupportRequestVM oldModel, User currentUser)
        {
            // Update audit info
            newModel.ModifiedBy = currentUser.UserName;
            newModel.Modified = DateTimeOffset.Now;

            // One way of handling readonly properties
            newModel.CreatedBy = oldModel.CreatedBy;
            newModel.Created = oldModel.Created;
            newModel.SerialNumber = oldModel.SerialNumber;
            newModel.Date = oldModel.Date;


            // The list of non-transactional actions to be executed at the end
            List<Func<Task>> deferredActions = new List<Func<Task>>();

            // Get the original state and the new state 
            string originalState = oldModel.State;
            string newState = newModel.State;

            #region Enforcing read-only values

            // Enforce rules regarding which fields are editable in which state
            // A well written front end client will prevent these errors from occurring in most cases
            var permissibleStates = new List<string> { SupportRequestStates.Draft };
            var oldLines = oldModel.LineItems.ToDictionary(e => e.Id);
            if (!permissibleStates.Contains(originalState) && !permissibleStates.Contains(newState))
            {
                // Cannot add or remove lines
                if (!oldLines.Keys.ToHashSet().SetEquals(newModel.LineItems.Select(e => e.Id)))
                {
                    throw new InvalidOperationException($"Lines cannot be added or removed after state {SupportRequestStates.Draft}");
                }

                // Cannot change requested support or the products
                foreach (var newLine in newModel.LineItems)
                {
                    var matchingOldLine = oldLines[newLine.Id];
                    if (matchingOldLine.Quantity != newLine.Quantity)
                    {
                        throw new InvalidOperationException($"Quantity cannot be modified after state {SupportRequestStates.Draft}");
                    }

                    if (matchingOldLine.RequestedSupport != newLine.RequestedSupport)
                    {
                        throw new InvalidOperationException($"Requested support cannot be modified after state {SupportRequestStates.Draft}");
                    }

                    if (matchingOldLine.RequestedValue != newLine.RequestedValue)
                    {
                        throw new InvalidOperationException($"Requested value cannot be modified after state {SupportRequestStates.Draft}");
                    }

                    if (matchingOldLine.Product != null && newLine.Product != null && matchingOldLine.Product.Id != newLine.Product.Id)
                    {
                        throw new InvalidOperationException($"The product cannot be modified after state {SupportRequestStates.Draft}");
                    }
                }

                // Cannot change submitted values
                if (newModel.Reason != oldModel.Reason)
                {
                    throw new InvalidOperationException($"The reason cannot be modified after state {SupportRequestStates.Draft}");
                }

                if (newModel.Store.Id != oldModel.Store.Id)
                {
                    throw new InvalidOperationException($"The store cannot be modified after state {SupportRequestStates.Draft}");
                }

                if (newModel.AccountExecutive.Id != oldModel.AccountExecutive.Id)
                {
                    throw new InvalidOperationException($"The key account executive cannot be modified after state {SupportRequestStates.Draft}");
                }

                if (newModel.Manager.Id != oldModel.Manager.Id)
                {
                    throw new InvalidOperationException($"The manager cannot be modified after state {SupportRequestStates.Draft}");
                }
            }

            permissibleStates.Add(SupportRequestStates.Submitted);
            if (!permissibleStates.Contains(originalState) && !permissibleStates.Contains(newState))
            {
                if (newModel.Comment != oldModel.Comment)
                {
                    throw new InvalidOperationException($"The comment cannot be modified after state {SupportRequestStates.Submitted}");
                }

                foreach (var newLine in newModel.LineItems)
                {
                    var matchingOldLine = oldLines[newLine.Id];
                    if (matchingOldLine.RequestedSupport != newLine.RequestedSupport)
                    {
                        throw new InvalidOperationException($"Approved support cannot be modified after state {SupportRequestStates.Submitted}");
                    }

                    if (matchingOldLine.RequestedValue != newLine.RequestedValue)
                    {
                        throw new InvalidOperationException($"Approved value cannot be modified after state {SupportRequestStates.Submitted}");
                    }
                }
            }

            permissibleStates.Add(SupportRequestStates.Approved);
            if (!permissibleStates.Contains(originalState) && !permissibleStates.Contains(newState))
            {
                foreach (var newLine in newModel.LineItems)
                {
                    var matchingOldLine = oldLines[newLine.Id];
                    if (matchingOldLine.UsedSupport != newLine.UsedSupport)
                    {
                        throw new InvalidOperationException($"Used support cannot be modified after state {SupportRequestStates.Approved}");
                    }

                    if (matchingOldLine.RequestedValue != newLine.RequestedValue)
                    {
                        throw new InvalidOperationException($"Used value cannot be modified after state {SupportRequestStates.Approved}");
                    }
                }
            }

            #endregion

            #region State update logic

            // State change logic
            if (originalState != newState) // State change logic below
            {
                if (originalState == SupportRequestStates.Draft && newState == SupportRequestStates.Submitted)
                {
                    // (1) Check roles
                    CheckRoles(currentUser, Roles.KAE);

                    // (2) Set default values
                    if (currentUser.Role != Roles.Administrator)
                    {
                        newModel.AccountExecutive = _mapper.Map<UserVM>(currentUser);
                    }

                    foreach (var line in newModel.LineItems)
                    {
                        // Copy the values from requested to approved by default
                        line.ApprovedSupport = line.RequestedSupport;
                        line.ApprovedValue = line.RequestedValue;
                    }

                    // (3) Email notofication
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.Manager.Email,
                        subject: $"{currentUser.FullName} is requesting support",
                        message: $"{currentUser.FullName} has submitted a new support request");
                }
                else if (originalState == SupportRequestStates.Submitted && newState == SupportRequestStates.Draft)
                {
                    // (1) Check that the manager (or the admin) returned it
                    CheckUser(currentUser, newModel.Manager);

                    // (3) Email notification
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.AccountExecutive.Email,
                        subject: $"{currentUser.FullName} has returned your support request",
                        message: $"{currentUser.FullName} has returned the support request you submitted");
                }

                else if (originalState == SupportRequestStates.Draft && newState == SupportRequestStates.Canceled)
                {
                    // Nothing
                }
                else if (originalState == SupportRequestStates.Canceled && newState == SupportRequestStates.Draft)
                {
                    // Nothing
                }

                else if (originalState == SupportRequestStates.Submitted && newState == SupportRequestStates.Approved)
                {
                    // (1) Check that the manager (or the admin) approved it
                    CheckUser(currentUser, newModel.Manager);

                    // (2) Unless the user is an admin, auto set the manager property to the current user
                    if (currentUser.Role != Roles.Administrator)
                    {
                        newModel.Manager = _mapper.Map<UserVM>(currentUser);
                    }

                    foreach (var line in newModel.LineItems)
                    {
                        // Copy the values from approved to used by default
                        line.UsedSupport = line.ApprovedSupport;
                        line.UsedValue = line.ApprovedValue;
                    }

                    // (3) Email notification
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.AccountExecutive.Email,
                        subject: $"{currentUser.FullName} has approved your request",
                        message: $"{currentUser.FullName} has approved your request");
                }
                else if (originalState == SupportRequestStates.Approved && newState == SupportRequestStates.Submitted)
                {
                    CheckUser(currentUser, newModel.AccountExecutive);

                    // Email notification
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.Manager.Email,
                        subject: $"{currentUser.FullName} has returned the support request",
                        message: $"{currentUser.FullName} has returned the support request that you approved");
                }

                else if (originalState == SupportRequestStates.Submitted && newState == SupportRequestStates.Rejected)
                {
                    // (1) Check that the manager (or the admin) approved it
                    CheckUser(currentUser, newModel.Manager);

                    // Email notification
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.AccountExecutive.Email,
                        subject: $"{currentUser.FullName} has rejected your request",
                        message: $"{currentUser.FullName} has rejected your request");
                }
                else if (originalState == SupportRequestStates.Rejected && newState == SupportRequestStates.Submitted)
                {
                    CheckUser(currentUser, newModel.Manager);
                }

                else if (originalState == SupportRequestStates.Approved && newState == SupportRequestStates.Posted)
                {
                    CheckUser(currentUser, newModel.AccountExecutive);

                    // Generate a new credit note
                    int maxSerial = oldModel.GeneratedDocuments.Max(e => (int?)e.SerialNumber) ?? 0;
                    _context.GeneratedDocuments.Add(new GeneratedDocument
                    {
                        SerialNumber = maxSerial + 1,
                        SupportRequestId = newModel.Id,
                        Date = DateTime.Today, // This may result in minor time zone issues, since the users aren't in UTC zone
                    });
                }
                else if (originalState == SupportRequestStates.Posted && newState == SupportRequestStates.Approved)
                {
                    CheckUser(currentUser, newModel.AccountExecutive);

                    // Void existing credit notes
                    var existingDocuments = _context.GeneratedDocuments.Where(e => e.SupportRequestId == newModel.Id).ToList();
                    existingDocuments.ForEach(e => e.State = -1);
                }


                else if (originalState == SupportRequestStates.Draft && newState == SupportRequestStates.Posted)
                {
                    CheckRoles(currentUser, Roles.KAE);

                    if (newModel.Reason != Reasons.FromBalance)
                    {
                        throw new InvalidOperationException(
                            "To post the document without approval the support reason must be specified as 'From Balance'");
                    }
                    else
                    {
                        // Make sure has sufficient balance
                        var balance = CurrentAvailableBalance(currentUser);
                        if (newModel.LineItems.Sum(e => e.UsedValue) > balance)
                            throw new InvalidOperationException("");
                    }

                    // Generate a new credit note
                    int maxSerial = oldModel.GeneratedDocuments.Max(e => (int?)e.SerialNumber) ?? 0;
                    _context.GeneratedDocuments.Add(new GeneratedDocument
                    {
                        SerialNumber = maxSerial + 1,
                        SupportRequestId = newModel.Id,
                        Date = DateTime.Today, // This may result in minor time zone issues, since the users aren't in UTC zone
                    });
                }
                else if (originalState == SupportRequestStates.Posted && newState == SupportRequestStates.Draft)
                {
                    CheckUser(currentUser, newModel.AccountExecutive);

                    // Void existing credit notes
                    var existingDocuments = _context.GeneratedDocuments.Where(e => e.SupportRequestId == newModel.Id).ToList();
                    existingDocuments.ForEach(e => e.State = -1);
                }

                else if (originalState == SupportRequestStates.Draft && newState == SupportRequestStates.Approved)
                {
                    // (1) Check Roles
                    CheckRoles(currentUser, Roles.Manager);

                    // (2) Set default values
                    foreach (var line in newModel.LineItems)
                    {
                        // Copy the values from requested to approved by default
                        line.RequestedSupport = 0;
                        line.RequestedValue = 0;
                        line.UsedSupport = line.ApprovedSupport;
                        line.UsedValue = line.ApprovedValue;
                    }

                    // (3) Email notification
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.AccountExecutive.Email,
                        subject: $"{currentUser.FullName} has approved a support amount for you",
                        message: $"{currentUser.FullName} has approved a support amount for you");
                }
                else if (originalState == SupportRequestStates.Approved && newState == SupportRequestStates.Draft)
                {
                    CheckUser(currentUser, newModel.AccountExecutive);

                    // (3) Email notification
                    PushSendEmail(deferredActions,
                        requestId: newModel.Id,
                        recipientEmail: newModel.AccountExecutive.Email,
                        subject: $"{currentUser.FullName} has returned your support amount",
                        message: $"{currentUser.FullName} has returned the support amount");
                }
                else
                {
                    throw new InvalidOperationException("This state change is not allowed");
                }
            }

            #endregion

            return Task.FromResult(deferredActions);
        }

        private decimal CurrentAvailableBalance(User user)
        {
            // The current available balance of the KAE is all the approved value 
            // minus the used value of all posted request line items
            var postedLines = from e in _context.SupportRequestLineItems
                              where e.SupportRequest.AccountExecutive.UserName == user.UserName &&
                              e.SupportRequest.State == SupportRequestStates.Posted
                              select e;

            return postedLines.Sum(e => (decimal?)(e.ApprovedValue - e.UsedValue)) ?? 0m;
        }

        private void PushSendEmail(List<Func<Task>> deferredActions, int requestId, string recipientEmail, string subject, string message)
        {
            deferredActions.Add(async () =>
            {
                // In a bigger app, the API should not know where the SPA lives
                // But this is fine and convenient for now
                string url = $"https://{Request.Host}/{Request.PathBase}support-requests/{requestId}";

                // Prepare the email content
                string htmlEmailContent = Util.Util.BSharpEmailTemplate(
                        message: message,
                        hrefToAction: url,
                        hrefLabel: "View Request");

                // Send the email using injected sender
                await _emailSender.SendEmail(
                    destinationEmailAddress: recipientEmail,
                    subject: subject,
                    htmlEmail: htmlEmailContent);
            });
        }

        private void CheckRoles(User user, params string[] allowedRoles)
        {
            if (user.Role != Roles.Administrator && !allowedRoles.Contains(user.Role))
            {
                throw new InvalidOperationException($"To perform this state change you must belong to one of the following roles: Administrator, {string.Join(", ", allowedRoles)}");
            }
        }

        private void CheckUser(User user, UserVM userVM)
        {
            if (user.Role != Roles.Administrator && user.Id != userVM.Id)
            {
                throw new InvalidOperationException($"Only the administrator or {userVM.FullName} can perform this state change");
            }
        }

        // Helper method
        private async Task<User> GetCurrentUserAsync()
        {
            // The requirement is that Key account executives can only see their own requests
            string userName = User.UserName();
            return await _userManager.FindByNameAsync(userName);
        }

        // Helper Method
        private async Task<IQueryable<SupportRequest>> ApplyRowLevelSecurityAsync(IQueryable<SupportRequest> query, User user = null)
        {
            if (user == null)
                user = await GetCurrentUserAsync();

            if (user.Role == Roles.KAE)
            {
                query = query.Where(e => e.AccountExecutive.UserName == user.UserName);
            }

            return query;
        }

        private IQueryable<SupportRequest> Includes(IQueryable<SupportRequest> query)
        {
            return query.Include(e => e.AccountExecutive)
                        .Include(e => e.Manager)
                        .Include(e => e.Store)
                        .Include(e => e.LineItems)
                        .ThenInclude(e => e.Product)
                        .Include(e => e.StateChanges)
                        .ThenInclude(e => e.User)
                        .Include(e => e.GeneratedDocuments);
        }

        // This method is reused in both Get(id) and Post(), an example of the DRY principal
        private async Task<SupportRequestVM> InternalGetAsync(int id)
        {
            // Retrieve the record
            var secureQuery = await ApplyRowLevelSecurityAsync(_context.SupportRequests.AsNoTracking());
            SupportRequest record = await Includes(secureQuery)
                                            .FirstOrDefaultAsync(e => e.Id == id);

            return _mapper.Map<SupportRequest, SupportRequestVM>(record);
        }
    }

    public class ValidationError
    {
        public string Message { get; set; }
    }
}
