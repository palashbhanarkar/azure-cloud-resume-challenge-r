# Copilot Instructions — Azure Cloud Resume Challenge

## Purpose
Rapidly onboard AI coding agents to make safe, accurate edits and execute common developer workflows.

## Architecture at a Glance

**Full-stack serverless portfolio**: Static frontend + HTTP-triggered Azure Function + Cosmos DB Table Storage

- **Frontend** (`frontend/`): Static HTML/CSS/JS → Azure Static Web Apps (no build required)
- **Backend** (`backend/api/GetResumeCounter/`): C# Azure Function (.NET 6.0) → reads/increments visitor counter
- **Storage** (Cosmos DB): Single-entity Table API table `VisitorCount` (PartitionKey="1", RowKey="1")

**Visitor counter flow**: Frontend calls hardcoded API endpoint (line ~126 in `main.js`) → Function increments Cosmos DB entity → response displayed in `#counter` DOM element.

## Critical Files by Role

| File | Purpose | Edit Notes |
|------|---------|-----------|
| `backend/api/GetResumeCounter/VisitorCounter.cs` | Function logic; Table ops, `visitorCount` property | Single-entity pattern; string-int conversions |
| `backend/api/GetResumeCounter/GetResumeCounter.csproj` | NuGet deps (Cosmos.Table v1.0.8, Functions v4.3.0) | Version changes require testing |
| `frontend/assets/js/main.js` | Event listeners, API fetch, DOM manipulation | API URL at line 126; `counter` element ID at line 133 |
| `frontend/assets/css/styles.css` | CSS variables (`--hue-color`, `--mb-*`); responsive typography | Change `--hue-color: 230` for theme |
| `frontend/index.html` | Static structure; nav, skills, projects, certs, counter | No compilation step |

## Build & Run Workflows

### Preferred: VS Code Tasks
```
clean (functions)          → dotnet clean
build (functions)          → dotnet build (depends on clean)
publish (functions)        → dotnet publish Release (depends on clean release)
func: 4                    → Azure Functions host on port 7050 (depends on build)
```

### Manual (from repo root)
```bash
# Backend
dotnet build backend/api/GetResumeCounter
func host start --script-root backend/api/GetResumeCounter/bin/Debug/net6.0

# Local test endpoint
curl http://localhost:7071/api/VisitorCounter
```

### Local Secrets
`local.settings.json` (git-ignored): Set `VisitorCounterConnectionString` to Cosmos DB connection string. Never commit.

## Patterns & Conventions

### Frontend: ID-Based Event Handlers
- Navigation: `nav-toggle`, `nav-close`, `nav-menu` (IDs)
- Skills: section headers with click → toggles `skills__open`/`skills__close` classes
- Modals: index-based; click → adds `active-modal` class
- Counter: fetched on `DOMContentLoaded`; writes to `#counter` via `textContent`
- Use `let` or `const` to prevent variable leaking to global scope

```javascript
// Pattern from main.js - proper scoping
for(let i=0; i < skillsContent.length; i++){
    skillsContent[i].className = 'skills__content skills__close'
}
```

### Backend: Table API Single-Entity Pattern with Error Handling
Retrieve → Validate → Modify → Replace with comprehensive error checking:
```csharp
// Configuration constants
private const string TableName = "VisitorCount";
private const string PartitionKey = "1";
private const string RowKey = "1";
private const string CounterPropertyName = "visitorCount";

try
{
    var entity = await RetrieveEntityAsync(table, PartitionKey, RowKey);
    if (entity == null) return NotFound("Counter not found");
    
    if (!int.TryParse(entity.Properties[CounterPropertyName].StringValue, out int count))
        return BadRequest("Invalid counter format");
    
    entity.Properties[CounterPropertyName].StringValue = (count + 1).ToString();
    await table.ExecuteAsync(TableOperation.Replace(entity));
    return Ok((count + 1).ToString());
}
catch (StorageException ex)
{
    log.LogError($"Storage error: {ex.Message}");
    return StatusCode(500, "Storage operation failed");
}
```

**Constraint**: Schema tightly coupled to code via constants. Changing partition/row keys requires `VisitorCounter.cs` updates.

### Frontend: API Configuration & Error Handling Pattern
```javascript
// Centralized configuration - supports environment variables
const VISITOR_COUNTER_CONFIG = {
    apiUrl: window.VISITOR_COUNTER_API_URL || "https://default-url",
    counterId: "counter",
    timeout: 5000
};

// Fetch with timeout, error handling, and fallback UI
try {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), VISITOR_COUNTER_CONFIG.timeout);
    
    const response = await fetch(VISITOR_COUNTER_CONFIG.apiUrl, { signal: controller.signal });
    clearTimeout(timeoutId);
    
    if (!response.ok) throw new Error(`API error: ${response.status}`);
    
    const data = await response.text();
    if (!data || isNaN(data)) throw new Error("Invalid data");
    
    document.getElementById(VISITOR_COUNTER_CONFIG.counterId).textContent = data;
} catch (error) {
    console.error(`Failed: ${error.message}`);
    // Show fallback UI - "N/A" or "Loading..."
}
```

### CSS: HSL-Based Theming
Root variables enable one-line theme swap:
```css
:root {
    --hue-color: 230; /* Blue 230, Green 142, Purple 250, Pink 340 */
    --mb-0-25: .25rem;  /* Responsive margins */
    --big-font-size: 2rem;
}
```

## CI/CD & Deployment

**Workflow**: `.github/workflows/azure-static-web-apps-jolly-field-0c7800903.yml`
- **Trigger**: `push` or `pull_request` → `master` branch
- **Auth**: GitHub OIDC (keyless)
- **Frontend deploy**: Static Web Apps → `./frontend/`
- **Backend deploy**: Separate (not in this workflow)

Pushing to `master` auto-deploys frontend. Backend requires separate Azure Functions deployment.

## What's Safe to Change

✅ **Frontend**: CSS/HTML/JS edits in `frontend/` (no build step)
✅ **Function logging**: `log.LogInformation()` calls in `VisitorCounter.cs` (but NEVER log secrets)
✅ **Exception handling**: Already implemented; add more specific catch blocks as needed
✅ **Hardcoded API URL**: Update via environment variable `window.VISITOR_COUNTER_API_URL`
✅ **Configuration**: Modify `VISITOR_COUNTER_CONFIG` in `main.js` for timeouts, retries, etc.

## What to Avoid

❌ **Table schema changes** without updating constants in `VisitorCounter.cs` 
❌ **NuGet version upgrades** (e.g., Cosmos.Table 1.0.8) without compatibility testing
❌ **Committing `.env.local`** or any `.env*` files (secrets are git-ignored)
❌ **Removing error handling** from fetch calls or Table operations
❌ **Hardcoding secrets** in frontend code (API keys, connection strings)
❌ **Azure Functions binding decorator changes** without understanding runtime lifecycle
❌ **Removing null checks** on entity properties—counter table has single-entity schema

## Common Tasks

**Add a frontend feature**:
1. Add HTML to `index.html`
2. Add CSS to `styles.css` (use `--variable-names`)
3. Add event listener to `main.js` (pattern: `document.getElementById('id').addEventListener(...)`)
4. Push to `master`

**Debug backend locally**:
1. Ensure `VisitorCounterConnectionString` in `local.settings.json`
2. Run task `build (functions)` → `func: 4`
3. Call `http://localhost:7071/api/VisitorCounter`
4. Check logs in terminal

**Change theme color**:
Edit `frontend/assets/css/styles.css`, line 10:
```css
--hue-color: 250; /* e.g., Purple */
```

**Test visitor counter end-to-end**:
1. Build backend, start Functions host (task `func: 4`)
2. Open `frontend/index.html` in browser (local HTTP server)
3. Check console for "Visitor Count API called" log
4. Verify `#counter` element displays number

## Tech Stack Details

- **.NET 6.0** (target in `.csproj`; port 7050 in `launchSettings.json`)
- **Cosmos.Table 1.0.8** (legacy Table API—stable, not `3.x+`)
- **Azure Functions v4** runtime
- **Swiper** carousel library (minified; `swiper-bundle.min.js`, `swiper-bundle.min.css`)
- **Unicons** & **Boxicons** icon libraries (CDN)

---

**Last Updated**: February 2026  
**Focus**: Single-entity Table Storage pattern, event-driven frontend, serverless backend
