# BTW Rapportage - Gebruiksvoorbeeld

## BTW Rapportage Implementatie - Samenvatting

✅ **Implementatie Compleet**

De NL BTW rapportage functie is succesvol geïmplementeerd in je boekhouding SaaS applicatie.

### Wat is geïmplementeerd:

1. **DTO Response** (`VatReportDto.cs`)
   - Periode informatie (from/to dates)
   - BTW uitsplitsing per tarief (0%, 9%, 21%)
   - Totalen (omzet, BTW, totaal incl. BTW)
   - Aantal geboekte facturen

2. **Service Layer** (`ReportService.cs`)
   - Filtert op geboekte facturen (source of truth)
   - Groepeert per BTW tarief
   - Berekent automatisch totalen

3. **API Endpoint** (`ReportsController.cs`)
   - `GET /api/reports/vat?from=YYYY-MM-DD&to=YYYY-MM-DD`
   - Authenticatie vereist (Admin/Owner)
   - Volledig gedocumenteerd in Swagger

### Gebruik via Swagger UI

1. Start de API: `dotnet run --project src\Api\Api.csproj`
2. Open Swagger: `http://localhost:5001/swagger`
3. Login via `/api/auth/login` endpoint:
   ```json
   {
     "email": "admin@local.test",
     "password": "Admin123!"
   }
   ```
4. Kopieer de JWT token uit de response
5. Klik op "Authorize" knop in Swagger (rechtsboven)
6. Plak token: `Bearer [YOUR_TOKEN_HERE]`
7. Test `/api/reports/vat` endpoint met datums

### Voorbeeld Request (cURL)

```bash
# 1. Login
curl -X POST "http://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@local.test","password":"Admin123!"}'

# Response bevat: {"token": "eyJ..."}

# 2. Get VAT Report
curl -X GET "http://localhost:5001/api/reports/vat?from=2026-01-01&to=2026-01-31" \
  -H "Authorization: Bearer eyJ..."
```

### Voorbeeld Response

```json
{
  "fromDate": "2026-01-01T00:00:00Z",
  "toDate": "2026-01-31T00:00:00Z",
  "vatRates": [
    {
      "vatRate": 0.0,
      "revenue": 600.00,
      "vatAmount": 0.00,
      "lineCount": 1
    },
    {
      "vatRate": 9.0,
      "revenue": 300.00,
      "vatAmount": 27.00,
      "lineCount": 2
    },
    {
      "vatRate": 21.0,
      "revenue": 1500.00,
      "vatAmount": 315.00,
      "lineCount": 2
    }
  ],
  "totalRevenue": 2400.00,
  "totalVat": 342.00,
  "totalIncludingVat": 2742.00,
  "invoiceCount": 4
}
```

### Postman Collection

Importeer deze requests in Postman:

**1. Login**
```
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "admin@local.test",
  "password": "Admin123!"
}
```

**2. VAT Report**
```
GET http://localhost:5001/api/reports/vat?from=2026-01-01&to=2026-01-31
Authorization: Bearer {{token}}
```

### Gebruik in Frontend

```typescript
// types/vat.ts
export interface VatReportDto {
  fromDate: string;
  toDate: string;
  vatRates: VatRateBreakdownDto[];
  totalRevenue: number;
  totalVat: number;
  totalIncludingVat: number;
  invoiceCount: number;
}

export interface VatRateBreakdownDto {
  vatRate: number;
  revenue: number;
  vatAmount: number;
  lineCount: number;
}

// services/reports.ts
export async function getVatReport(
  from: string,
  to: string,
  token: string
): Promise<VatReportDto> {
  const response = await fetch(
    `http://localhost:5001/api/reports/vat?from=${from}&to=${to}`,
    {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }
  );
  
  if (!response.ok) {
    throw new Error('Failed to fetch VAT report');
  }
  
  return response.json();
}

// components/VatReport.tsx
import { useEffect, useState } from 'react';
import { getVatReport, VatReportDto } from '../services/reports';

export function VatReport() {
  const [report, setReport] = useState<VatReportDto | null>(null);
  
  useEffect(() => {
    const fetchReport = async () => {
      const token = localStorage.getItem('token');
      if (!token) return;
      
      const data = await getVatReport('2026-01-01', '2026-01-31', token);
      setReport(data);
    };
    
    fetchReport();
  }, []);
  
  if (!report) return <div>Loading...</div>;
  
  return (
    <div>
      <h2>BTW Rapportage</h2>
      <p>Periode: {report.fromDate.substring(0, 10)} t/m {report.toDate.substring(0, 10)}</p>
      <p>Aantal facturen: {report.invoiceCount}</p>
      
      <table>
        <thead>
          <tr>
            <th>BTW Tarief</th>
            <th>Omzet (excl. BTW)</th>
            <th>BTW Bedrag</th>
          </tr>
        </thead>
        <tbody>
          {report.vatRates.map((rate) => (
            <tr key={rate.vatRate}>
              <td>{rate.vatRate}%</td>
              <td>€ {rate.revenue.toFixed(2)}</td>
              <td>€ {rate.vatAmount.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
        <tfoot>
          <tr>
            <th>Totaal</th>
            <th>€ {report.totalRevenue.toFixed(2)}</th>
            <th>€ {report.totalVat.toFixed(2)}</th>
          </tr>
        </tfoot>
      </table>
      
      <p><strong>Totaal incl. BTW: € {report.totalIncludingVat.toFixed(2)}</strong></p>
      <p><strong>Af te dragen BTW: € {report.totalVat.toFixed(2)}</strong></p>
    </div>
  );
}
```

### Belangrijke Punten

- ✅ **Alleen geboekte facturen**: Rapportage is gebaseerd op `Posted` invoices
- ✅ **Factuurdatum**: Filtert op `IssueDate` van de factuur
- ✅ **Multi-tenant safe**: Automatisch gefilterd per tenant
- ✅ **Volledige berekening**: Alle BTW tarieven worden automatisch berekend

### Geïmplementeerde Bestanden

```
backend/
├── src/
│   ├── Application/
│   │   ├── DTOs/Reports/
│   │   │   └── VatReportDto.cs          ✅ Nieuw
│   │   └── Interfaces/
│   │       └── IReportService.cs        ✅ Updated
│   ├── Infrastructure/
│   │   └── Services/
│   │       └── ReportService.cs         ✅ Updated
│   └── Api/
│       └── Controllers/
│           └── ReportsController.cs     ✅ Updated
└── VAT_REPORT_README.md                 ✅ Documentatie
```

### Volgende Stappen (Optioneel)

Voor verdere uitbreiding kun je overwegen:

1. **BTW Codes Tabel**: Voor specifieke aangifteformulieren (rubrieken 1a, 1b, etc.)
2. **Creditnota's**: Aftrek van creditnota's van de BTW
3. **Inkoop BTW**: Voorbelasting berekenen
4. **Export functie**: Download als CSV/Excel
5. **Automatische aangiftes**: Integratie met Belastingdienst API

---

**De implementatie is klaar voor productie gebruik!** 🎉

Gebruik Swagger UI voor het testen van de endpoint.
