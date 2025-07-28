using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PetSitterConnect.Interfaces;
using PetSitterConnect.Models;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace PetSitterConnect.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PetCareRequestsController : ControllerBase
{
    private readonly IPetCareRequestService _petCareRequestService;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PetCareRequestsController> _logger;

    public PetCareRequestsController(
        IPetCareRequestService petCareRequestService,
        IDistributedCache cache,
        ILogger<PetCareRequestsController> logger)
    {
        _petCareRequestService = petCareRequestService;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Get all pet care requests with caching for auto-scale performance
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PetCareRequest>>> GetPetCareRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? location = null,
        [FromQuery] CareType? careType = null)
    {
        try
        {
            var cacheKey = $"petcare_requests_{page}_{pageSize}_{location}_{careType}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                _logger.LogInformation("Returning cached pet care requests for key: {CacheKey}", cacheKey);
                var cachedRequests = JsonSerializer.Deserialize<IEnumerable<PetCareRequest>>(cachedResult);
                return Ok(cachedRequests);
            }

            var requests = await _petCareRequestService.GetPetCareRequestsAsync(page, pageSize, location, careType);
            
            // Cache for 5 minutes to improve auto-scale performance
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(requests), cacheOptions);
            
            _logger.LogInformation("Retrieved and cached {Count} pet care requests", requests.Count());
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pet care requests");
            return StatusCode(500, "An error occurred while retrieving pet care requests");
        }
    }

    /// <summary>
    /// Get a specific pet care request by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PetCareRequest>> GetPetCareRequest(int id)
    {
        try
        {
            var cacheKey = $"petcare_request_{id}";
            var cachedResult = await _cache.GetStringAsync(cacheKey);

            if (!string.IsNullOrEmpty(cachedResult))
            {
                var cachedRequest = JsonSerializer.Deserialize<PetCareRequest>(cachedResult);
                return Ok(cachedRequest);
            }

            var request = await _petCareRequestService.GetPetCareRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound($"Pet care request with ID {id} not found");
            }

            // Cache for 10 minutes
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(request), cacheOptions);
            
            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pet care request {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the pet care request");
        }
    }

    /// <summary>
    /// Create a new pet care request
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PetCareRequest>> CreatePetCareRequest([FromBody] PetCareRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdRequest = await _petCareRequestService.CreatePetCareRequestAsync(request);
            
            // Invalidate related cache entries
            await InvalidateRelatedCache();
            
            _logger.LogInformation("Created new pet care request with ID {Id}", createdRequest.Id);
            return CreatedAtAction(nameof(GetPetCareRequest), new { id = createdRequest.Id }, createdRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating pet care request");
            return StatusCode(500, "An error occurred while creating the pet care request");
        }
    }

    /// <summary>
    /// Update an existing pet care request
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePetCareRequest(int id, [FromBody] PetCareRequest request)
    {
        try
        {
            if (id != request.Id)
            {
                return BadRequest("ID mismatch");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _petCareRequestService.UpdatePetCareRequestAsync(request);
            if (!updated)
            {
                return NotFound($"Pet care request with ID {id} not found");
            }

            // Invalidate cache for this specific request and related caches
            await _cache.RemoveAsync($"petcare_request_{id}");
            await InvalidateRelatedCache();
            
            _logger.LogInformation("Updated pet care request with ID {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating pet care request {Id}", id);
            return StatusCode(500, "An error occurred while updating the pet care request");
        }
    }

    /// <summary>
    /// Delete a pet care request
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePetCareRequest(int id)
    {
        try
        {
            var deleted = await _petCareRequestService.DeletePetCareRequestAsync(id);
            if (!deleted)
            {
                return NotFound($"Pet care request with ID {id} not found");
            }

            // Invalidate cache
            await _cache.RemoveAsync($"petcare_request_{id}");
            await InvalidateRelatedCache();
            
            _logger.LogInformation("Deleted pet care request with ID {Id}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting pet care request {Id}", id);
            return StatusCode(500, "An error occurred while deleting the pet care request");
        }
    }

    private async Task InvalidateRelatedCache()
    {
        // In a production environment, you might want to use a more sophisticated cache invalidation strategy
        // For now, we'll use a simple pattern-based approach
        // This is a simplified example - in production, consider using cache tags or other strategies
        
        // Remove common cache patterns
        var cacheKeys = new[]
        {
            "petcare_requests_1_10__",
            "petcare_requests_1_20__",
            // Add more patterns as needed
        };

        foreach (var key in cacheKeys)
        {
            await _cache.RemoveAsync(key);
        }
    }
}