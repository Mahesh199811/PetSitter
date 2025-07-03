# Role-Based Access Control in PetSitter Connect

## 🔒 How Pet Information Access Works

### **Pet Owner Workflow:**

#### **1. Creating a Pet Care Request**
```
Pet Owner logs in → Goes to "Requests" tab → Clicks "Create Request"
↓
Pet Picker shows ONLY their own pets (filtered by OwnerId)
↓
Selects their pet → Fills request details → Publishes request
↓
Request becomes visible to Pet Sitters WITH pet details
```

**Code Implementation:**
```csharp
// In PetService.cs - Only owner's pets are shown
public async Task<IEnumerable<Pet>> GetPetsByOwnerAsync(string ownerId)
{
    return await _context.Pets
        .Where(p => p.OwnerId == ownerId)  // ✅ Security: Only owner's pets
        .OrderBy(p => p.Name)
        .ToListAsync();
}
```

#### **2. Viewing Their Own Requests**
```
Pet Owner → "Requests" tab → Toggle "My Requests Only"
↓
Shows only their own requests with basic pet info
```

### **Pet Sitter Workflow:**

#### **1. Browsing Available Requests**
```
Pet Sitter logs in → Goes to "Requests" tab → Views all available requests
↓
Can see pet details (name, breed, age, special needs) for ALL open requests
↓
This is INTENTIONAL - sitters need pet info to make informed decisions
```

**Code Implementation:**
```csharp
// In PetCareRequestService.cs - Sitters see pet details
public async Task<IEnumerable<PetCareRequest>> GetAvailableRequestsAsync(...)
{
    return await _context.PetCareRequests
        .Include(r => r.Owner)
        .Include(r => r.Pet)  // ✅ Pet details included for sitters
        .Where(r => r.Status == RequestStatus.Open)
        .ToListAsync();
}
```

#### **2. Applying for Requests**
```
Pet Sitter → Views request details → Sees full pet information → Applies
↓
Pet Owner receives application notification
↓
Pet Owner can accept/reject the application
```

## 🎯 **Why Pet Sitters Can See Pet Details**

### **This is the CORRECT behavior because:**

1. **Informed Decision Making**: Sitters need to know if they can handle the specific pet
2. **Safety**: Sitters need to know about medical conditions, special needs, temperament
3. **Service Quality**: Better matching between pet needs and sitter capabilities
4. **Transparency**: Open information leads to better care outcomes

### **Real-World Examples:**

**Scenario 1: Dog with Medical Needs**
```
Pet Owner creates request for "Max" (Diabetic Labrador, needs insulin)
↓
Pet Sitters can see: "Max, Labrador, 8 years old, diabetic, needs medication"
↓
Only experienced sitters who can handle medical needs will apply
↓
Better care outcome for Max
```

**Scenario 2: Aggressive Cat**
```
Pet Owner creates request for "Whiskers" (Territorial Persian, doesn't like strangers)
↓
Pet Sitters can see: "Whiskers, Persian, 3 years old, territorial, needs experienced handler"
↓
Only confident cat sitters will apply
↓
Safer situation for both pet and sitter
```

## 🔐 **Security Boundaries**

### **What Pet Owners Control:**
- ✅ Only see their own pets when creating requests
- ✅ Only see their own requests in "My Requests"
- ✅ Control who can access their pet through application approval
- ✅ Can include/exclude sensitive information in request description

### **What Pet Sitters Can See:**
- ✅ Pet name, breed, age, size from published requests
- ✅ Special needs and care requirements
- ✅ Owner's general location (for service area matching)
- ❌ Cannot see pets that aren't part of published requests
- ❌ Cannot see owner's personal information beyond what's needed

### **What's Protected:**
- ❌ Pet Sitters cannot see pets from unpublished requests
- ❌ Pet Sitters cannot create requests for other people's pets
- ❌ Pet Owners cannot see other owners' pets
- ❌ Detailed personal information is not shared

## 📱 **UI Implementation**

### **Pet Owner View (Create Request):**
```xml
<!-- Only shows owner's pets -->
<Picker ItemsSource="{Binding UserPets}"
        SelectedItem="{Binding SelectedPet}"
        ItemDisplayBinding="{Binding Name}"
        Title="Choose your pet" />
```

### **Pet Sitter View (Browse Requests):**
```xml
<!-- Shows pet details from published requests -->
<StackLayout>
    <Label Text="{Binding Pet.Name}" FontAttributes="Bold" />
    <Label Text="{Binding Pet.Type}" />
    <Label Text="{Binding Pet.Breed}" />
    <Label Text="{Binding Pet.Age, StringFormat='Age: {0}'}" />
    <Label Text="{Binding Pet.SpecialNeeds}" />
</StackLayout>
```

## ✅ **Testing the Role-Based Access**

### **Test as Pet Owner:**
1. Login with: `admin@petsitterconnect.com` / `Admin123!`
2. Go to "Requests" → "Create Request"
3. Pet picker shows: Buddy, Whiskers, Max (only admin's pets)
4. Create a request and publish it

### **Test as Pet Sitter:**
1. Register a new account as "Pet Sitter"
2. Go to "Requests" tab
3. You'll see the request created by admin WITH pet details
4. This is correct - sitters need pet info to provide good care

## 🎯 **Summary**

The current implementation is **CORRECT**:

- **Pet Owners** can only select from their own pets when creating requests
- **Pet Sitters** can see pet details from published requests to make informed decisions
- **Security** is maintained through proper data filtering and access controls
- **User Experience** is optimized for both safety and service quality

This follows industry standards for pet care platforms like Rover, Wag, and Care.com where transparency about pet information leads to better care outcomes.
