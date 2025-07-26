using PetSitterConnect.Models;

namespace PetSitterConnect.Interfaces;

public interface IPetService
{
    Task<Pet?> GetPetByIdAsync(int petId);
    Task<IEnumerable<Pet>> GetPetsByOwnerAsync(string ownerId);
    Task<Pet> CreatePetAsync(Pet pet);
    Task<bool> UpdatePetAsync(Pet pet);
    Task<bool> DeletePetAsync(int petId);
    Task<IEnumerable<Pet>> SearchPetsAsync(string searchTerm, PetType? petType = null);
}
