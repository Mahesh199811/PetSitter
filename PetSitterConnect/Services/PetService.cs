using Microsoft.EntityFrameworkCore;
using PetSitterConnect.Data;
using PetSitterConnect.Models;

namespace PetSitterConnect.Services;

public class PetService : IPetService
{
    private readonly PetSitterDbContext _context;

    public PetService(PetSitterDbContext context)
    {
        _context = context;
    }

    public async Task<Pet?> GetPetByIdAsync(int petId)
    {
        return await _context.Pets
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.Id == petId);
    }

    public async Task<IEnumerable<Pet>> GetPetsByOwnerAsync(string ownerId)
    {
        return await _context.Pets
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Pet> CreatePetAsync(Pet pet)
    {
        pet.CreatedAt = DateTime.UtcNow;
        pet.UpdatedAt = DateTime.UtcNow;

        _context.Pets.Add(pet);
        await _context.SaveChangesAsync();
        return pet;
    }

    public async Task<bool> UpdatePetAsync(Pet pet)
    {
        try
        {
            pet.UpdatedAt = DateTime.UtcNow;
            _context.Pets.Update(pet);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeletePetAsync(int petId)
    {
        try
        {
            var pet = await _context.Pets.FindAsync(petId);
            if (pet == null) return false;

            _context.Pets.Remove(pet);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Pet>> SearchPetsAsync(string searchTerm, PetType? petType = null)
    {
        var query = _context.Pets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(searchTerm) ||
                                    p.Breed!.ToLower().Contains(searchTerm) ||
                                    p.Description!.ToLower().Contains(searchTerm));
        }

        if (petType.HasValue)
        {
            query = query.Where(p => p.Type == petType.Value);
        }

        return await query
            .Include(p => p.Owner)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }
}
