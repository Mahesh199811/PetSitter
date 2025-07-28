using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Models;
using PetSitterConnect.Interfaces;

namespace PetSitterConnect.Services;

public class PetCareRequestService : IPetCareRequestService
{
    private readonly PetSitterDbContext _context;

    public PetCareRequestService(PetSitterDbContext context)
    {
        _context = context;
    }

    public async Task<PetCareRequest?> GetRequestByIdAsync(int requestId)
    {
        return await _context.PetCareRequests
            .Include(r => r.Owner)
            .Include(r => r.Pet)
            .Include(r => r.Bookings)
            .FirstOrDefaultAsync(r => r.Id == requestId);
    }

    public async Task<IEnumerable<PetCareRequest>> GetRequestsByOwnerAsync(string ownerId)
    {
        return await _context.PetCareRequests
            .Include(r => r.Pet)
            .Include(r => r.Bookings)
            .Where(r => r.OwnerId == ownerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<PetCareRequest>> GetAvailableRequestsAsync(double? latitude = null, double? longitude = null, double radiusKm = 50)
    {
        var query = _context.PetCareRequests
            .Include(r => r.Owner)
            .Include(r => r.Pet)
            .Where(r => r.Status == RequestStatus.Open && r.EndDate > DateTime.UtcNow);

        if (latitude.HasValue && longitude.HasValue)
        {
            // Simple distance calculation
            query = query.Where(r => r.Latitude.HasValue && r.Longitude.HasValue)
                        .Where(r => Math.Abs(r.Latitude!.Value - latitude.Value) <= radiusKm / 111.0 
                                   && Math.Abs(r.Longitude!.Value - longitude.Value) <= radiusKm / 111.0);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<PetCareRequest> CreateRequestAsync(PetCareRequest request)
    {
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.Status = RequestStatus.Open;

        _context.PetCareRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<bool> UpdateRequestAsync(PetCareRequest request)
    {
        try
        {
            request.UpdatedAt = DateTime.UtcNow;
            _context.PetCareRequests.Update(request);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteRequestAsync(int requestId)
    {
        try
        {
            var request = await _context.PetCareRequests.FindAsync(requestId);
            if (request == null) return false;

            _context.PetCareRequests.Remove(request);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateRequestStatusAsync(int requestId, RequestStatus status)
    {
        try
        {
            var request = await _context.PetCareRequests.FindAsync(requestId);
            if (request == null) return false;

            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<PetCareRequest>> SearchRequestsAsync(string searchTerm, CareType? careType = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.PetCareRequests
            .Include(r => r.Owner)
            .Include(r => r.Pet)
            .Where(r => r.Status == RequestStatus.Open);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(searchTerm) ||
                                    r.Description!.ToLower().Contains(searchTerm) ||
                                    r.Location!.ToLower().Contains(searchTerm) ||
                                    r.Pet.Name.ToLower().Contains(searchTerm));
        }

        if (careType.HasValue)
        {
            query = query.Where(r => r.CareType == careType.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(r => r.StartDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(r => r.EndDate <= endDate.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    // Additional methods for API compatibility
    public async Task<PetCareRequest?> GetPetCareRequestByIdAsync(int requestId)
    {
        return await GetRequestByIdAsync(requestId);
    }

    public async Task<IEnumerable<PetCareRequest>> GetPetCareRequestsAsync(int page, int pageSize, string? location = null, CareType? careType = null)
    {
        var query = _context.PetCareRequests
            .Include(r => r.Owner)
            .Include(r => r.Pet)
            .Where(r => r.Status == RequestStatus.Open);

        if (!string.IsNullOrWhiteSpace(location))
        {
            query = query.Where(r => r.Location!.ToLower().Contains(location.ToLower()));
        }

        if (careType.HasValue)
        {
            query = query.Where(r => r.CareType == careType.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<PetCareRequest> CreatePetCareRequestAsync(PetCareRequest request)
    {
        return await CreateRequestAsync(request);
    }

    public async Task<bool> UpdatePetCareRequestAsync(PetCareRequest request)
    {
        return await UpdateRequestAsync(request);
    }

    public async Task<bool> DeletePetCareRequestAsync(int requestId)
    {
        return await DeleteRequestAsync(requestId);
    }
}
