# Contacts Module

De Contacts module biedt functionaliteit voor het beheren van klanten, leveranciers en contacten die zowel klant als leverancier zijn.

## Features

- ✅ Tenant-scoped contacten
- ✅ Drie types: Customer, Supplier, Both
- ✅ Volledige CRUD functionaliteit
- ✅ Search functionaliteit (zoeken in naam, email, telefoon, BTW-nummer, KvK)
- ✅ Filter op type (Customer/Supplier/Both)
- ✅ Filter op actieve status
- ✅ Paginering
- ✅ Email format validatie
- ✅ Seeding van voorbeelddata

## Entity Structure

```csharp
Contact
├── Id (Guid)
├── TenantId (Guid) - Multi-tenant isolation
├── Type (ContactType enum) - Customer/Supplier/Both
├── DisplayName (string, required) - Weergavenaam
├── Email (string, optional) - Email adres (met validatie)
├── Phone (string, optional) - Telefoonnummer
├── AddressLine1 (string, optional) - Adresregel 1
├── AddressLine2 (string, optional) - Adresregel 2
├── PostalCode (string, optional) - Postcode
├── City (string, optional) - Plaats
├── Country (string, default "NL") - Land (2-letter code)
├── VatNumber (string, optional) - BTW nummer
├── KvK (string, optional) - KvK nummer
├── IsActive (bool, default true) - Actieve status
├── CreatedAt (DateTime)
└── UpdatedAt (DateTime?)
```

## API Endpoints

### GET /api/contacts
Haal alle contacten op met optionele filtering en paginering.

**Query Parameters:**
- `page` (int, default: 1) - Paginanummer
- `pageSize` (int, default: 20, max: 100) - Aantal items per pagina
- `q` (string, optional) - Zoekterm (zoekt in DisplayName, Email, Phone, VatNumber, KvK)
- `type` (ContactType, optional) - Filter op type (1=Customer, 2=Supplier, 3=Both)
- `isActive` (bool, optional) - Filter op actieve status

**Response:**
```json
{
  "items": [
    {
      "id": "guid",
      "type": 1,
      "typeName": "Customer",
      "displayName": "Acme Corporation",
      "email": "info@acme.nl",
      "phone": "+31 20 123 4567",
      "addressLine1": "Hoofdstraat 123",
      "addressLine2": null,
      "postalCode": "1012 AB",
      "city": "Amsterdam",
      "country": "NL",
      "vatNumber": "NL123456789B01",
      "kvk": "12345678",
      "isActive": true,
      "createdAt": "2026-01-17T18:00:00Z",
      "updatedAt": null
    }
  ],
  "totalCount": 6,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

### GET /api/contacts/{id}
Haal een specifiek contact op.

**Response:**
```json
{
  "id": "guid",
  "type": 1,
  "typeName": "Customer",
  "displayName": "Acme Corporation",
  "email": "info@acme.nl",
  "phone": "+31 20 123 4567",
  "addressLine1": "Hoofdstraat 123",
  "addressLine2": null,
  "postalCode": "1012 AB",
  "city": "Amsterdam",
  "country": "NL",
  "vatNumber": "NL123456789B01",
  "kvk": "12345678",
  "isActive": true,
  "createdAt": "2026-01-17T18:00:00Z",
  "updatedAt": null
}
```

### POST /api/contacts
Maak een nieuw contact aan.

**Request Body:**
```json
{
  "type": 1,
  "displayName": "Test Bedrijf BV",
  "email": "info@testbedrijf.nl",
  "phone": "+31 20 123 9999",
  "addressLine1": "Teststraat 1",
  "addressLine2": null,
  "postalCode": "1234 AB",
  "city": "Amsterdam",
  "country": "NL",
  "vatNumber": "NL999888777B01",
  "kvk": "99988877",
  "isActive": true
}
```

**Validaties:**
- `type` - Required
- `displayName` - Required, max 200 karakters
- `email` - Optioneel, maar moet geldig email formaat zijn
- `country` - Moet precies 2 karakters zijn

### PUT /api/contacts/{id}
Update een bestaand contact.

**Request Body:** Zelfde structuur als POST

### DELETE /api/contacts/{id}
Verwijder een contact.

**Response:** 204 No Content

## Contact Types

```csharp
public enum ContactType
{
    Customer = 1,   // Klant
    Supplier = 2,   // Leverancier
    Both = 3        // Zowel klant als leverancier
}
```

## Seeded Data

Bij development worden automatisch 6 voorbeeldcontacten aangemaakt per tenant:
- 2 Klanten (waaronder Acme Corporation en TechStart BV)
- 2 Leveranciers (Office Supplies Nederland en CloudHost Services)
- 1 Both (Software Solutions Group)
- 1 Particuliere klant zonder BTW/KvK (Jan Jansen)

## Testing

Gebruik de test scripts om de API te testen:

### Uitgebreide test (alle functionaliteit):
```powershell
.\test-contacts.ps1
```

### Snelle test (basis functionaliteit):
```powershell
.\test-contacts-simple.ps1
```

## Database Indexes

Voor optimale performance zijn de volgende indexes aangemaakt:
- `IX_Contacts_TenantId_DisplayName` - Voor naam zoeken
- `IX_Contacts_TenantId_Type_IsActive` - Voor type filtering
- `IX_Contacts_TenantId_Email` - Voor email lookup

## Multi-Tenancy

Alle contacten zijn tenant-scoped:
- Automatische TenantId toewijzing bij aanmaken
- Query filter zorgt ervoor dat alleen contacten van de huidige tenant zichtbaar zijn
- X-Tenant-Id header required voor alle requests

## Architecture

```
Api/Controllers/ContactsController.cs
  ↓
Application/Interfaces/IContactService.cs
  ↓
Infrastructure/Services/ContactService.cs
  ↓
Infrastructure/Data/ApplicationDbContext.cs
  ↓
Domain/Entities/Contact.cs
```

## Future Enhancements

Mogelijke uitbreidingen:
- [ ] Contact import/export (CSV, Excel)
- [ ] Contact categorieën/tags
- [ ] Contact notities/geschiedenis
- [ ] Link naar facturen/journal entries
- [ ] Duplicate detection
- [ ] Bulk operations
- [ ] Advanced search filters
