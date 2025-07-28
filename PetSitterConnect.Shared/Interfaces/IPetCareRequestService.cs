using PetSitterConnect.Models;

namespace PetSitterConnect.Interfaces;

public interface IPetCareRequestService
{
    Task<PetCareRequest?> GetRequestByIdAsync(int requestId);
    Task<PetCareRequest?> GetPetCareRequestByIdAsync(int requestId);
    Task<IEnumerable<PetCareRequest>> GetRequestsByOwnerAsync(string ownerId);
    Task<IEnumerable<PetCareRequest>> GetPetCareRequestsAsync(int page, int pageSize, string? location = null, CareType? careType = null);
    Task<IEnumerable<PetCareRequest>> GetAvailableRequestsAsync(double? latitude = null, double? longitude = null, double radiusKm = 50);
    Task<PetCareRequest> CreateRequestAsync(PetCareRequest request);
    Task<PetCareRequest> CreatePetCareRequestAsync(PetCareRequest request);
    Task<bool> UpdateRequestAsync(PetCareRequest request);
    Task<bool> UpdatePetCareRequestAsync(PetCareRequest request);
    Task<bool> DeleteRequestAsync(int requestId);
    Task<bool> DeletePetCareRequestAsync(int requestId);
    Task<bool> UpdateRequestStatusAsync(int requestId, RequestStatus status);
    Task<IEnumerable<PetCareRequest>> SearchRequestsAsync(string searchTerm, CareType? careType = null, DateTime? startDate = null, DateTime? endDate = null);
}
